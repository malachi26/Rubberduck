﻿using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Rubberduck.Parsing.Symbols;
using Rubberduck.VBEditor;

namespace RubberduckTests.Symbols
{
    [TestClass]
    public class AccessibilityCheckTests
    {
        
        //project tests

        [TestMethod]
        public void ProjectsAreAlwaysAccessible()
        {
            var projectDeclatation = GetTestProject("test_project");

            Assert.IsTrue(AccessibilityCheck.IsAccessible(null,null,null,projectDeclatation));
        }

            private static ProjectDeclaration GetTestProject(string name)
            {
                var qualifiedProjectName = new QualifiedMemberName(StubQualifiedModuleName(), name);
                return new ProjectDeclaration(qualifiedProjectName, name, false);
            }

                private static QualifiedModuleName StubQualifiedModuleName()
                {
                    return new QualifiedModuleName("dummy", "dummy", "dummy");
                }



        //module tests

        [TestMethod]
        public void ModulesCanBeAccessedFromWithinThemselves()
        {
            var projectDeclatation = GetTestProject("test_project");
            var moduleDeclatation = GetTestClassModule(projectDeclatation, "test_Module");

            Assert.IsTrue(AccessibilityCheck.IsAccessible(projectDeclatation, moduleDeclatation, null, moduleDeclatation));
        }

            private static ClassModuleDeclaration GetTestClassModule(Declaration projectDeclatation, string name, bool isExposed = false)
            {
                var qualifiedClassModuleMemberName = new QualifiedMemberName(StubQualifiedModuleName(), name);
                var classModuleAttributes = new Rubberduck.Parsing.VBA.Attributes();
                if (isExposed)
                {
                    classModuleAttributes.AddExposedClassAttribute();
                }
                return new ClassModuleDeclaration(qualifiedClassModuleMemberName, projectDeclatation, name, false, null, classModuleAttributes);
            }


        [TestMethod]
        public void ModulesCanBeAccessedFromTheSameProject()
        {
            var projectDeclatation = GetTestProject("test_project");
            var calleeModuleDeclatation = GetTestClassModule(projectDeclatation, "calleeModule");
            var callingModuleDeclatation = GetTestClassModule(projectDeclatation, "callingModule");

            Assert.IsTrue(AccessibilityCheck.IsAccessible(projectDeclatation, callingModuleDeclatation, null, calleeModuleDeclatation));
        }


        [TestMethod]
        public void ExposedClassModulesCanBeAccessedFromOtherProjects()
        {
            var calleeProjectDeclatation = GetTestProject("calleeProject");
            var calleeModuleDeclatation = GetTestClassModule(calleeProjectDeclatation, "caleeModule", true);
            var callingProjectDeclatation = GetTestProject("callingProject");
            var callingModuleDeclatation = GetTestClassModule(callingProjectDeclatation, "callingModule");

            Assert.IsTrue(AccessibilityCheck.IsAccessible(callingProjectDeclatation, callingModuleDeclatation, null, calleeModuleDeclatation));
        }


        [TestMethod]
        public void NonExposedClassModulesCannotBeAccessedFromOtherProjects()
        {
            var calleeProjectDeclatation = GetTestProject("calleeProject");
            var calleeModuleDeclatation = GetTestClassModule(calleeProjectDeclatation, "caleeModule", false);
            var callingProjectDeclatation = GetTestProject("callingProject");
            var callingModuleDeclatation = GetTestClassModule(callingProjectDeclatation, "callingModule");

            Assert.IsFalse(AccessibilityCheck.IsAccessible(callingProjectDeclatation, callingModuleDeclatation, null, calleeModuleDeclatation));
        }


        [TestMethod]
        public void NonPrivateProceduralModulesCanBeAccessedFromOtherProjects()
        {
            var calleeProjectDeclatation = GetTestProject("calleeProject");
            var calleeModuleDeclatation = GetTestProceduralModule(calleeProjectDeclatation, "caleeModule");
            var callingProjectDeclatation = GetTestProject("callingProject");
            var callingModuleDeclatation = GetTestClassModule(callingProjectDeclatation, "callingModule");

            Assert.IsTrue(AccessibilityCheck.IsAccessible(callingProjectDeclatation, callingModuleDeclatation, null, calleeModuleDeclatation));
        }

            private static ProceduralModuleDeclaration GetTestProceduralModule(Declaration projectDeclatation, string name)
            {
                var qualifiedClassModuleMemberName = new QualifiedMemberName(StubQualifiedModuleName(), name);
                var proceduralModuleDeclaration = new ProceduralModuleDeclaration(qualifiedClassModuleMemberName, projectDeclatation, name, false, null, null);
                return proceduralModuleDeclaration;
            }


        //todo: Find a way to write PrivateProceduralModulesCannotBeAccessedFromOtherProjects. (isPrivateModule is a property with internal set.)



        //procedure tests

