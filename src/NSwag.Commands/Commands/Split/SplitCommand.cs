using Microsoft.Extensions.Hosting;
using NConsole;
using Newtonsoft.Json.Linq;
using NSwag.Commands.Commands.Split;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace NSwag.Commands.Split
{
    [Command(Name = "split", Description = "Split a swagger.json into separated files ewith an own client.")]
    public class SplitCommand : IConsoleCommand
    {
        [Description("Path to swagger.json file.")]
        [Argument(Name = "InputFile", IsRequired = true)]
        public string InputFile { get; set; }

        [Description("Path to .nswag file.")]
        [Argument(Name = "InputNSwagFile", IsRequired = true)]
        public string InputNSwagFile { get; set; }

        [Description("Path to a directory for generated files")]
        [Argument(Name = "OutputDirectory", IsRequired = true)]
        public string OutputDirectory { get; set; }

        public async Task<object> RunAsync(CommandLineProcessor processor, IConsoleHost host)
        {
            if (Directory.Exists(OutputDirectory))
            {
                Directory.Delete(OutputDirectory, true);
            }
            Directory.CreateDirectory(OutputDirectory);

            if (!File.Exists(InputFile))
            {
                throw new FileNotFoundException(InputFile);
            }

            if (!File.Exists(InputNSwagFile))
            {
                throw new FileNotFoundException(InputNSwagFile);
            }

            var document = new JsonReader().Read(InputFile);

            var getApiKey = (JProperty p) =>
            {
                var segments = p.Name.TrimStart('/').Split('/');
                return segments.Length > 1 ? segments[1] : null;
            };

            var pathGroups = document[DocumentKeys.Paths].Values<JProperty>().GroupBy(p => getApiKey(p)).Where(g => g.Key != null);
            foreach (var group in pathGroups)
            {
                host.WriteMessage($"Current Path: {group.Key}{Environment.NewLine}");
                var newDocument = document.DeepClone();
                var properties = group.ToArray();
                newDocument[DocumentKeys.Paths] = new JObject(properties);

                var newSchema = new JObject();
                var references = new List<(string key, JToken token)>();
                foreach (var property in properties)
                {   
                    property.Value.ResolvePropertiesByRefName(document, references);
                    foreach (var reference in references)
                    {
                        if (reference.key != null)
                        {
                            newSchema[reference.key] = reference.token;
                        }
                    }
                }

                newDocument[DocumentKeys.Components][DocumentKeys.Schemas] = newSchema;

                var basePath = $"{OutputDirectory.TrimEnd('/').TrimEnd('\\')}\\{group.Key}";                

                var nswagDocument = new JsonReader().Read(InputNSwagFile);
                if (nswagDocument[DocumentKeys.CodeGenerators] == null || nswagDocument[DocumentKeys.CodeGenerators][DocumentKeys.OpenApiToTypeScriptClient] == null)
                {
                    throw new ArgumentOutOfRangeException(".nswag");
                }

                nswagDocument[DocumentKeys.CodeGenerators][DocumentKeys.OpenApiToTypeScriptClient][DocumentKeys.Output] = new JValue($"{group.Key}.ts");
                nswagDocument[DocumentKeys.DocumentGenerator][DocumentKeys.FromDocument][DocumentKeys.Url] = new JValue($"{group.Key}.json");

                File.WriteAllText($"{basePath}.json", newDocument.ToString());
                File.WriteAllText($"{basePath}.nswag", nswagDocument.ToString());
            }

            return await Task.FromResult(Task.CompletedTask);
        }
    }
}
