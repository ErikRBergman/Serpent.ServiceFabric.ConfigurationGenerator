namespace Serpent.ServiceFabric.ConfigurationGenerator.Logic
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Xml;
    using System.Xml.Linq;

    using Serpent.ServiceFabric.ConfigurationGenerator.Logic.Models;

    public static class SettingsParser
    {
        public static IEnumerable<Section> GetSettings(XDocument reader)
        {
            var settings = reader.Root;

            if (settings == null || settings.Name.LocalName != "Settings")
            {
                throw new Exception($"XML document is not a valid Settings.xml file");
            }

            var sections = new List<Section>();

            foreach (var section in settings.Nodes().Where(n => n.NodeType == XmlNodeType.Element).Cast<XElement>().Where(e => e.Name.LocalName == "Section"))
            {
                var nameAttribute = section.Attribute("Name")?.Value;

                if (string.IsNullOrWhiteSpace(nameAttribute))
                {
                    continue;
                }

                var newSection = new Section
                                     {
                                         Name = nameAttribute
                                     };

                sections.Add(newSection);

                var parameters = new List<Parameter>();

                foreach (var parameter in section.Nodes().Where(n => n.NodeType == XmlNodeType.Element).Cast<XElement>().Where(e => e.Name.LocalName == "Parameter"))
                {
                    parameters.Add(
                        new Parameter
                            {
                                Name = parameter.Attribute("Name")?.Value,
                                CustomType = parameter.Attribute("CustomType")?.Value,
                                IsEncrypted = string.Compare("true", parameter.Attribute("IsEncrypted")?.Value ?? "false", StringComparison.OrdinalIgnoreCase) == 0
                            });
                }

                newSection.Parameters = parameters;
            }

            return sections;
        }
    }
}