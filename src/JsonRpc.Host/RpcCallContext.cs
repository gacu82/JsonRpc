using JsonRpc.Commons;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;

namespace JsonRpc.Host
{
    internal enum RpcCallType
    {
        Unknown,
        Call,
        Notification
    }

    internal class RpcCallContext
    {
        public RpcCallContext()
        {
            this.CallType = RpcCallType.Unknown;
            this.Parameters = new List<object>();
            this.Diagnostics = new Diagnostics();
        }
        public string MethodName { get; set; }
        public RpcMethod RegistryEntry { get; set; }
        public List<object> Parameters { get; set; }
        public RpcCallType CallType { get; set; }
        public object Id { get; set; }
        public RpcError Error { get; set; }
        public object Result { get; set; }
        public JToken RawRequestJson { get; set; }
        public Diagnostics Diagnostics { get; }
    }
}