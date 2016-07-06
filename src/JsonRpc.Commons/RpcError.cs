using System;
using JsonRpc.Commons.Exceptions;
using Newtonsoft.Json;

namespace JsonRpc.Commons
{
    public class RpcError
    {
        [JsonConstructor]
        public RpcError(int code, string message, object data = null)
        {
            this.Code = code;
            this.Message = message;
            this.Data = data;
        }

        public RpcError(Enum code, string message, object data = null)
        {
            this.Code = Convert.ToInt32(code);
            this.Message = message;
            this.Data = data;
        }

        public int Code { get; }
        public string Message { get; }
        public object Data { get; }

        public static RpcError FromException(JsonRpcException ex)
        {
            return new RpcError(ex.ErrorCode, ex.ErrorMessage, ex.ErrorData);

        }
    }
}
