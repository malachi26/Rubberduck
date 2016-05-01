﻿using Antlr4.Runtime;
using Rubberduck.Parsing.Symbols;

namespace Rubberduck.Parsing.Binding
{
    public sealed class MemberAccessDefaultBinding : IExpressionBinding
    {
        private readonly DeclarationFinder _declarationFinder;
        private readonly Declaration _project;
        private readonly Declaration _module;
        private readonly Declaration _parent;
        private readonly ParserRuleContext _context;
        private readonly string _name;
        private readonly IBoundExpression _lExpression;

        public MemberAccessDefaultBinding(
            DeclarationFinder declarationFinder,
            Declaration project,
            Declaration module,
            Declaration parent,
            VBAExpressionParser.MemberAccessExpressionContext expression,
            IExpressionBinding lExpressionBinding)
            : this(
                  declarationFinder,
                  project,
                  module,
                  parent,
                  expression,
                  lExpressionBinding.Resolve(),
                  ExpressionName.GetName(expression.unrestrictedName()))
        {
        }

        public MemberAccessDefaultBinding(
            DeclarationFinder declarationFinder,
            Declaration project,
            Declaration module,
            Declaration parent,
            VBAExpressionParser.MemberAccessExprContext expression,
            IExpressionBinding lExpressionBinding)
            : this(
                  declarationFinder,
                  project,
                  module,
                  parent,
                  expression,
                  lExpressionBinding.Resolve(),
                  ExpressionName.GetName(expression.unrestrictedName()))
        {
        }

        public MemberAccessDefaultBinding(
            DeclarationFinder declarationFinder,
            Declaration project,
            Declaration module,
            Declaration parent,
            ParserRuleContext expression,
            IBoundExpression lExpression,
            string name)
        {
            _declarationFinder = declarationFinder;
            _project = project;
            _module = module;
            _parent = parent;
            _context = expression;
            _lExpression = lExpression;
            _name = name;
        }

        public IBoundExpression Resolve()
        {
            IBoundExpression boundExpression = null;
            if (_lExpression == null)
            {
                return null;
            }
            boundExpression = ResolveLExpressionIsVariablePropertyOrFunction();
            if (boundExpression != null)
            {
                return boundExpression;
            }
            boundExpression = ResolveLExpressionIsUnbound();
            if (boundExpression != null)
            {
                return boundExpression;
            }
            boundExpression = ResolveLExpressionIsProject();
            if (boundExpression != null)
            {
                return boundExpression;
            }
            boundExpression = ResolveLExpressionIsProceduralModule();
            if (boundExpression != null)
            {
                return boundExpression;
            }
            boundExpression = ResolveLExpressionIsEnum();
            if (boundExpression != null)
            {
                return boundExpression;
            }
            return null;
        }

