using JsonRpc.Commons;
using JsonRpc.Commons.Exceptions;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using System;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace JsonRpc.Client
{
    public class RpcClient : IRpcClient
    {
        private readonly ILogger loggerRequests;
        private readonly ILogger loggerDiagnostics;
        private readonly Encoding utf8EncodingWithoutBom = new UTF8Encoding(false);
        private readonly IRequestFactory requestFactory;

        public static TimeSpan DefaultTimeout => TimeSpan.FromSeconds(60);

        public RpcClient(ILoggerFactory loggerFactory, IRequestFactory requestFactory=null, TimeSpan? timeout=null)
        {
            this.requestFactory = requestFactory ?? new DefaultRequestFactory();
            this.requestFactory.Timeout = timeout ?? DefaultTimeout;
            this.loggerRequests = loggerFactory.CreateLogger("JsonRpc.Client.Requests");
            this.loggerDiagnostics = loggerFactory.CreateLogger("JsonRpc.Client.Diagnostics");
        }

        public async Task<RpcResponse> CallAsync(Uri uri, string method, object id, object @params, string service = null)
        {
            return await this.CallAsyncInner(uri, new RpcCall(method, id, @params), service);
        }

        public async Task<RpcResponse<T>> CallAsync<T>(Uri uri, RpcCall call, string service = null)
        {
            var response = await this.CallAsyncInner(uri, call, service);
            T resultObject = default(T);
            if(response.Result != null) resultObject = JToken.FromObject(response.Result).ToObject<T>(); // FIXME
            return new RpcResponse<T>(resultObject, response.Id, response.Error);
        }

        private async Task<RpcResponse> CallAsyncInner(Uri uri,
            RpcCall call,
            string service = null)
        {
            var watch = Stopwatch.StartNew();
            if (!string.IsNullOrWhiteSpace(service))
            {
                uri = new Uri(uri, service);
            }
            var requestString = RpcSerializer.ToJson(call);
            string responseString = null;
            DateTime startDate = DateTime.Now;
            try
            {
                var content = new StringContent(requestString, Encoding.UTF8);
                content.Headers.ContentType = MediaTypeHeaderValue.Parse("application/json; charser=utf-8");
                var response = await this.requestFactory.PostAsync(uri, content);//, cancellationTokenSource.Token);
                response.EnsureSuccessStatusCode();
                responseString = await response.Content.ReadAsStringAsync();
                var jToken = JToken.Parse(responseString);
                if (!this.IsValidJsonRpcResponse(jToken))
                {
                    throw new JsonRpcCommunicationException(
                        "Invalid JSONRPC in response", uri.ToString(), responseString);
                }
                return jToken.ToObject<RpcResponse>();
            }
            catch (Exception ex)
            {
                throw new JsonRpcCommunicationException(
                    "Request error. Check inner exception for details",
                    uri.ToString(), requestString, ex);
            }
            finally
            {
                var processTime = watch.ElapsedMilliseconds;
                this.loggerRequests.LogInformation("startDate: {startDate} request: {request} response: {response} processTime: {processTime}",
                    startDate, requestString, responseString, processTime);
                this.loggerDiagnostics.LogInformation("startDate {startDate} method: {method} processTime: {processTime}",
                    startDate, call.Method, processTime);
            }
        }

        private bool IsValidJsonRpcResponse(JToken response)
        {
            return (response.Type == JTokenType.Object && response["jsonrpc"]?.ToString() == "2.0") &&
                   (response["result"] != null || response["error"] != null);
        }
    }
}