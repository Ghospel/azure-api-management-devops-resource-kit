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

namespace Microsoft.Azure.Management.ApiManagement.ArmTemplates.Merge
{
    public class MergeCommand : CommandLineApplication
    {
        public MergeCommand()
        {
            this.Name = GlobalConstants.MergeName;
            this.Description = GlobalConstants.MergeDescription;

            var inputFilesOption = this.Option("--inputFile <inputFile>", "ARM template input", CommandOptionType.MultipleValue).IsRequired();
            var outputFileOption = this.Option("--outputFile <outputFile>", "ARM template output", CommandOptionType.SingleValue).IsRequired();

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

                        return fileReader.ReadToEnd();
                    });

                    var templates = inputFilesData.Select(file => JObject.Parse(file)).ToList();

                    if (templates.Count == 0)
                    {
                        throw new InvalidOperationException("No JSON files found.");
                    }

                    var templateCreator = new TemplateCreator();

                    var mergedTemplate = templateCreator.CreateEmptyTemplate();
                    mergedTemplate.parameters = new Dictionary<string, TemplateParameterProperties>();

                    var resources = new List<JToken>();

                    foreach (var template in templates)
                    {
                        foreach (var parameter in template.GetValue("parameters").ToObject<Dictionary<string, TemplateParameterProperties>>())
                        {
                            if (!mergedTemplate.parameters.ContainsKey(parameter.Key))
                            {
                                mergedTemplate.parameters.Add(parameter.Key, parameter.Value);
                            }
                        }

                        resources.AddRange(template.GetValue("resources"));
                    }

                    var dynamicTemplate = JsonConvert.DeserializeObject<JObject>(
                        JsonConvert.SerializeObject(
                            mergedTemplate, 
                            new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore }));

                    dynamicTemplate.Remove("resources");
                    dynamicTemplate.Add("resources", JArray.FromObject(resources));

                    var fileWriter = new FileWriter();

                    fileWriter.WriteJSONToFile(dynamicTemplate, outputFileOption.Value());
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