        private IBoundExpression ResolveLExpressionIsVariablePropertyOrFunction()
        {
            /*
              
                <l-expression> is classified as a variable, a property or a function and one of the following is 
                true:  
                    1. The declared type of <l-expression> is a UDT type or specific class, this type has an accessible 
                    member named <unrestricted-name>, <unrestricted-name> either does not specify a type 
                    character or specifies a type character whose associated type matches the declared type of 
                    the member, and one of the following is true:

                        1.1 The member is a variable, property or function. In this case, the member access expression 
                        is classified as a variable, property or function, respectively, refers to the member, and has 
                        the same declared type as the member.

                        1.2 The member is a subroutine. In this case, the member access expression is classified as a 
                        subroutine and refers to the member.


                    2. The declared type of <l-expression> is Object or Variant. In this case, the member access 
                    expression is classified as an unbound member and has a declared type of Variant.  
             */
            if (
                _lExpression.Classification != ExpressionClassification.Variable
                && _lExpression.Classification != ExpressionClassification.Property
                && _lExpression.Classification != ExpressionClassification.Function)
            {
                return null;
            }
            var lExpressionDeclaration = _lExpression.ReferencedDeclaration;
            var referencedType = lExpressionDeclaration.AsTypeDeclaration;
            if (referencedType == null)
            {
                return null;
            }
            if (referencedType.DeclarationType != DeclarationType.UserDefinedType && referencedType.DeclarationType != DeclarationType.ClassModule)
            {
                return null;
            }
            var variable = _declarationFinder.FindMemberWithParent(_project, _module, referencedType, _name, DeclarationType.Variable);
            if (variable != null)
            {
                return new MemberAccessExpression(variable, ExpressionClassification.Variable, _context, _lExpression);
            }
            var property = _declarationFinder.FindMemberWithParent(_project, _module, referencedType, _name, DeclarationType.Property);
            if (property != null)
            {
                return new MemberAccessExpression(property, ExpressionClassification.Property, _context, _lExpression);
            }
            var function = _declarationFinder.FindMemberWithParent(_project, _module, referencedType, _name, DeclarationType.Function);
            if (function != null)
            {
                return new MemberAccessExpression(function, ExpressionClassification.Function, _context, _lExpression);
            }
            var subroutine = _declarationFinder.FindMemberWithParent(_project, _module, referencedType, _name, DeclarationType.Procedure);
            if (subroutine != null)
            {
                return new MemberAccessExpression(subroutine, ExpressionClassification.Subroutine, _context, _lExpression);
            }
            // Note: To not have to deal with declared types we simply assume that no match means unbound member.
            // This way the rest of the member access expression can still be bound.
            return new MemberAccessExpression(null, ExpressionClassification.Unbound, _context, _lExpression);
        }

        private IBoundExpression ResolveLExpressionIsUnbound()
        {
            if (_lExpression.Classification != ExpressionClassification.Unbound)
            {
                return null;
            }
            return new MemberAccessExpression(null, ExpressionClassification.Unbound, _context, _lExpression);
        }

        private IBoundExpression ResolveLExpressionIsProject()
        {
            /*
                <l-expression> is classified as a project, this project is either the enclosing project or a 
                referenced project, and one of the following is true:  
                    -   <l-expression> refers to the enclosing project and <unrestricted-name> is either the name of 
                        the enclosing project or a referenced project. In this case, the member access expression is 
                        classified as a project and refers to the specified project.  
                    -   The project has an accessible procedural module named <unrestricted-name>. In this case, the 
                        member access expression is classified as a procedural module and refers to the specified 
                        procedural module.  
                    -   The project does not have an accessible procedural module named <unrestricted-name> and 
                        exactly one of the procedural modules within the project has an accessible member named 
                        <unrestricted-name>, <unrestricted-name> either does not specify a type character or 
                        specifies a type character whose associated type matches the declared type of the member, 
                        and one of the following is true:  
                        -   The member is a variable, property or function. In this case, the member access expression 
                            is classified as a variable, property or function, respectively, refers to the member, and has 
                            the same declared type as the member.  
                        -   The member is a subroutine. In this case, the member access expression is classified as a 
                            subroutine and refers to the member.  
                        -   The member is a value. In this case, the member access expression is classified as a value 
                            with the same declared type as the member.  
             */
            if (_lExpression.Classification != ExpressionClassification.Project)
            {
                return null;
            }
            IBoundExpression boundExpression = null;
            var referencedProject = _lExpression.ReferencedDeclaration;
            bool lExpressionIsEnclosingProject = _project.Equals(referencedProject);
            boundExpression = ResolveProject();
            if (boundExpression != null)
            {
                return boundExpression;
            }
            boundExpression = ResolveProceduralModule(lExpressionIsEnclosingProject, referencedProject);
            if (boundExpression != null)
            {
                return boundExpression;
            }
            boundExpression = ResolveMemberInReferencedProject(lExpressionIsEnclosingProject, referencedProject, DeclarationType.Variable, ExpressionClassification.Variable);
            if (boundExpression != null)
            {
                return boundExpression;
            }
            boundExpression = ResolveMemberInReferencedProject(lExpressionIsEnclosingProject, referencedProject, DeclarationType.Property, ExpressionClassification.Property);
            if (boundExpression != null)
            {
                return boundExpression;
            }
            boundExpression = ResolveMemberInReferencedProject(lExpressionIsEnclosingProject, referencedProject, DeclarationType.Function, ExpressionClassification.Function);
            if (boundExpression != null)
            {
                return boundExpression;
            }
            boundExpression = ResolveMemberInReferencedProject(lExpressionIsEnclosingProject, referencedProject, DeclarationType.Procedure, ExpressionClassification.Subroutine);
            if (boundExpression != null)
            {
                return boundExpression;
            }
            boundExpression = ResolveMemberInReferencedProject(lExpressionIsEnclosingProject, referencedProject, DeclarationType.Constant, ExpressionClassification.Value);
            if (boundExpression != null)
            {
                return boundExpression;
            }
            boundExpression = ResolveMemberInReferencedProject(lExpressionIsEnclosingProject, referencedProject, DeclarationType.Enumeration, ExpressionClassification.Value);
            if (boundExpression != null)
            {
                return boundExpression;
            }
            boundExpression = ResolveMemberInReferencedProject(lExpressionIsEnclosingProject, referencedProject, DeclarationType.EnumerationMember, ExpressionClassification.Value);
            if (boundExpression != null)
            {
                return boundExpression;
            }
            return boundExpression;
        }

