namespace Serpent.ServiceFabric.ConfigurationGenerator
{
    using System;
    using System.IO;
    using System.Linq;
    using System.Xml.Linq;

    using Serpent.ServiceFabric.ConfigurationGenerator.Logic;

    internal class Program
    {
        private static string GetOutputDirectory(string outputFile, string settingsFile)
        {
            var directory = Path.GetDirectoryName(outputFile);

            if (Path.IsPathRooted(directory) == false)
            {
                directory = Path.Combine(Path.GetDirectoryName(settingsFile), directory);
            }

            return directory;
        }

        private static void Main(string[] args)
        {
            if (args.Length != 2)
            {
                Console.WriteLine("Usage: serpent.servicefabric.configurationgenerator {path to settings.xml} {path to generated csharp file} ");
                return;
            }

            var settingsFile = args[0];
            var outputFile = args[1];

            var reader = XDocument.Load(settingsFile);
            var sections = SettingsParser.GetSettings(reader);

            if (sections.Any())
            {
                var outputText = ConfigCreator.GetSettingsCSharpClass(sections);

                // Get the output directory and filename
                var directory = GetOutputDirectory(outputFile, settingsFile);
                outputFile = Path.Combine(directory, Path.GetFileName(outputFile));

                // Ensure the directory exists
                Directory.CreateDirectory(directory);

                File.WriteAllText(outputFile, outputText);
            }
        }
    }
}