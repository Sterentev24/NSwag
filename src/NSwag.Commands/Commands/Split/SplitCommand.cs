using NConsole;
using Newtonsoft.Json.Linq;
using NSwag.CodeGeneration.TypeScript;
using System;
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

            var referenceResolver = new TypeReferenceResolver();

            var pathGroups = document[DocumentKeys.Paths].Values<JProperty>().GroupBy(p => getApiKey(p)).Where(g => g.Key != null);
            foreach (var group in pathGroups)
            {
                host.WriteMessage($"Current Path: {group.Key}{Environment.NewLine}");
                var newDocument = document.DeepClone();
                var properties = group.ToArray();
                newDocument[DocumentKeys.Paths] = new JObject(properties);
                var newSchema = new JObject();
                var refDocument = new TypeScriptTypeReferenceDocument();
                foreach (var property in properties)
                {                    
                    var references = referenceResolver.ResolvePropertiesByRefName(property.Value, document);                                        
                    foreach (var reference in references)
                    {   
                        if (reference.fromTypeName != null && reference.fromTypeName != reference.toTypeName)
                        {
                            var it = refDocument.References.FirstOrDefault(x => x.FromTypeName == reference.fromTypeName && x.ToTypeName == reference.toTypeName);
                            if (it == null)
                            {
                                refDocument.References.Add(new TypeScriptTypeReferenceInfo { FromTypeName = reference.fromTypeName, ToTypeName = reference.toTypeName });
                            }                            
                            newSchema[reference.fromTypeName] = reference.token;
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

                var openApiToTypeScriptClient = nswagDocument[DocumentKeys.CodeGenerators][DocumentKeys.OpenApiToTypeScriptClient];
                openApiToTypeScriptClient[DocumentKeys.Output] = new JValue($"{basePath}Client.ts");                
                openApiToTypeScriptClient[DocumentKeys.ExtractEverySchemaTypeToFile] = new JValue(true);
                var typReferenceMapPath = $"{basePath}.tref";
                openApiToTypeScriptClient[DocumentKeys.TypeReferenceMapPath] = new JValue(typReferenceMapPath);
                nswagDocument[DocumentKeys.DocumentGenerator][DocumentKeys.FromDocument][DocumentKeys.Url] = new JValue($"{basePath}.json");                

                File.WriteAllText($"{basePath}.json", newDocument.ToString());
                File.WriteAllText($"{basePath}.nswag", nswagDocument.ToString());
                refDocument.Save(typReferenceMapPath);
            }

            return await Task.FromResult(Task.CompletedTask);
        }
    }
}