        private IBoundExpression ResolveProject()
        {
            if (_declarationFinder.IsMatch(_project.ProjectName, _name))
            {
                return new MemberAccessExpression(_project, ExpressionClassification.Project, _context, _lExpression);
            }
            var referencedProjectRightOfDot = _declarationFinder.FindReferencedProject(_project, _name);
            if (referencedProjectRightOfDot != null)
            {
                return new MemberAccessExpression(referencedProjectRightOfDot, ExpressionClassification.Project, _context, _lExpression);
            }
            return null;
        }

        private IBoundExpression ResolveProceduralModule(bool lExpressionIsEnclosingProject, Declaration referencedProject)
        {
            if (lExpressionIsEnclosingProject)
            {
                if (_module.DeclarationType == DeclarationType.ProceduralModule && _declarationFinder.IsMatch(_module.IdentifierName, _name))
                {
                    return new MemberAccessExpression(_module, ExpressionClassification.ProceduralModule, _context, _lExpression);
                }
                var proceduralModuleEnclosingProject = _declarationFinder.FindModuleEnclosingProjectWithoutEnclosingModule(_project, _module, _name, DeclarationType.ProceduralModule);
                if (proceduralModuleEnclosingProject != null)
                {
                    return new MemberAccessExpression(proceduralModuleEnclosingProject, ExpressionClassification.ProceduralModule, _context, _lExpression);
                }
            }
            else
            {
                var proceduralModuleInReferencedProject = _declarationFinder.FindModuleReferencedProject(_project, _module, referencedProject, _name, DeclarationType.ProceduralModule);
                if (proceduralModuleInReferencedProject != null)
                {
                    return new MemberAccessExpression(proceduralModuleInReferencedProject, ExpressionClassification.ProceduralModule, _context, _lExpression);
                }
            }
            return null;
        }

        private IBoundExpression ResolveMemberInReferencedProject(bool lExpressionIsEnclosingProject, Declaration referencedProject, DeclarationType memberType, ExpressionClassification classification)
        {
            if (lExpressionIsEnclosingProject)
            {
                var foundType = _declarationFinder.FindMemberEnclosingModule(_project, _module, _parent, _name, memberType);
                if (foundType != null)
                {
                    return new MemberAccessExpression(foundType, classification, _context, _lExpression);
                }
                var accessibleType = _declarationFinder.FindMemberEnclosedProjectWithoutEnclosingModule(_project, _module, _parent, _name, memberType);
                if (accessibleType != null)
                {
                    return new MemberAccessExpression(accessibleType, classification, _context, _lExpression);
                }
            }
            else
            {
                var referencedProjectType = _declarationFinder.FindMemberReferencedProject(_project, _module, _parent, referencedProject, _name, memberType);
                if (referencedProjectType != null)
                {
                    return new MemberAccessExpression(referencedProjectType, classification, _context, _lExpression);
                }
            }
            return null;
        }

