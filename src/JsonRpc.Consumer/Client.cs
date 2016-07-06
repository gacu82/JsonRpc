using JsonRpc.Commons;
using JsonRpc.Commons.Exceptions;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace JsonRpc.Client
{
    public class JsonRpcClient
    {
        private readonly ILogger logger;
        private readonly Encoding utf8EncodingWithoutBom = new UTF8Encoding(false);
        private readonly TimeSpan timeout;
        private readonly IRequestFactory requestFactory;

        public JsonRpcClient(IRequestFactory requestFactory, TimeSpan timeout, ILogger<JsonRpcClient> logger)
        {
            this.requestFactory = requestFactory;
            this.timeout = timeout;
            this.logger = logger;
        }

        public async Task<RpcResponse> CallAsync(Uri uri, object id, string method, object @params, string service = null)
        {
            return await this.CallAsync(uri, new RpcCall(method, id, @params), service);
        }

        public async Task<RpcResponse<T>> CallAsync<T>(Uri uri, RpcCall call, string service = null)
        {
            var response = await this.CallAsync(uri, call, service);
            T resultObject = default(T);
            if(response.Result != null) resultObject = JToken.FromObject(response.Result).ToObject<T>(); // FIXME
            return new RpcResponse<T>(resultObject, response.Id, response.Error);
        }

        public async Task<RpcResponse> CallAsync(Uri uri, RpcCall call, string service = null)
        {
            return await CallAsyncInner(uri, call, service).TimeoutAfter(this.timeout);
        }

        private async Task<RpcResponse> CallAsyncInner(Uri uri, RpcCall call, string service = null)
        {
            if (!string.IsNullOrWhiteSpace(service))
            {
                uri = new Uri(uri, service);
            }
            var requestString = RpcSerializer.ToJson(call);
            logger.LogTrace($"Request {uri} {requestString}");
            try
            {
                var request = this.requestFactory.CreateHttp(uri);
                request.ContentType = "application/json";
                request.Headers["Accept-Charset"] = "utf-8";
                request.Method = "POST";
                using (var requestStream = await request.GetRequestStreamAsync())
                {
                    var requestBytes = utf8EncodingWithoutBom.GetBytes(requestString);
                    await requestStream.WriteAsync(requestBytes, 0, requestBytes.Length);
                }
                var response = await request.GetResponseAsync();
                using (var responseStream = response.GetResponseStream())
                {
                    using (TextReader reader = new StreamReader(responseStream, utf8EncodingWithoutBom))
                    {
                        var responseString = await reader.ReadToEndAsync();
                        logger.LogTrace($"Response {uri} {responseString}");
                        var jToken = JToken.Parse(responseString);
                        if (!this.IsValidJsonRpcResponse(jToken))
                        {
                            throw new JsonRpcCommunicationException(
                                "Invalid JSONRPC in response", uri.ToString(), responseString);
                        }
                        return jToken.ToObject<RpcResponse>();
                    }
                }
            }
            catch (Exception ex)
            {
                throw new JsonRpcCommunicationException(
                    "Request error. Check inner exception for details",
                    uri.ToString(), requestString, ex);
            }
        }

        private bool IsValidJsonRpcResponse(JToken response)
        {
            return (response.Type == JTokenType.Object && response["jsonrpc"]?.ToString() == "2.0") &&
                   (response["result"] != null || response["error"] != null);
        }
    }

    public class JsonRpcCommunicationException : Exception
    {
        public JsonRpcCommunicationException(
            string message,
            string remoteServiceName = null,
            string reqObjString = null,
            Exception innerException = null
        )
            : base(message, innerException)
        {
            this.RemoteServiceName = remoteServiceName;
            this.RequestObjString = reqObjString;
        }

        public string RemoteServiceName { get; }
        public string RequestObjString { get; }
    }
}