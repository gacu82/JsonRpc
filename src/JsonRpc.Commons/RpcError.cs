using JsonRpc.Commons.Exceptions;

namespace JsonRpc.Commons
{
    public class RpcError
    {
        public RpcError(int code, string message, object data = null)
        {
            this.Code = code;
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