        [TestMethod]
        public void PrivateProceduresAreAccessibleFromTheEnclosingModule()
        {
            var projectDeclatation = GetTestProject("test_project");
            var moduleDeclatation = GetTestClassModule(projectDeclatation, "test_Module");
            var privateCalleeFunctionDeclaration = GetTestFunction(moduleDeclatation, "calleeFoo", Accessibility.Private);
            var privateCallingFunctionDeclaration = GetTestFunction(moduleDeclatation, "callingFoo", Accessibility.Private);

            Assert.IsTrue(AccessibilityCheck.IsAccessible(projectDeclatation, moduleDeclatation, privateCallingFunctionDeclaration, privateCalleeFunctionDeclaration));
        }

            private static FunctionDeclaration GetTestFunction(Declaration moduleDeclatation, string name, Accessibility functionAccessibility)
            {
                var qualifiedFunctionMemberName = new QualifiedMemberName(StubQualifiedModuleName(), name);
                return new FunctionDeclaration(qualifiedFunctionMemberName, moduleDeclatation, moduleDeclatation, "test", null, "test", functionAccessibility, null, Selection.Home, false, false, null, null);
            }

        [TestMethod]
        public void PrivateProceduresAreAccessibleIfTheyAreInAClassAboveTheEnclosingModuleOfTheCallerInTheClassHierarchy()
        {
            var projectDeclatation = GetTestProject("test_project");
            var callingModule = GetTestClassModule(projectDeclatation, "callingModule");
            var privateCallingFunction = GetTestFunction(callingModule, "callingFoo", Accessibility.Private);
            var supertypeOfCallingModule = GetTestClassModule(projectDeclatation, "callingModuleSuper");
            callingModule.AddSupertype(supertypeOfCallingModule);
            var supertypeOfSupertypeOfCallingModule = GetTestClassModule(projectDeclatation, "callingModuleSuperSuper");
            supertypeOfCallingModule.AddSupertype(supertypeOfSupertypeOfCallingModule);
            var privateCalleeFunction = GetTestFunction(supertypeOfSupertypeOfCallingModule, "calleeFoo", Accessibility.Private);

            Assert.IsTrue(AccessibilityCheck.IsAccessible(projectDeclatation, callingModule, privateCallingFunction, privateCalleeFunction));
        }


        [TestMethod]
        public void PrivateProceduresAreNotAcessibleFromOtherUnrelatedModules()
        {
            var projectDeclatation = GetTestProject("test_project");
            var calleeModuleDeclatation = GetTestClassModule(projectDeclatation, "callee_test_Module");
            var calleeFunctionDeclaration = GetTestFunction(calleeModuleDeclatation, "calleeFoo", Accessibility.Private);
            var callingModuleDeclatation = GetTestClassModule(projectDeclatation, "calling_test_Module");
            var callingFunctionDeclaration = GetTestFunction(callingModuleDeclatation, "callingFoo", Accessibility.Private);

            Assert.IsFalse(AccessibilityCheck.IsAccessible(projectDeclatation, callingModuleDeclatation, callingFunctionDeclaration, calleeFunctionDeclaration));
        }


        [TestMethod]
        public void FriendProceduresAreAcessibleFromOtherModulesInTheSameProject()
        {
            var projectDeclatation = GetTestProject("test_project");
            var calleeModuleDeclatation = GetTestClassModule(projectDeclatation, "callee_test_Module");
            var friendFunctionDeclaration = GetTestFunction(calleeModuleDeclatation, "calleeFoo", Accessibility.Friend);
            var callingModuleDeclatation = GetTestClassModule(projectDeclatation, "calling_test_Module");
            var otherPrivateFunctionDeclaration = GetTestFunction(callingModuleDeclatation, "callingFoo", Accessibility.Private);

            Assert.IsTrue(AccessibilityCheck.IsAccessible(projectDeclatation, callingModuleDeclatation, otherPrivateFunctionDeclaration, friendFunctionDeclaration));
        }


        [TestMethod]
        public void FriendProceduresAreNotAcessibleFromOtherProjects()
        {
            var calleeProjectDeclatation = GetTestProject("callee_test_project");
            var calleeModuleDeclatation = GetTestClassModule(calleeProjectDeclatation, "callee_test_Module", true);
            var friendFunctionDeclaration = GetTestFunction(calleeModuleDeclatation, "calleeFoo", Accessibility.Friend);
            var callingProjectDeclatation = GetTestProject("calling_test_project");
            var callingModuleDeclatation = GetTestClassModule(callingProjectDeclatation, "calling_test_Module");
            var otherPrivateFunctionDeclaration = GetTestFunction(callingModuleDeclatation, "callingFoo", Accessibility.Private);

            Assert.IsFalse(AccessibilityCheck.IsAccessible(callingProjectDeclatation, callingModuleDeclatation, otherPrivateFunctionDeclaration, friendFunctionDeclaration));
        }


