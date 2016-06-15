using System;
using System.Linq;
using System.Collections.Concurrent;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;

namespace JsonRpc.Host
{
    internal class RpcMethodRegister
    {

        private ConcurrentDictionary<string, RpcMethod> register = new ConcurrentDictionary<string, RpcMethod>();
        private readonly ILogger logger;
        private bool assembliesScanned = false;

        public RpcMethodRegister(ILogger logger)
        {
            this.logger = logger;
        }

        public void ScanAssemblies(Assembly[] assemblies)
        {
            foreach (Assembly a in assemblies)
            {
                foreach (Type t in a.GetTypes())
                {
                    var typeInfo = t.GetTypeInfo();
                    if (!typeInfo.IsClass || typeInfo.IsAbstract) continue;
                    foreach (var memberInfo in t.GetMethods(BindingFlags.Public | BindingFlags.Instance))
                    {
                        var attrib = memberInfo.GetCustomAttributes(typeof(JsonRpcMethodAttribute), true).FirstOrDefault();
                        if (attrib == null) continue;
                        var rpcAttrib = (JsonRpcMethodAttribute)attrib;
                        var methodInfo = t.GetMethod(memberInfo.Name);
                        var name = rpcAttrib.MethodName ?? memberInfo.Name;
                        var service = rpcAttrib.ServiceName;
                        var key = this.GetServiceMethodKey(name, service);
                        this.register.AddOrUpdate(key, new RpcMethod(t, methodInfo), (s, e) => { return e; });
                    }
                }
            }

            this.assembliesScanned = true;
        }

        public RpcMethod GetMethodEntry(string name, string service)
        {
            if (!this.assembliesScanned) throw new InvalidOperationException("Assemblies not scanned");
            RpcMethod entry = null;
            var key = this.GetServiceMethodKey(name, service);
            if (this.register.TryGetValue(key, out entry))
            {
                return entry;
            }
            else
            {
                return null;
            }
        }

        public List<KeyValuePair<string, RpcMethod>> GetSortedEntries()
        {
            return this.register.OrderBy(x => x.Key).ToList();
        }

        private string GetServiceMethodKey(string methodName, string serviceName)
        {
            return $"{serviceName}/{methodName}".ToLower();
        }

    }
}
