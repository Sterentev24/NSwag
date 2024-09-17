using Newtonsoft.Json.Linq;
using System.IO;

namespace NSwag.Commands.Split
{
    internal class JsonReader
    {
        public JObject Read(string jsonPath)
        {
            var buffer = File.ReadAllText(jsonPath);
            return JObject.Parse(buffer);
        }
    }
}