        [TestMethod]
        public void PublicProceduresInExposedClassModulesAreAcessibleFromOtherProjects()
        {
            var calleeProjectDeclatation = GetTestProject("callee_test_project");
            var calleeModuleDeclatation = GetTestClassModule(calleeProjectDeclatation, "callee_test_Module", true);
            var calleeFunctionDeclaration = GetTestFunction(calleeModuleDeclatation, "calleeFoo", Accessibility.Public);
            var callingProjectDeclatation = GetTestProject("calling_test_project");
            var callingModuleDeclatation = GetTestClassModule(callingProjectDeclatation, "calling_test_Module");
            var otherPrivateFunctionDeclaration = GetTestFunction(callingModuleDeclatation, "callingFoo", Accessibility.Private);

            Assert.IsTrue(AccessibilityCheck.IsAccessible(callingProjectDeclatation, callingModuleDeclatation, otherPrivateFunctionDeclaration, calleeFunctionDeclaration));
        }


        [TestMethod]
        public void PublicProceduresInNonPrivateProceduralModulesAreAcessibleFromOtherProjects()
        {
            var calleeProjectDeclatation = GetTestProject("callee_test_project");
            var calleeModuleDeclatation = GetTestProceduralModule(calleeProjectDeclatation, "callee_test_Module");
            var calleeFunctionDeclaration = GetTestFunction(calleeModuleDeclatation, "calleeFoo", Accessibility.Public);
            var callingProjectDeclatation = GetTestProject("calling_test_project");
            var callingModuleDeclatation = GetTestClassModule(callingProjectDeclatation, "calling_test_Module");
            var otherPrivateFunctionDeclaration = GetTestFunction(callingModuleDeclatation, "callingFoo", Accessibility.Private);

            Assert.IsTrue(AccessibilityCheck.IsAccessible(callingProjectDeclatation, callingModuleDeclatation, otherPrivateFunctionDeclaration, calleeFunctionDeclaration));
        }


        [TestMethod]
        public void ImplicitelyScopedProceduresInExposedClassModulesAreAcessibleFromOtherProjects()
        {
            var calleeProjectDeclatation = GetTestProject("callee_test_project");
            var calleeModuleDeclatation = GetTestClassModule(calleeProjectDeclatation, "callee_test_Module", true);
            var calleeFunctionDeclaration = GetTestFunction(calleeModuleDeclatation, "calleeFoo", Accessibility.Implicit);
            var callingProjectDeclatation = GetTestProject("calling_test_project");
            var callingModuleDeclatation = GetTestClassModule(callingProjectDeclatation, "calling_test_Module");
            var otherPrivateFunctionDeclaration = GetTestFunction(callingModuleDeclatation, "callingFoo", Accessibility.Private);

            Assert.IsTrue(AccessibilityCheck.IsAccessible(callingProjectDeclatation, callingModuleDeclatation, otherPrivateFunctionDeclaration, calleeFunctionDeclaration));
        }


        [TestMethod]
        public void ImplicitelyScopedProceduresInNonPrivateProceduralModulesAreAcessibleFromOtherProjects()
        {
            var calleeProjectDeclatation = GetTestProject("callee_test_project");
            var calleeModuleDeclatation = GetTestProceduralModule(calleeProjectDeclatation, "callee_test_Module");
            var calleeFunctionDeclaration = GetTestFunction(calleeModuleDeclatation, "calleeFoo", Accessibility.Implicit);
            var callingProjectDeclatation = GetTestProject("calling_test_project");
            var callingModuleDeclatation = GetTestClassModule(callingProjectDeclatation, "calling_test_Module");
            var otherPrivateFunctionDeclaration = GetTestFunction(callingModuleDeclatation, "callingFoo", Accessibility.Private);

            Assert.IsTrue(AccessibilityCheck.IsAccessible(callingProjectDeclatation, callingModuleDeclatation, otherPrivateFunctionDeclaration, calleeFunctionDeclaration));
        }



        //instance variable tests

        [TestMethod]
        public void PrivateInstanceVariablesAreAccessibleFromTheEnclosingModule()     
        {
            var projectDeclatation = GetTestProject("test_project");
            var moduleDeclatation = GetTestClassModule(projectDeclatation, "test_Module");
            var privateFunctionDeclaration = GetTestFunction(moduleDeclatation, "foo", Accessibility.Private);
            var privateInstanceVariable = GetTestVariable(moduleDeclatation, "x", Accessibility.Private);

            Assert.IsTrue(AccessibilityCheck.IsAccessible(projectDeclatation,moduleDeclatation,privateFunctionDeclaration,privateInstanceVariable));
        }

            private static Declaration GetTestVariable(Declaration parentDeclatation, string name, Accessibility variableAccessibility)
            {
                var qualifiedVariableMemberName = new QualifiedMemberName(StubQualifiedModuleName(), name);
                return new Declaration(qualifiedVariableMemberName, parentDeclatation, "dummy", "test", "test", false, false, variableAccessibility, DeclarationType.Variable, null, Selection.Home, false, null);
            }


