
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NSwag.CodeGeneration.TypeScript;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace NSwag.Commands.Split
{
    using TypeReference = (string fromTypeName, string toTypeName, JToken token);
    internal class TypeReferenceResolver
    {
        public List<TypeReference> ResolvePropertiesByRefName(JToken token, JObject document)
        {
            var result = new List<TypeReference>();
            ResolvePropertiesByRefName(token, document, null, result);
            return result;
        }

        private static void ResolvePropertiesByRefName(JToken token, JObject document, string toTypeName, List<TypeReference> result)
        {
            if (token == null)
            {
                return;
            }

            var processReference = (JProperty property) =>
            {
                var (typeName, token) = document.GetByPath(property.Value.ToString());
                if (typeName != null && !result.Any(it => it.toTypeName == typeName))
                {
                    result.Add((typeName, toTypeName, token));
                    ResolvePropertiesByRefName(token, document, typeName, result);
                }
            };

            if (token.Type == JTokenType.Object)
            {
                var jObject = (JObject)token;
                foreach (var property in jObject.Properties())
                {
                    switch (property.Name)
                    {
                        case DocumentKeys.RefKey:
                            processReference(property);
                            break;
                        case DocumentKeys.Mapping:
                            var refs = property.Value<JToken>().Value<JToken>().Values<JToken>().Values<JProperty>();
                            foreach (var r in refs)
                            {
                                processReference(r);
                            }
                            break;

                        default:
                            ResolvePropertiesByRefName(property.Value, document, toTypeName, result);
                            break;
                    }
                }
            }

            else if (token.Type == JTokenType.Array)
            {
                foreach (var item in token.Children())
                {
                    ResolvePropertiesByRefName(item, document, toTypeName, result);
                }
            }
        }


    }
}
