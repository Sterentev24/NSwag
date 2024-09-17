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

            if (token.Type == JTokenType.Object)
            {
                var jObject = (JObject)token;
                foreach (var property in jObject.Properties())
                {
                    if (property.Name == DocumentKeys.RefKey)
                    {
                        var def = document.GetByPath(property.Value.ToString());
                        if (def.key != null && !result.Any(it => it.key == def.key))
                        {
                            result.Add(def);
                            def.token.ResolvePropertiesByRefName(document, result);
                        }
                    }
                    else
                    {
                        ResolvePropertiesByRefName(property.Value, document, result); 
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

