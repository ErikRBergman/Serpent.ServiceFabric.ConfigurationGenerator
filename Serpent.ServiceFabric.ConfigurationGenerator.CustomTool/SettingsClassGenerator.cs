// This code was originally taken from https://github.com/Microsoft/VSSDK-Extensibility-Samples/tree/master/Single_File_Generator but modified

namespace Serpent.ServiceFabric.ConfigurationGenerator.CustomTool
{
    using System;
    using System.CodeDom.Compiler;
    using System.IO;
    using System.Runtime.InteropServices;
    using System.Text;
    using System.Xml.Linq;
    using System.Xml.Schema;

    using Microsoft.Samples.VisualStudio.GeneratorSample;
    using Microsoft.VisualStudio.Shell;

    using Serpent.ServiceFabric.ConfigurationGenerator.Logic;

    using VSLangProj80;

    [ComVisible(true)]
    [Guid("cc4ccba5-fb13-4de6-b512-c5be2381ed5d")]
    [CodeGeneratorRegistration(typeof(SettingsClassGenerator), "C# XML Class Generator", vsContextGuids.vsContextGuidVCSProject, GeneratesDesignTimeSource = true)]
    [CodeGeneratorRegistration(typeof(SettingsClassGenerator), "VB XML Class Generator", vsContextGuids.vsContextGuidVBProject, GeneratesDesignTimeSource = true)]
    [CodeGeneratorRegistration(typeof(SettingsClassGenerator), "J# XML Class Generator", vsContextGuids.vsContextGuidVJSProject, GeneratesDesignTimeSource = true)]
    [ProvideObject(typeof(SettingsClassGenerator))]
    public class SettingsClassGenerator : BaseCodeGeneratorWithSite
    {
#pragma warning disable 0414

        // The name of this generator (use for 'Custom Tool' property of project item)
        internal static string name = "SettingsClassGenerator";
#pragma warning restore 0414

        internal static bool validXML;

        /// <summary>
        ///     Function that builds the contents of the generated file based on the contents of the input file
        /// </summary>
        /// <param name="inputFileContent">Content of the input file</param>
        /// <returns>Generated file as a byte array</returns>
        protected override byte[] GenerateCode(string inputFileContent)
        {
            var provider = this.GetCodeProvider();

            try
            {
                var reader = XDocument.Parse(inputFileContent);
                var sections = SettingsParser.GetSettings(reader);

                // Create the CodeCompileUnit from the passed-in XML file
                var compileUnit = SourceCodeGenerator.CreateCodeCompileUnit(sections, this.FileNameSpace);

                if (this.CodeGeneratorProgress != null)
                {
                    // Report that we are 1/2 done
                    this.CodeGeneratorProgress.Progress(50, 100);
                }

                using (var writer = new StringWriter(new StringBuilder()))
                {
                    var options = new CodeGeneratorOptions
                                      {
                                          BlankLinesBetweenMembers = false,
                                          BracingStyle = "C"
                                      };

                    // Generate the code
                    provider.GenerateCodeFromCompileUnit(compileUnit, writer, options);

                    // Report that we are done
                    this.CodeGeneratorProgress?.Progress(100, 100);

                    writer.Flush();

                    // Get the Encoding used by the writer. We're getting the WindowsCodePage encoding, 
                    // which may not work with all languages
                    var enc = Encoding.GetEncoding(writer.Encoding.WindowsCodePage);

                    // Get the preamble (byte-order mark) for our encoding
                    var preamble = enc.GetPreamble();
                    var preambleLength = preamble.Length;

                    // Convert the writer contents to a byte array
                    var body = enc.GetBytes(writer.ToString());

                    // Prepend the preamble to body (store result in resized preamble array)
                    Array.Resize(ref preamble, preambleLength + body.Length);
                    Array.Copy(body, 0, preamble, preambleLength, body.Length);

                    // Return the combined byte array
                    return preamble;
                }
            }
            catch (Exception e)
            {
                this.GeneratorError(4, e.ToString(), 1, 1);

                // Returning null signifies that generation has failed
                return null;
            }
        }
    }
}