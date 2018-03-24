namespace Serpent.ServiceFabric.ConfigurationGenerator.Logic
{
    using System.Collections.Generic;
    using System.Text;

    using Serpent.ServiceFabric.ConfigurationGenerator.Logic.Models;

    public static class ConfigCreator
    {
        public static void CreateConfigurationClass(StringBuilder text, IEnumerable<Section> sections)
        {
            text.AppendLine($"    public class Configuration");
            text.AppendLine("    {");

            text.AppendLine("        private readonly ServiceContext context;");
            text.AppendLine();

            text.AppendLine($"        public Configuration(ServiceContext context)");
            text.AppendLine("        {");
            text.AppendLine($"            this.context = context;");
            text.AppendLine("        }");

            foreach (var section in sections)
            {
                text.AppendLine();
                text.AppendLine(
                    $"        public {section.Name} {section.Name} => new {section.Name}(this.context.CodePackageActivationContext.GetConfigurationPackageObject(\"Config\"));");

                text.AppendLine();
                text.AppendLine(
                    $"        public {section.Name}Values {section.Name}Values => new {section.Name}Values(this.context.CodePackageActivationContext.GetConfigurationPackageObject(\"Config\"));");
            }

            // end of class
            text.AppendLine("    }");
        }

        public static void CreateConfigurationExtensionsClass(StringBuilder text, IEnumerable<Section> sections)
        {
            text.AppendLine($"    public static class ConfigurationExtensions");
            text.AppendLine("    {");

            foreach (var section in sections)
            {
                text.AppendLine($"        public static Configuration Configuration(this StatelessService service)");
                text.AppendLine("        {");
                text.AppendLine($"            return new Configuration(service.Context);");
                text.AppendLine("        }");
            }

            // end of class
            text.AppendLine("    }");

            // end of namespace
            text.AppendLine("}");
        }

        public static void CreateConfigurationSectionClass(StringBuilder text, Section section)
        {
            text.AppendLine($"    public class {section.Name}");
            text.AppendLine("    {");

            text.AppendLine("        private readonly ConfigurationSection section;");
            text.AppendLine();

            text.AppendLine($"        public {section.Name}(ConfigurationPackage package)");
            text.AppendLine("        {");
            text.AppendLine($"            this.section = package.Settings.Sections[\"{section.Name}\"];");
            text.AppendLine("        }");

            text.AppendLine();

            text.AppendLine($"        public {section.Name}(ConfigurationSection section)");
            text.AppendLine("        {");
            text.AppendLine("            this.section = section;");
            text.AppendLine("        }");

            foreach (var parameter in section.Parameters)
            {
                text.AppendLine();

                text.AppendLine($"        public ConfigurationProperty {parameter.Name} => this.section.Parameters[\"{parameter.Name}\"];");
            }

            // end of class
            text.AppendLine("    }");
        }

        public static void CreateConfigurationSectionValuesClass(StringBuilder text, Section section)
        {
            text.AppendLine($"    public class {section.Name}Values");
            text.AppendLine("    {");

            text.AppendLine("        private readonly ConfigurationSection section;");
            text.AppendLine();

            text.AppendLine($"        public {section.Name}Values(ConfigurationPackage package)");
            text.AppendLine("        {");
            text.AppendLine($"            this.section = package.Settings.Sections[\"{section.Name}\"];");
            text.AppendLine("        }");

            text.AppendLine();

            text.AppendLine($"        public {section.Name}Values(ConfigurationSection section)");
            text.AppendLine("        {");
            text.AppendLine("            this.section = section;");
            text.AppendLine("        }");

            foreach (var parameter in section.Parameters)
            {
                text.AppendLine();

                if (parameter.IsEncrypted)
                {
                    text.AppendLine($"        public SecureString {parameter.Name} => this.section.Parameters[\"{parameter.Name}\"].DecryptValue();");
                }
                else
                {
                    text.AppendLine($"        public string {parameter.Name} => this.section.Parameters[\"{parameter.Name}\"].Value;");
                }
            }

            // end of class
            text.AppendLine("    }");
        }

        public static string GetSettingsCSharpClass(IEnumerable<Section> sections)
        {
            var text = new StringBuilder(1024);

            text.AppendLine("namespace Configuration");
            text.AppendLine("{");

            text.AppendLine("    using System.Fabric;");
            text.AppendLine("    using System.Fabric.Description;");
            text.AppendLine("    using System.Security;");
            text.AppendLine("    using Microsoft.ServiceFabric.Services.Runtime;");

            // Create the normal sections
            text.AppendLine();
            foreach (var section in sections)
            {
                ConfigCreator.CreateConfigurationSectionClass(text, section);
            }

            // Create the the values sections
            text.AppendLine();
            foreach (var section in sections)
            {
                ConfigCreator.CreateConfigurationSectionValuesClass(text, section);
            }

            // The configuration class
            text.AppendLine();
            ConfigCreator.CreateConfigurationClass(text, sections);

            // And now the extension class
            text.AppendLine();
            ConfigCreator.CreateConfigurationExtensionsClass(text, sections);

            return text.ToString();
        }
    }
}