        [TestMethod]
            public void PrivateInstanceVariablesAreAccessibleIfTheyAreInAClassAboveTheEnclosingModuleOfTheCallerInTheClassHierarchy()
        {
            var projectDeclatation = GetTestProject("test_project");
            var callingModule = GetTestClassModule(projectDeclatation, "callingModule");
            var privateCallingFunction = GetTestFunction(callingModule, "callingFoo", Accessibility.Private);
            var supertypeOfCallingModule = GetTestClassModule(projectDeclatation, "callingModuleSuper");
            callingModule.AddSupertype(supertypeOfCallingModule);
            var supertypeOfSupertypeOfCallingModule = GetTestClassModule(projectDeclatation, "callingModuleSuperSuper");
            supertypeOfCallingModule.AddSupertype(supertypeOfSupertypeOfCallingModule);
            var privateCalleeInstanceVariable = GetTestVariable(supertypeOfSupertypeOfCallingModule, "calleeFoo", Accessibility.Private);

            Assert.IsTrue(AccessibilityCheck.IsAccessible(projectDeclatation, callingModule, privateCallingFunction, privateCalleeInstanceVariable));
        }


        [TestMethod]
        public void PrivateInstanceVariablesAreNotAcessibleFromOtherUnrelatedModules()
        {
            var projectDeclatation = GetTestProject("test_project");
            var calleeModuleDeclatation = GetTestClassModule(projectDeclatation, "callee_test_Module");
            var instanceVariable = GetTestVariable(calleeModuleDeclatation, "x", Accessibility.Private);
            var callingModuleDeclatation = GetTestClassModule(projectDeclatation, "calliong_test_Module");
            var functionDeclaration = GetTestFunction(callingModuleDeclatation, "foo", Accessibility.Private);

            Assert.IsFalse(AccessibilityCheck.IsAccessible(projectDeclatation, callingModuleDeclatation, functionDeclaration, instanceVariable));
        }


        [TestMethod]
        public void PublicInstanceVariablesInExposedClassModulesAreAcessibleFromOtherProjects()
        {
            var calleeProjectDeclatation = GetTestProject("callee_test_project");
            var calleeModuleDeclatation = GetTestClassModule(calleeProjectDeclatation, "callee_test_Module", true);
            var instanceVariable = GetTestVariable(calleeModuleDeclatation, "x", Accessibility.Public);
            var callingProjectDeclatation = GetTestProject("calling_test_project");
            var callingModuleDeclatation = GetTestClassModule(callingProjectDeclatation, "calling_test_Module");
            var otherPrivateFunctionDeclaration = GetTestFunction(callingModuleDeclatation, "callingFoo", Accessibility.Private);

            Assert.IsTrue(AccessibilityCheck.IsAccessible(callingProjectDeclatation, callingModuleDeclatation, otherPrivateFunctionDeclaration, instanceVariable));
        }


        [TestMethod]
        public void PublicInstanceVariablesInNonPrivateProceduralModulesAreAcessibleFromTheSameProject()
        {
            var calleeProjectDeclatation = GetTestProject("callee_test_project");
            var calleeModuleDeclatation = GetTestProceduralModule(calleeProjectDeclatation, "callee_test_Module");
            var instanceVariable = GetTestVariable(calleeModuleDeclatation, "x", Accessibility.Public);
            var callingModuleDeclatation = GetTestClassModule(calleeProjectDeclatation, "calling_test_Module");
            var otherPrivateFunctionDeclaration = GetTestFunction(callingModuleDeclatation, "callingFoo", Accessibility.Private);

            Assert.IsTrue(AccessibilityCheck.IsAccessible(calleeProjectDeclatation, callingModuleDeclatation, otherPrivateFunctionDeclaration, instanceVariable));
        }


        [TestMethod] 
        public void PublicInstanceVariablesInNonPrivateProceduralModulesAreAcessibleFromOtherProjects()
        {
            var calleeProjectDeclatation = GetTestProject("callee_test_project");
            var calleeModuleDeclatation = GetTestProceduralModule(calleeProjectDeclatation, "callee_test_Module");
            var instanceVariable = GetTestVariable(calleeModuleDeclatation, "x", Accessibility.Public);
            var callingProjectDeclatation = GetTestProject("calling_test_project");
            var callingModuleDeclatation = GetTestClassModule(callingProjectDeclatation, "calling_test_Module");
            var otherPrivateFunctionDeclaration = GetTestFunction(callingModuleDeclatation, "callingFoo", Accessibility.Private);

            Assert.IsTrue(AccessibilityCheck.IsAccessible(callingProjectDeclatation, callingModuleDeclatation, otherPrivateFunctionDeclaration, instanceVariable));
        }


        [TestMethod] 
        public void GlobalInstanceVariablesInNonPrivateProceduralModulesAreAcessibleFromOtherProjects()
        {
            var calleeProjectDeclatation = GetTestProject("callee_test_project");
            var calleeModuleDeclatation = GetTestProceduralModule(calleeProjectDeclatation, "callee_test_Module");
            var instanceVariable = GetTestVariable(calleeModuleDeclatation, "x", Accessibility.Global);
            var callingProjectDeclatation = GetTestProject("calling_test_project");
            var callingModuleDeclatation = GetTestClassModule(callingProjectDeclatation, "calling_test_Module");
            var otherPrivateFunctionDeclaration = GetTestFunction(callingModuleDeclatation, "callingFoo", Accessibility.Private);

            Assert.IsTrue(AccessibilityCheck.IsAccessible(callingProjectDeclatation, callingModuleDeclatation, otherPrivateFunctionDeclaration, instanceVariable));
        }