        private IBoundExpression ResolveLExpressionIsProceduralModule()
        {
            /*
                <l-expression> is classified as a procedural module, this procedural module has an accessible 
                member named <unrestricted-name>, <unrestricted-name> either does not specify a type 
                character or specifies a type character whose associated type matches the declared type of the 
                member, and one of the following is true:  
                    -   The member is a variable, property or function. In this case, the member access expression is 
                        classified as a variable, property or function, respectively, and has the same declared type as 
                        the member.  
                    -   The member is a subroutine. In this case, the member access expression is classified as a 
                        subroutine.  
                    -   The member is a value. In this case, the member access expression is classified as a value with 
                        the same declared type as the member. 
             */
            if (_lExpression.Classification != ExpressionClassification.ProceduralModule)
            {
                return null;
            }
            IBoundExpression boundExpression = null;
            boundExpression = ResolveMemberInModule(_lExpression.ReferencedDeclaration, DeclarationType.Variable, ExpressionClassification.Variable);
            if (boundExpression != null)
            {
                return boundExpression;
            }
            boundExpression = ResolveMemberInModule(_lExpression.ReferencedDeclaration, DeclarationType.Property, ExpressionClassification.Property);
            if (boundExpression != null)
            {
                return boundExpression;
            }
            boundExpression = ResolveMemberInModule(_lExpression.ReferencedDeclaration, DeclarationType.Function, ExpressionClassification.Function);
            if (boundExpression != null)
            {
                return boundExpression;
            }
            boundExpression = ResolveMemberInModule(_lExpression.ReferencedDeclaration, DeclarationType.Procedure, ExpressionClassification.Subroutine);
            if (boundExpression != null)
            {
                return boundExpression;
            }
            boundExpression = ResolveMemberInModule(_lExpression.ReferencedDeclaration, DeclarationType.Constant, ExpressionClassification.Value);
            if (boundExpression != null)
            {
                return boundExpression;
            }
            boundExpression = ResolveMemberInModule(_lExpression.ReferencedDeclaration, DeclarationType.Enumeration, ExpressionClassification.Value);
            if (boundExpression != null)
            {
                return boundExpression;
            }
            boundExpression = ResolveMemberInModule(_lExpression.ReferencedDeclaration, DeclarationType.EnumerationMember, ExpressionClassification.Value);
            if (boundExpression != null)
            {
                return boundExpression;
            }
            return boundExpression;
        }

        private IBoundExpression ResolveMemberInModule(Declaration module, DeclarationType memberType, ExpressionClassification classification)
        {
            var enclosingProjectType = _declarationFinder.FindMemberEnclosedProjectInModule(_project, _module, _parent, module, _name, memberType);
            if (enclosingProjectType != null)
            {
                return new MemberAccessExpression(enclosingProjectType, classification, _context, _lExpression);
            }

            var referencedProjectType = _declarationFinder.FindMemberReferencedProjectInModule(_project, _module, _parent, module, _name, memberType);
            if (referencedProjectType != null)
            {
                return new MemberAccessExpression(referencedProjectType, classification, _context, _lExpression);
            }
            return null;
        }

        private IBoundExpression ResolveLExpressionIsEnum()
        {
            /*
                <l-expression> is classified as a type, this type is an Enum type, and this type has an enum 
                member named <unrestricted-name>. In this case, the member access expression is classified 
                as a value with the same declared type as the enum member.  
             */
            if (_lExpression.Classification != ExpressionClassification.Type && _lExpression.ReferencedDeclaration.DeclarationType != DeclarationType.Enumeration)
            {
                return null;
            }
            var enumMember = _declarationFinder.FindMemberWithParent(_project, _module, _lExpression.ReferencedDeclaration, _name, DeclarationType.EnumerationMember);
            if (enumMember != null)
            {
                return new MemberAccessExpression(enumMember, ExpressionClassification.Value, _context, _lExpression);
            }
            return null;
        }
    }
}
