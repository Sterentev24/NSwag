using Newtonsoft.Json.Linq;

namespace NSwag.Commands.Split
{
    static internal class JObjectExtensions
    {
        public static (string typeName, JToken token) GetByPath(this JObject jsonObject, string path)
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
    }
}

