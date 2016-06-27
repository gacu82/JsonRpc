using System.Collections.Generic;
using System.Reflection;
using Newtonsoft.Json;

namespace JsonRpc.Host
{
    public class JsonRpcHostOptions
    {
        public JsonSerializerSettings SerializerSettings { get; set; }
        public IEnumerable<Assembly> AssembliesToScan { get; set; }
    }
}
