/***************************************************************************

Copyright (c) Microsoft Corporation. All rights reserved.
This code is licensed under the Visual Studio SDK license terms.
THIS CODE IS PROVIDED *AS IS* WITHOUT WARRANTY OF
ANY KIND, EITHER EXPRESS OR IMPLIED, INCLUDING ANY
IMPLIED WARRANTIES OF FITNESS FOR A PARTICULAR
PURPOSE, MERCHANTABILITY, OR NON-INFRINGEMENT.

***************************************************************************/

namespace Serpent.ServiceFabric.ConfigurationGenerator.CustomTool
{
    using System;
    using System.CodeDom;
    using System.Collections.Generic;
    using System.Reflection;

    using Serpent.ServiceFabric.ConfigurationGenerator.Logic.Models;

    // In order to be compatible with this single file generator, the input file has to
    // follow the schema in XMLClassGeneratorSchema.xsd

    /// <summary>
    /// Generates source code based on a XML document
    /// </summary>
    public static class SourceCodeGenerator
    {
        /// <summary>
        /// Create a CodeCompileUnit based on the XmlDocument doc
        /// In order to be compatible with this single file generator, the input XmlDocument has to
        /// follow the schema in XMLClassGeneratorSchema.xsd
        /// </summary>
        /// <param name="sections">An XML document that contains the description of the code to be generated</param>
        /// <param name="namespaceName">If the root node of doc does not have a namespace attribute, use this instead</param>
        /// <returns>The generated CodeCompileUnit</returns>
        public static CodeCompileUnit CreateCodeCompileUnit(IEnumerable<Section> sections, string namespaceName)
        {

            CodeCompileUnit code = new CodeCompileUnit();

            // Just for VB.NET:
            // Option Strict On (controls whether implicit type conversions are allowed)
            code.UserData.Add("AllowLateBound", false);

            // Option Explicit On (controls whether variable declarations are required)
            code.UserData.Add("RequireVariableDeclaration", true);

            CodeNamespace codeNamespace = new CodeNamespace(namespaceName);

            codeNamespace.Imports.Add(new CodeNamespaceImport("System.Fabric"));
            codeNamespace.Imports.Add(new CodeNamespaceImport("System.Fabric.Description"));
            codeNamespace.Imports.Add(new CodeNamespaceImport("System.Security"));
            codeNamespace.Imports.Add(new CodeNamespaceImport("Microsoft.ServiceFabric.Services.Runtime"));

            foreach (var section in sections)
            {
                codeNamespace.Types.Add(CreateConfigurationSectionClass(section));
                codeNamespace.Types.Add(CreateConfigurationSectionValuesClass(section));
            }

            codeNamespace.Types.Add(CreateConfigurationClass(sections));
            codeNamespace.Types.Add(CreateConfigurationExtensionsClass(sections));

            code.Namespaces.Add(codeNamespace);
            return code;
        }

        private static CodeTypeDeclaration CreateConfigurationExtensionsClass(IEnumerable<Section> sections)
        {
            string className = "ConfigurationExtensions";

            CodeTypeDeclaration typeDeclaration = new CodeTypeDeclaration(className)
            {
                IsClass = true,
                TypeAttributes = TypeAttributes.Public | TypeAttributes.Class,
            };
            
            typeDeclaration.StartDirectives.Add(new CodeRegionDirective(
                CodeRegionMode.Start, className + Environment.NewLine + "\tstatic"));

            typeDeclaration.EndDirectives.Add(new CodeRegionDirective(
                CodeRegionMode.End, string.Empty));

            typeDeclaration.Members.Add(CreateExtensionMethod("this StatelessService", "service", ".Context"));
            typeDeclaration.Members.Add(CreateExtensionMethod("this StatefulService", "service", ".Context"));

            typeDeclaration.Members.Add(CreateExtensionMethod("this StatefulServiceContext", "service"));
            typeDeclaration.Members.Add(CreateExtensionMethod("this StatelessServiceContext", "service"));


            ////typeDeclaration.Members.Add(CreateExtensionMethod("this Actor", "actor"));

            return typeDeclaration;
        }

        private static CodeMemberMethod CreateExtensionMethod(string parameterTypeName, string parameterName, string parameterProperty = null, string conditionCompilationSymbol = null)
        {
            CodeMemberMethod member = new CodeMemberMethod()
                                          {
                                              Attributes = MemberAttributes.Public | MemberAttributes.Static,
                                              Name = "Configuration",
                                              ReturnType = new CodeTypeReference("Configuration")
                                          };

            member.Parameters.Add(new CodeParameterDeclarationExpression(parameterTypeName, parameterName));

            var parameterPropertyValue = parameterProperty != null ? "." + parameterProperty : string.Empty;

            member.Statements.Add(new CodeSnippetExpression($"return new Configuration({parameterName}{parameterPropertyValue})"));

            return member;
        }