        [TestMethod]
        public void ImplicitlyScopedInstanceVariablesAreAcessibleWithinTheirEnclosingModule()
        {
            var projectDeclatation = GetTestProject("test_project");
            var moduleDeclatation = GetTestClassModule(projectDeclatation, "test_Module");
            var privateFunctionDeclaration = GetTestFunction(moduleDeclatation, "foo", Accessibility.Private);
            var instanceVariable = GetTestVariable(moduleDeclatation, "x", Accessibility.Implicit);

            Assert.IsTrue(AccessibilityCheck.IsAccessible(projectDeclatation, moduleDeclatation, privateFunctionDeclaration, instanceVariable));
        }


        [TestMethod]
        public void ImplicitlyScopedInstanceVariablesAreNotAcessibleFromOtherModules()
        {
            var projectDeclatation = GetTestProject("test_project");
            var calleeModuleDeclatation = GetTestClassModule(projectDeclatation, "callee_test_Module");
            var instanceVariable = GetTestVariable(calleeModuleDeclatation, "x", Accessibility.Implicit);
            var callingModuleDeclatation = GetTestClassModule(projectDeclatation, "calliong_test_Module");
            var functionDeclaration = GetTestFunction(callingModuleDeclatation, "foo", Accessibility.Private);

            Assert.IsFalse(AccessibilityCheck.IsAccessible(projectDeclatation, callingModuleDeclatation, functionDeclaration, instanceVariable));
        }



        //local variable tests

        [TestMethod]
        public void LocalMembersAreAcessibleFromTheMethodTheyAreDefinedIn()
        {
            var projectDeclatation = GetTestProject("test_project");
            var moduleDeclatation = GetTestClassModule(projectDeclatation, "test_Module");
            var privateFunctionDeclaration = GetTestFunction(moduleDeclatation, "foo", Accessibility.Private);
            var localVariable = GetTestVariable(privateFunctionDeclaration, "x", Accessibility.Implicit);

            Assert.IsTrue(AccessibilityCheck.IsAccessible(projectDeclatation, moduleDeclatation, privateFunctionDeclaration, localVariable));
        }


        [TestMethod]
        public void LocalMembersAreNotAcessibleFromOtherMethods()
        {
            var projectDeclatation = GetTestProject("test_project");
            var moduleDeclatation = GetTestClassModule(projectDeclatation, "test_Module");
            var privateFunctionDeclaration = GetTestFunction(moduleDeclatation, "calleeFoo", Accessibility.Private);
            var localVariable = GetTestVariable(privateFunctionDeclaration, "x", Accessibility.Implicit);
            var otherPrivateFunctionDeclaration = GetTestFunction(moduleDeclatation, "callingFoo", Accessibility.Private);

            Assert.IsFalse(AccessibilityCheck.IsAccessible(projectDeclatation, moduleDeclatation, otherPrivateFunctionDeclaration, localVariable));
        }


        [TestMethod]
        public void StaticLocalMembersAreAcessibleFromTheMethodTheyAreDefinedIn()
        {
            var projectDeclatation = GetTestProject("test_project");
            var moduleDeclatation = GetTestClassModule(projectDeclatation, "test_Module");
            var privateFunctionDeclaration = GetTestFunction(moduleDeclatation, "foo", Accessibility.Private);
            var localVariable = GetTestVariable(privateFunctionDeclaration, "x", Accessibility.Static);

            Assert.IsTrue(AccessibilityCheck.IsAccessible(projectDeclatation, moduleDeclatation, privateFunctionDeclaration, localVariable));
        }


        [TestMethod]
        public void StaticLocalMembersAreNotAcessibleFromOtherMethods()
        {
            var projectDeclatation = GetTestProject("test_project");
            var moduleDeclatation = GetTestClassModule(projectDeclatation, "test_Module");
            var privateFunctionDeclaration = GetTestFunction(moduleDeclatation, "calleeFoo", Accessibility.Private);
            var localVariable = GetTestVariable(privateFunctionDeclaration, "x", Accessibility.Static);
            var otherPrivateFunctionDeclaration = GetTestFunction(moduleDeclatation, "callingFoo", Accessibility.Private);

            Assert.IsFalse(AccessibilityCheck.IsAccessible(projectDeclatation, moduleDeclatation, otherPrivateFunctionDeclaration, localVariable));
        }



        //enum tests

        [TestMethod]
        public void PrivateEnumsAreAccessibleInTheSameModule()
        {
            var calleeProjectDeclatation = GetTestProject("callee_test_project");
            var calleeModuleDeclatation = GetTestClassModule(calleeProjectDeclatation, "callee_test_Module", true);
            var enumDeclarartion = GetTestEnum(calleeModuleDeclatation, "x", Accessibility.Private);
            var functionDeclaration = GetTestFunction(calleeModuleDeclatation, "callingFoo", Accessibility.Private);

            Assert.IsTrue(AccessibilityCheck.IsAccessible(calleeProjectDeclatation, calleeModuleDeclatation, functionDeclaration, enumDeclarartion));
        }

