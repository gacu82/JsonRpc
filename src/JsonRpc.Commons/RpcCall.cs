namespace JsonRpc.Commons
{
    public class RpcCall
    {
        public RpcCall(string method, object id = null, object @params = null)
        {
            this.Method = method;
            this.Params = @params;
            this.Id = id;
        }
        public string Method { get; }
        public object Params { get; }
        public object Id { get; }
    }
}
