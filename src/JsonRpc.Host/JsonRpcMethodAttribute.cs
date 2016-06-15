using System;

namespace JsonRpc.Host
{
    public class JsonRpcMethodAttribute : Attribute
    {
        public JsonRpcMethodAttribute(string methodName = null, string serviceName = null)
        {
            this.MethodName = methodName;
            this.ServiceName = serviceName;
        }

        public string MethodName { get; }
        public string ServiceName { get; }
    }
}