            private static Declaration GetTestEnum(Declaration parentDeclatation, string name, Accessibility enumAccessibility)
            {
                var qualifiedVariableMemberName = new QualifiedMemberName(StubQualifiedModuleName(), name);
                return new Declaration(qualifiedVariableMemberName, parentDeclatation, "dummy", "test", "test", false, false, enumAccessibility, DeclarationType.Enumeration, null, Selection.Home, false, null);
            }


        [TestMethod]
        public void PrivateEnumsAreNotAccessibleFromOtherModules()
        {
            var calleeProjectDeclatation = GetTestProject("callee_test_project");
            var calleeModuleDeclatation = GetTestClassModule(calleeProjectDeclatation, "callee_test_Module", true);
            var enumDeclarartion = GetTestEnum(calleeModuleDeclatation, "x", Accessibility.Private);
            var callingModuleDeclatation = GetTestClassModule(calleeProjectDeclatation, "calling_test_Module");
            var functionDeclaration = GetTestFunction(callingModuleDeclatation, "callingFoo", Accessibility.Private);

            Assert.IsFalse(AccessibilityCheck.IsAccessible(calleeProjectDeclatation, callingModuleDeclatation, functionDeclaration, enumDeclarartion));
        }


        [TestMethod]
        public void PublicEnumsInNonPrivateProceduralModulesAreNotAcessibleFromOtherProjects()
        {
            var calleeProjectDeclatation = GetTestProject("callee_test_project");
            var calleeModuleDeclatation = GetTestProceduralModule(calleeProjectDeclatation, "callee_test_Module");
            var enumDeclarartion = GetTestEnum(calleeModuleDeclatation, "x", Accessibility.Public);
            var callingProjectDeclatation = GetTestProject("calling_test_project");
            var callingModuleDeclatation = GetTestClassModule(callingProjectDeclatation, "calling_test_Module");
            var functionDeclaration = GetTestFunction(callingModuleDeclatation, "callingFoo", Accessibility.Private);

            Assert.IsTrue(AccessibilityCheck.IsAccessible(callingProjectDeclatation, callingModuleDeclatation, functionDeclaration, enumDeclarartion));
        }


        [TestMethod]
        public void GlobalEnumsInNonPrivateProceduralModulesAreNotAcessibleFromOtherProjects()
        {
            var calleeProjectDeclatation = GetTestProject("callee_test_project");
            var calleeModuleDeclatation = GetTestProceduralModule(calleeProjectDeclatation, "callee_test_Module");
            var enumDeclarartion = GetTestEnum(calleeModuleDeclatation, "x", Accessibility.Global);
            var callingProjectDeclatation = GetTestProject("calling_test_project");
            var callingModuleDeclatation = GetTestClassModule(callingProjectDeclatation, "calling_test_Module");
            var functionDeclaration = GetTestFunction(callingModuleDeclatation, "callingFoo", Accessibility.Private);

            Assert.IsTrue(AccessibilityCheck.IsAccessible(callingProjectDeclatation, callingModuleDeclatation, functionDeclaration, enumDeclarartion));
        }


        [TestMethod]
        public void ImplicitelyScopedEnumsInNonPrivateProceduralModulesAreNotAcessibleFromOtherProjects()
        {
            var calleeProjectDeclatation = GetTestProject("callee_test_project");
            var calleeModuleDeclatation = GetTestProceduralModule(calleeProjectDeclatation, "callee_test_Module");
            var enumDeclarartion = GetTestEnum(calleeModuleDeclatation, "x", Accessibility.Implicit);
            var callingProjectDeclatation = GetTestProject("calling_test_project");
            var callingModuleDeclatation = GetTestClassModule(callingProjectDeclatation, "calling_test_Module");
            var functionDeclaration = GetTestFunction(callingModuleDeclatation, "callingFoo", Accessibility.Private);

            Assert.IsTrue(AccessibilityCheck.IsAccessible(callingProjectDeclatation, callingModuleDeclatation, functionDeclaration, enumDeclarartion));
        }



        //user type tests


        [TestMethod]
        public void PrivateUserTypesAreAccessibleInTheSameModule()
        {
            var calleeProjectDeclatation = GetTestProject("callee_test_project");
            var calleeModuleDeclatation = GetTestClassModule(calleeProjectDeclatation, "callee_test_Module", true);
            var userType = GetTestUserType(calleeModuleDeclatation, "x", Accessibility.Private);
            var functionDeclaration = GetTestFunction(calleeModuleDeclatation, "callingFoo", Accessibility.Private);

            Assert.IsTrue(AccessibilityCheck.IsAccessible(calleeProjectDeclatation, calleeModuleDeclatation, functionDeclaration, userType));
        }

