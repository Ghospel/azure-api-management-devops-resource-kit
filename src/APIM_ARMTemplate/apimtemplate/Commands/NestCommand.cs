using McMaster.Extensions.CommandLineUtils;
using Microsoft.Azure.Management.ApiManagement.ArmTemplates.Common;
using Microsoft.Azure.Management.ApiManagement.ArmTemplates.Create;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Microsoft.Azure.Management.ApiManagement.ArmTemplates.Link
{
    public class NestCommand : CommandLineApplication
    {
        public NestCommand()
        {
            this.Name = GlobalConstants.NestName;
            this.Description = GlobalConstants.NestDescription;

            var inputFilesOption = this.Option("--inputFile <inputFile>", "ARM template input", CommandOptionType.MultipleValue).IsRequired();
            var outputFileOption = this.Option("--outputFile <outputFile>", "Nested ARM template output", CommandOptionType.SingleValue).IsRequired();

            this.HelpOption();

            this.OnExecute(() =>
            {
                try
                {
                    var inputFiles = inputFilesOption.Values;

                    var inputFilesData = inputFiles.Select(file =>
                    {
                        using var fileStream = File.OpenRead(file);
                        using var fileReader = new StreamReader(fileStream);

                        return (fileName: Path.GetFileName(file).Replace(Path.GetExtension(file), ""), json: fileReader.ReadToEnd());
                    });

                    var templates = inputFilesData.Select(data => (data.fileName, template: JObject.Parse(data.json))).ToList();

                    if (templates.Count == 0)
                    {
                        throw new InvalidOperationException("No JSON files found.");
                    }

                    var templateCreator = new MasterTemplateCreator();

                    var linkedTemplate = templateCreator.CreateEmptyTemplate();
                    linkedTemplate.parameters = new Dictionary<string, TemplateParameterProperties>();

                    var resources = new List<NestedTemplateResource>();
                    var dependsOn = new List<string>();

                    var fileWriter = new FileWriter();

                    foreach ((string fileName, JObject template) in templates)
                    {
                        var parameters = template.GetValue("parameters").ToObject<Dictionary<string, TemplateParameterProperties>>();

                        var resource = templateCreator.CreateNestedMasterTemplateResource(
                            fileName, 
                            dependsOn.ToArray(),
                            template,
                            parameters);
                        

                        foreach (var parameter in parameters)
                        {
                            if (!linkedTemplate.parameters.ContainsKey(parameter.Key))
                            {
                                linkedTemplate.parameters.Add(parameter.Key, parameter.Value);
                            }
                        }

                        dependsOn.Add($"[resourceId('Microsoft.Resources/deployments', '{fileName}')]");
                        resources.Add(resource);
                    }

                    linkedTemplate.resources = resources.ToArray();

                    fileWriter.WriteJSONToFile(linkedTemplate, outputFileOption.Value());
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Error occured: " + ex.Message);
                    throw;
                }
            });
        }
    }
}
