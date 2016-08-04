using JsonRpc.Host;
using System;
using System.Threading;

namespace JsonRpc.Test.Server
{
    public class Services
    {
        [JsonRpcMethod]
        public int Add(int a, int b)
        {
            Thread.Sleep(50 + new Random().Next(0, 200));
            return a + b;
        }
    }
}