            private static Declaration GetTestUserType(Declaration parentDeclatation, string name, Accessibility userTypeAccessibility)
            {
                var qualifiedVariableMemberName = new QualifiedMemberName(StubQualifiedModuleName(), name);
                return new Declaration(qualifiedVariableMemberName, parentDeclatation, "dummy", "test", "test", false, false, userTypeAccessibility, DeclarationType.UserDefinedType, null, Selection.Home, false, null);
            }


        [TestMethod]
        public void PrivateUserTypesAreNotAccessibleFromOtherModules()
        {
            var calleeProjectDeclatation = GetTestProject("callee_test_project");
            var calleeModuleDeclatation = GetTestClassModule(calleeProjectDeclatation, "callee_test_Module", true);
            var userType = GetTestUserType(calleeModuleDeclatation, "x", Accessibility.Private);
            var callingModuleDeclatation = GetTestClassModule(calleeProjectDeclatation, "calling_test_Module");
            var functionDeclaration = GetTestFunction(callingModuleDeclatation, "callingFoo", Accessibility.Private);

            Assert.IsFalse(AccessibilityCheck.IsAccessible(calleeProjectDeclatation, callingModuleDeclatation, functionDeclaration, userType));
        }


        [TestMethod]
        public void PublicUserTypesInNonPrivateProceduralModulesAreNotAcessibleFromOtherProjects()
        {
            var calleeProjectDeclatation = GetTestProject("callee_test_project");
            var calleeModuleDeclatation = GetTestProceduralModule(calleeProjectDeclatation, "callee_test_Module");
            var userType = GetTestUserType(calleeModuleDeclatation, "x", Accessibility.Public);
            var callingProjectDeclatation = GetTestProject("calling_test_project");
            var callingModuleDeclatation = GetTestClassModule(callingProjectDeclatation, "calling_test_Module");
            var functionDeclaration = GetTestFunction(callingModuleDeclatation, "callingFoo", Accessibility.Private);

            Assert.IsTrue(AccessibilityCheck.IsAccessible(callingProjectDeclatation, callingModuleDeclatation, functionDeclaration, userType));
        }


        [TestMethod]
        public void GlobalUserTypesInNonPrivateProceduralModulesAreNotAcessibleFromOtherProjects()
        {
            var calleeProjectDeclatation = GetTestProject("callee_test_project");
            var calleeModuleDeclatation = GetTestProceduralModule(calleeProjectDeclatation, "callee_test_Module");
            var userType = GetTestUserType(calleeModuleDeclatation, "x", Accessibility.Global);
            var callingProjectDeclatation = GetTestProject("calling_test_project");
            var callingModuleDeclatation = GetTestClassModule(callingProjectDeclatation, "calling_test_Module");
            var functionDeclaration = GetTestFunction(callingModuleDeclatation, "callingFoo", Accessibility.Private);

            Assert.IsTrue(AccessibilityCheck.IsAccessible(callingProjectDeclatation, callingModuleDeclatation, functionDeclaration, userType));
        }


        [TestMethod]
        public void ImplicitelyScopedUserTypesInNonPrivateProceduralModulesAreNotAcessibleFromOtherProjects()
        {
            var calleeProjectDeclatation = GetTestProject("callee_test_project");
            var calleeModuleDeclatation = GetTestProceduralModule(calleeProjectDeclatation, "callee_test_Module");
            var userType = GetTestUserType(calleeModuleDeclatation, "x", Accessibility.Implicit);
            var callingProjectDeclatation = GetTestProject("calling_test_project");
            var callingModuleDeclatation = GetTestClassModule(callingProjectDeclatation, "calling_test_Module");
            var functionDeclaration = GetTestFunction(callingModuleDeclatation, "callingFoo", Accessibility.Private);

            Assert.IsTrue(AccessibilityCheck.IsAccessible(callingProjectDeclatation, callingModuleDeclatation, functionDeclaration, userType));
        }


        //further tests (derived from code already present)

        [TestMethod]
        public void EnumMembersInNonPrivateProceduralModulesAreNotAcessibleFromOtherProjects()
        {
            var calleeProjectDeclatation = GetTestProject("callee_test_project");
            var calleeModuleDeclatation = GetTestProceduralModule(calleeProjectDeclatation, "callee_test_Module");
            var instanceVariable = GetTestEnumMember(calleeModuleDeclatation, "x");
            var callingProjectDeclatation = GetTestProject("calling_test_project");
            var callingModuleDeclatation = GetTestClassModule(callingProjectDeclatation, "calling_test_Module");
            var otherPrivateFunctionDeclaration = GetTestFunction(callingModuleDeclatation, "callingFoo", Accessibility.Private);

            Assert.IsTrue(AccessibilityCheck.IsAccessible(callingProjectDeclatation, callingModuleDeclatation, otherPrivateFunctionDeclaration, instanceVariable));
        }

            private static Declaration GetTestEnumMember(Declaration parentDeclatation, string name)
            {
                var qualifiedVariableMemberName = new QualifiedMemberName(StubQualifiedModuleName(), name);
                return new Declaration(qualifiedVariableMemberName, parentDeclatation, "dummy", "test", "test", false, false, Accessibility.Implicit, DeclarationType.EnumerationMember, null, Selection.Home, false, null);
            }
        

