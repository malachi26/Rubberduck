﻿using NLog;
using Rubberduck.Parsing.Annotations;
using Rubberduck.Parsing.VBA;
using Rubberduck.VBEditor;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Rubberduck.Parsing.Grammar;

namespace Rubberduck.Parsing.Symbols
{
    public class SpecialFormDeclarations : ICustomDeclarationLoader
    {
        private readonly DeclarationFinder _finder;

        public SpecialFormDeclarations(RubberduckParserState state)
        {
            _finder = new DeclarationFinder(state.AllDeclarations, new CommentNode[] { }, new IAnnotation[] { });
        }


        public IReadOnlyList<Declaration> Load()
        {
            if (ThereIsAGlobalBuiltInErrVariableDeclaration(_finder))
            {
                return new List<Declaration>();
            }

            var vba = _finder.FindProject("VBA");
            if (vba == null)
            {
                // If the VBA project is null, we haven't loaded any COM references;
                // we're in a unit test and the mock project didn't setup any references.
                return new List<Declaration>();
            }

            var informationModule = _finder.FindStdModule("Information", vba, true);
            Debug.Assert(informationModule != null, "We expect the information module to exist in the VBA project.");

            return LoadSpecialFormDeclarations(informationModule);
        }

            private static bool ThereIsAGlobalBuiltInErrVariableDeclaration(DeclarationFinder finder)
            {
                return finder.MatchName(Tokens.Err).Any(declaration => declaration.IsBuiltIn
                                                                        && declaration.DeclarationType == DeclarationType.Variable
                                                                        && declaration.Accessibility == Accessibility.Global);
            }


            private List<Declaration> LoadSpecialFormDeclarations(Declaration parentModule)
            {
                Debug.Assert(parentModule != null);

                var arrayFunction = ArrayFunction(parentModule);
                var inputFunction = InputFunction(parentModule);
                var inputBFunction = InputBFunction(parentModule);
                var lboundFunction = LBoundFunction(parentModule);
                var uboundFunction = UBoundFunction(parentModule);

                return new List<Declaration> { 
                    arrayFunction,
                    inputFunction,
                    inputBFunction,
                    lboundFunction,
                    uboundFunction
                };
            }

                private static FunctionDeclaration ArrayFunction(Declaration parentModule)
                {
                    return new FunctionDeclaration(
                        new QualifiedMemberName(parentModule.QualifiedName.QualifiedModuleName, "Array"),
                        parentModule,
                        parentModule,
                        "Variant",
                        null,
                        null,
                        Accessibility.Public,
                        null,
                        Selection.Home,
                        false,
                        true,
                        null,
                        new Attributes());
                }

                private static SubroutineDeclaration InputFunction(Declaration parentModule)
                {
                    var inputFunction = InputFunctionWithoutParameters(parentModule);
                    inputFunction.AddParameter(NumberParameter(parentModule, inputFunction));
                    inputFunction.AddParameter(FileNumberParameter(parentModule, inputFunction));
                    return inputFunction;
                }

                    private static SubroutineDeclaration InputFunctionWithoutParameters(Declaration parentModule)
                    {
                        return new SubroutineDeclaration(
                            new QualifiedMemberName(parentModule.QualifiedName.QualifiedModuleName, "Input"),
                            parentModule,
                            parentModule,
                            "Variant",
                            Accessibility.Public,
                            null,
                            Selection.Home,
                            true,
                            null,
                            new Attributes());
                    }

                    private static ParameterDeclaration NumberParameter(Declaration parentModule, SubroutineDeclaration ParentSubroutine)
                    {
                        return new ParameterDeclaration(
                            new QualifiedMemberName(parentModule.QualifiedName.QualifiedModuleName, "Number"),
                            ParentSubroutine,
                            "Integer",
                            null,
                            null,
                            false,
                            false);
                    }

                    private static ParameterDeclaration FileNumberParameter(Declaration parentModule, SubroutineDeclaration ParentSubroutine)
                    {
                        return new ParameterDeclaration(
                            new QualifiedMemberName(parentModule.QualifiedName.QualifiedModuleName, "Filenumber"),
                            ParentSubroutine,
                            "Integer",
                            null,
                            null,
                            false,
                            false);
                    }


                private static SubroutineDeclaration InputBFunction(Declaration parentModule)
                {
                    var inputBFunction = InputBFunctionWithoutParameters(parentModule);
                    inputBFunction.AddParameter(NumberParameter(parentModule, inputBFunction));
                    inputBFunction.AddParameter(FileNumberParameter(parentModule, inputBFunction));
                    return inputBFunction;
                }

                    private static SubroutineDeclaration InputBFunctionWithoutParameters(Declaration parentModule)
                    {
                        return new SubroutineDeclaration(
                            new QualifiedMemberName(parentModule.QualifiedName.QualifiedModuleName, "InputB"),
                            parentModule,
                            parentModule,
                            "Variant",
                            Accessibility.Public,
                            null,
                            Selection.Home,
                            true,
                            null,
                            new Attributes());
                    }


                private static FunctionDeclaration LBoundFunction(Declaration parentModule)
                {
                    var lboundFunction = LBoundFunctionWithoutParameters(parentModule);
                    lboundFunction.AddParameter(ArrayNameParameter(parentModule, lboundFunction));
                    lboundFunction.AddParameter(DimensionParameter(parentModule, lboundFunction));
                    return lboundFunction;
                }

                    private static FunctionDeclaration LBoundFunctionWithoutParameters(Declaration parentModule)
                    {
                        return new FunctionDeclaration(
                            new QualifiedMemberName(parentModule.QualifiedName.QualifiedModuleName, "LBound"),
                            parentModule,
                            parentModule,
                            "Long",
                            null,
                            null,
                            Accessibility.Public,
                            null,
                            Selection.Home,
                            false,
                            true,
                            null,
                            new Attributes());
                    }

                    private static ParameterDeclaration ArrayNameParameter(Declaration parentModule, FunctionDeclaration parentFunction)
                    {
                        var arrayParam = new ParameterDeclaration(new QualifiedMemberName(parentModule.QualifiedName.QualifiedModuleName, "Arrayname"), parentFunction, "Variant", null, null, false, false, true);
                        return arrayParam;
                    }

                    private static ParameterDeclaration DimensionParameter(Declaration parentModule, FunctionDeclaration parentFunction)
                    {
                        var rankParam = new ParameterDeclaration(new QualifiedMemberName(parentModule.QualifiedName.QualifiedModuleName, "Dimension"), parentFunction, "Long", null, null, true, false);
                        return rankParam;
                    }


                private static FunctionDeclaration UBoundFunction(Declaration parentModule)
                {
                    var uboundFunction = UBoundFunctionWithoutParameters(parentModule);
                    uboundFunction.AddParameter(ArrayNameParameter(parentModule, uboundFunction));
                    uboundFunction.AddParameter(DimensionParameter(parentModule, uboundFunction));
                    return uboundFunction;
                }

                    private static FunctionDeclaration UBoundFunctionWithoutParameters(Declaration parentModule)
                    {
                        return new FunctionDeclaration(
                            new QualifiedMemberName(parentModule.QualifiedName.QualifiedModuleName, "UBound"),
                            parentModule,
                            parentModule,
                            "Long",
                            null,
                            null,
                            Accessibility.Public,
                            null,
                            Selection.Home,
                            false,
                            true,
                            null,
                            new Attributes());
                    }
    }
}
