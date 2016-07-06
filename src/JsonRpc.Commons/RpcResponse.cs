namespace JsonRpc.Commons
{
    public class RpcResponse
    {
        public RpcResponse(object result, object id = null, RpcError error = null)
        {
            this.Result = result;
            this.Error = error;
            this.Id = id;
        }

        public RpcError Error { get; }
        public object Result { get; }
        public object Id { get; }
    }

    public class RpcResponse<T>
    {
        public RpcResponse(T result, object id = null, RpcError error = null)
        {
            this.Result = result;
            this.Error = error;
            this.Id = id;
        }

        public RpcError Error { get; }
        public T Result { get; }
        public object Id { get; }
    }
}