        [TestMethod]
        public void UserTypeMembersInExposedClassModulesAreAcessibleFromOtherProjects()
        {
            var calleeProjectDeclatation = GetTestProject("callee_test_project");
            var calleeModuleDeclatation = GetTestClassModule(calleeProjectDeclatation, "callee_test_Module", true);
            var instanceVariable = GetTestUserTypeMember(calleeModuleDeclatation, "x");
            var callingProjectDeclatation = GetTestProject("calling_test_project");
            var callingModuleDeclatation = GetTestClassModule(callingProjectDeclatation, "calling_test_Module");
            var otherPrivateFunctionDeclaration = GetTestFunction(callingModuleDeclatation, "callingFoo", Accessibility.Private);

            Assert.IsTrue(AccessibilityCheck.IsAccessible(callingProjectDeclatation, callingModuleDeclatation, otherPrivateFunctionDeclaration, instanceVariable));
        }

            private static Declaration GetTestUserTypeMember(Declaration parentDeclatation, string name)
            {
                var qualifiedVariableMemberName = new QualifiedMemberName(StubQualifiedModuleName(), name);
                return new Declaration(qualifiedVariableMemberName, parentDeclatation, "dummy", "test", "test", false, false, Accessibility.Implicit, DeclarationType.UserDefinedTypeMember, null, Selection.Home, false, null);
            }


        [TestMethod]
        public void UserTypeMembersInNonPrivateProceduralModulesAreNotAcessibleFromOtherProjects()
        {
            var calleeProjectDeclatation = GetTestProject("callee_test_project");
            var calleeModuleDeclatation = GetTestProceduralModule(calleeProjectDeclatation, "callee_test_Module");
            var instanceVariable = GetTestUserTypeMember(calleeModuleDeclatation, "x");
            var callingProjectDeclatation = GetTestProject("calling_test_project");
            var callingModuleDeclatation = GetTestClassModule(callingProjectDeclatation, "calling_test_Module");
            var otherPrivateFunctionDeclaration = GetTestFunction(callingModuleDeclatation, "callingFoo", Accessibility.Private);

            Assert.IsTrue(AccessibilityCheck.IsAccessible(callingProjectDeclatation, callingModuleDeclatation, otherPrivateFunctionDeclaration, instanceVariable));
        }


        [TestMethod]
        public void AccessibilityCheckDoesNotTakeIntoAccountThatAMemberMightNotBeAccessibleBecauseItIsCoveredByAnotherMemberInNarrowerScope()
        {
            var projectDeclatation = GetTestProject("test_project");
            var moduleDeclatation = GetTestClassModule(projectDeclatation, "test_Module");
            var privateInstanceVariable = GetTestVariable(moduleDeclatation, "x", Accessibility.Private);
            var privateFunctionDeclaration = GetTestFunction(moduleDeclatation, "foo", Accessibility.Private);
            var localVariable = GetTestVariable(privateFunctionDeclaration, "x", Accessibility.Implicit);   //This variable makes the instance variable of the same name inaccessible inside the function.

            Assert.IsTrue(AccessibilityCheck.IsAccessible(projectDeclatation, moduleDeclatation, privateFunctionDeclaration, privateInstanceVariable));
        }



        //null reference handling tests
        
        [TestMethod]
        public void CalleesWhichAreNullAreNotAcessibleFromOtherProjects()
        {
            var callingProjectDeclatation = GetTestProject("calling_test_project");
            var callingModuleDeclatation = GetTestClassModule(callingProjectDeclatation, "calling_test_Module");
            var callingFunctionDeclaration = GetTestFunction(callingModuleDeclatation, "callingFoo", Accessibility.Private);

            Assert.IsFalse(AccessibilityCheck.IsAccessible(callingProjectDeclatation, callingModuleDeclatation, callingFunctionDeclaration, null));
        }


        [TestMethod]
        public void CalleeModulesWhichAreNullAreNotAcessibleFromOtherProjects()
        {
            var callingProjectDeclatation = GetTestProject("calling_test_project");
            var callingModuleDeclatation = GetTestClassModule(callingProjectDeclatation, "calling_test_Module");
            var otherPrivateFunctionDeclaration = GetTestFunction(callingModuleDeclatation, "callingFoo", Accessibility.Private);

            Assert.IsFalse(AccessibilityCheck.IsModuleAccessible(callingProjectDeclatation, callingModuleDeclatation, null));
        }


        [TestMethod]
        public void CalleeMembersWhichAreNullAreNotAcessibleFromOtherProjects()
        {
            var callingProjectDeclatation = GetTestProject("calling_test_project");
            var callingModuleDeclatation = GetTestClassModule(callingProjectDeclatation, "calling_test_Module");
            var callingFunctionDeclaration = GetTestFunction(callingModuleDeclatation, "callingFoo", Accessibility.Private);

            Assert.IsFalse(AccessibilityCheck.IsMemberAccessible(callingProjectDeclatation, callingModuleDeclatation, callingFunctionDeclaration, null));
        }


    }
}
