using System;
using System.Collections.Generic;
using System.Linq;
using Antlr4.Runtime;
using Rubberduck.Common;
using Rubberduck.Parsing.Grammar;
using Rubberduck.Parsing.Symbols;

namespace Rubberduck.Inspections
{
    public class UntypedFunctionUsageInspectionResult : InspectionResultBase
    {
        private readonly IdentifierReference _reference;
        private readonly IEnumerable<CodeInspectionQuickFix> _quickFixes;

        public UntypedFunctionUsageInspectionResult(IInspection inspection, IdentifierReference reference) 
            : base(inspection, reference.QualifiedModuleName, reference.Context)
        {
            _reference = reference;
            _quickFixes = new CodeInspectionQuickFix[]
            {
                new UntypedFunctionUsageQuickFix((ParserRuleContext)GetFirst(typeof(VBAParser.IdentifierContext)).Parent, QualifiedSelection), 
                new IgnoreOnceQuickFix(Context, QualifiedSelection, Inspection.AnnotationName), 
            };
        }

        public override IEnumerable<CodeInspectionQuickFix> QuickFixes { get { return _quickFixes; } }

        public override string Description
        {
            get { return string.Format(Inspection.Description, _reference.Declaration.IdentifierName).Captialize(); }
        }

        private ParserRuleContext GetFirst(Type nodeType)
        {
            var unexploredNodes = new List<ParserRuleContext> {Context};

            while (unexploredNodes.Any())
            {
                if (unexploredNodes[0].GetType() == nodeType)
                {
                    return unexploredNodes[0];
                }
                
                unexploredNodes.AddRange(unexploredNodes[0].children.OfType<ParserRuleContext>());
                unexploredNodes.RemoveAt(0);
            }

            return null;
        }
    }
}
