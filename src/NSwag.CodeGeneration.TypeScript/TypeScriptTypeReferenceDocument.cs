using Newtonsoft.Json;
using System.Collections.Generic;
using System.IO;

namespace NSwag.CodeGeneration.TypeScript
{
    public class TypeScriptTypeReferenceDocument
    {
        public List<TypeScriptTypeReferenceInfo> References { get; set; } = new List<TypeScriptTypeReferenceInfo>();

        public void Save(string path)
        {
            var buf = JsonConvert.SerializeObject(this);
            File.WriteAllText(path, buf);
        }

        public static TypeScriptTypeReferenceDocument LoadFrom(string path)
        {
            var buf = File.ReadAllText(path);
            return JsonConvert.DeserializeObject<TypeScriptTypeReferenceDocument>(buf);
        }
    }
}
