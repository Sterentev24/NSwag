using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using Newtonsoft.Json.Linq;
using NSwag.Commands.Commands.Split;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata;

namespace NSwag.Commands.Split
{
    static internal class JObjectExtensions
    {
        public static (string key, JToken token) GetByPath(this JObject jsonObject, string path)
        {
            var pathTokens = path.Replace("#/", string.Empty).Split('/');
            JToken result = jsonObject;
            string key = null;

            foreach ( var token in pathTokens)
            {
                result = result[token];
                key = token;
            }

            return (key, result);
        }        

        public static void ResolvePropertiesByRefName(this JToken token, JObject document, List<(string key, JToken token)> result)
        {
            if (token == null)
            {
                return;
            }

            var processReference = (JProperty property) =>
            {
                var def = document.GetByPath(property.Value.ToString());
                if (def.key != null && !result.Any(it => it.key == def.key))
                {
                    result.Add(def);
                    def.token.ResolvePropertiesByRefName(document, result);
                }
            };

            if (token.Type == JTokenType.Object)
            {
                var jObject = (JObject)token;
                foreach (var property in jObject.Properties())
                {
                    switch(property.Name)
                    {
                        case DocumentKeys.RefKey:
                            processReference(property);
                            break;
                        case DocumentKeys.Mapping:
                            var refs = property.Value<JToken>().Value<JToken>().Values<JToken>().Values<JProperty>();
                            foreach(var r in refs)
                            {
                                processReference(r);
                            }
                            break;

                        default: 
                            ResolvePropertiesByRefName(property.Value, document, result);
                            break;
                    }
                }
            }
            
            else if (token.Type == JTokenType.Array)
            {
                foreach (var item in token.Children())
                {
                    ResolvePropertiesByRefName(item, document, result); 
                }
            }
        }
    }
}

