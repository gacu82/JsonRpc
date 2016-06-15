using Newtonsoft.Json.Linq;

namespace JsonRpc.Host
{
    public interface IRequestHookService
    {
        void Process(JToken request);
    }
}