        private static CodeTypeDeclaration CreateConfigurationClass(IEnumerable<Section> sections)
        {
            string className = "Configuration";

            CodeTypeDeclaration typeDeclaration = new CodeTypeDeclaration(className)
            {
                IsClass = true,
                TypeAttributes = TypeAttributes.Public | TypeAttributes.Class
            };

            CodeMemberField sectionField = new CodeMemberField("ServiceContext", "context")
            {
                Attributes = MemberAttributes.Private,
            };

            typeDeclaration.Members.Add(sectionField);

            // Constructor
            CodeConstructor ctor = new CodeConstructor()
            {
                Attributes = MemberAttributes.Public,
                Name = className,
                ReturnType = new CodeTypeReference(typeof(void))
            };

            ctor.Parameters.Add(new CodeParameterDeclarationExpression("ServiceContext", "context"));
            ctor.Statements.Add(new CodeSnippetExpression("this.context = context"));
            typeDeclaration.Members.Add(ctor);

            // sections
            foreach (var section in sections)
            {
                var name = section.Name + "Values";

                CodeMemberProperty property = new CodeMemberProperty()
                {
                    Attributes = MemberAttributes.Public | MemberAttributes.Final,
                    Name = name,
                    Type = new CodeTypeReference(name)
                };

                property.GetStatements.Add(new CodeSnippetExpression($"return new {name}(this.context.CodePackageActivationContext.GetConfigurationPackageObject(\"Config\"))"));
                typeDeclaration.Members.Add(property);

                name = section.Name;

                property = new CodeMemberProperty
                {
                    Attributes = MemberAttributes.Public | MemberAttributes.Final,
                    Name = name,
                    Type = new CodeTypeReference(name)
                };

                property.GetStatements.Add(new CodeSnippetExpression($"return new {name}(this.context.CodePackageActivationContext.GetConfigurationPackageObject(\"Config\"))"));
                typeDeclaration.Members.Add(property);


            }

            return typeDeclaration;
        }

        private static CodeTypeDeclaration CreateConfigurationSectionClass(Section section)
        {
            string className = section.Name;
            CodeTypeDeclaration typeDeclaration = new CodeTypeDeclaration(className)
            {
                IsClass = true,
                TypeAttributes = TypeAttributes.Public | TypeAttributes.Class
            };

            CodeMemberField sectionField = new CodeMemberField("ConfigurationSection", "section")
            {
                Attributes = MemberAttributes.Private,
            };

            typeDeclaration.Members.Add(sectionField);

            // Constructor
            CodeConstructor ctor = new CodeConstructor()
            {
                Attributes = MemberAttributes.Public,
                Name = className,
                ReturnType = new CodeTypeReference(typeof(void))
            };

            ctor.Parameters.Add(new CodeParameterDeclarationExpression("ConfigurationPackage", "package"));
            ctor.Statements.Add(new CodeSnippetExpression($"this.section = package.Settings.Sections[\"{section.Name}\"]"));
            typeDeclaration.Members.Add(ctor);

            // Constructor
            ctor = new CodeConstructor()
            {
                Attributes = MemberAttributes.Public,
                Name = className,
                ReturnType = new CodeTypeReference(typeof(void))
            };

            ctor.Parameters.Add(new CodeParameterDeclarationExpression("ConfigurationSection", "section"));
            ctor.Statements.Add(new CodeSnippetExpression("this.section = section"));
            typeDeclaration.Members.Add(ctor);

            // properties
            foreach (var parameter in section.Parameters)
            {
                CodeMemberProperty property = new CodeMemberProperty()
                {
                    Attributes = MemberAttributes.Public | MemberAttributes.Final,
                    Name = parameter.Name,
                    Type = new CodeTypeReference("ConfigurationProperty")
                };

                property.GetStatements.Add(new CodeSnippetExpression($"return this.section.Parameters[\"{parameter.Name}\"]"));

                typeDeclaration.Members.Add(property);
            }

            return typeDeclaration;
        }



        private static CodeTypeDeclaration CreateConfigurationSectionValuesClass(Section section)
        {
            string className = section.Name + "Values";
            CodeTypeDeclaration typeDeclaration = new CodeTypeDeclaration(className)
            {
                IsClass = true,
                TypeAttributes = TypeAttributes.Public | TypeAttributes.Class
            };

            CodeMemberField sectionField = new CodeMemberField("ConfigurationSection", "section")
            {
                Attributes = MemberAttributes.Private,
            };

            typeDeclaration.Members.Add(sectionField);

            // Constructor
            CodeConstructor ctor = new CodeConstructor()
            {
                Attributes = MemberAttributes.Public,
                Name = className,
                ReturnType = new CodeTypeReference(typeof(void))
            };

            ctor.Parameters.Add(new CodeParameterDeclarationExpression("ConfigurationPackage", "package"));
            ctor.Statements.Add(new CodeSnippetExpression($"this.section = package.Settings.Sections[\"{section.Name}\"]"));
            typeDeclaration.Members.Add(ctor);

            // Constructor
            ctor = new CodeConstructor()
            {
                Attributes = MemberAttributes.Public,
                Name = className,
                ReturnType = new CodeTypeReference(typeof(void))
            };

            ctor.Parameters.Add(new CodeParameterDeclarationExpression("ConfigurationSection", "section"));
            ctor.Statements.Add(new CodeSnippetExpression("this.section = section"));
            typeDeclaration.Members.Add(ctor);

            // properties
            foreach (var parameter in section.Parameters)
            {
                CodeMemberProperty property = new CodeMemberProperty()
                {
                    Attributes = MemberAttributes.Public | MemberAttributes.Final,
                    Name = parameter.Name,
                    Type = new CodeTypeReference(typeof(string))
                };

                if (parameter.IsEncrypted)
                {
                    property.Type = new CodeTypeReference("SecureString");
                    property.GetStatements.Add(new CodeSnippetExpression($"return this.section.Parameters[\"{parameter.Name}\"].DecryptValue()"));

                }
                else
                {
                    property.GetStatements.Add(new CodeSnippetExpression($"return this.section.Parameters[\"{parameter.Name}\"].Value"));
                }

                typeDeclaration.Members.Add(property);
            }

            return typeDeclaration;
        }

    }
}