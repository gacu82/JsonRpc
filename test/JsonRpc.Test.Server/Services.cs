using JsonRpc.Host;

namespace JsonRpc.Test.Server
{
    public class Services
    {
        [JsonRpcMethod]
        public int Add(int a, int b)
        {
            return a + b;
        }
    }
}
