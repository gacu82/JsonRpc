using System;
using System.Threading.Tasks;
using JsonRpc.Commons;

namespace JsonRpc.Client
{
    public interface IRpcClient
    {
        Task<RpcResponse> CallAsync(Uri uri, string method, object id, object @params, string service = null);
        Task<RpcResponse<T>> CallAsync<T>(Uri uri, RpcCall call, string service = null);
    }
}