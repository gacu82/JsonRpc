using JsonRpc.Commons;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;

namespace JsonRpc.Host
{
    internal enum RpcType
    {
        Unknown,
        Call,
        Notification
    }

    internal class RpcCallContext
    {
        public RpcCallContext(JToken json)
        {
            this.CallType = RpcType.Unknown;
            this.Parameters = new List<object>();
            this.RawRequestJson = json;
        }
        public string MethodName { get; set; }
        public RpcMethod RegistryEntry { get; set; }
        public List<object> Parameters { get; set; }
        public RpcType CallType { get; set; }
        public object Id { get; set; }
        public RpcError Error { get; set; }
        public object Result { get; set; }
        public JToken RawRequestJson { get; set; }
    }
}