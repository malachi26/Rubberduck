using System.Collections.Generic;
using Antlr4.Runtime;
using Rubberduck.Common;
using Rubberduck.Parsing.Symbols;
using Rubberduck.VBEditor;

namespace Rubberduck.Inspections
{
    public class UnassignedVariableUsageInspectionResult : InspectionResultBase
    {
        private readonly IEnumerable<CodeInspectionQuickFix> _quickFixes;

        public UnassignedVariableUsageInspectionResult(IInspection inspection, ParserRuleContext context, QualifiedModuleName qualifiedName, Declaration declaration)
            : base(inspection, qualifiedName, context, declaration)
        {
            _quickFixes = new CodeInspectionQuickFix[]
            {
                //new RemoveUnassignedVariableUsageQuickFix(Context, QualifiedSelection),   // removed until we can reinstate this for a specific reference
                new IgnoreOnceQuickFix(Context, QualifiedSelection, Inspection.AnnotationName), 
            };
        }

        public override IEnumerable<CodeInspectionQuickFix> QuickFixes { get { return _quickFixes; } }

        public override string Description
        {
            get
            {
                return string.Format(InspectionsUI.UnassignedVariableUsageInspectionResultFormat, Target.IdentifierName).Captialize();
            }
        }
    }
}
