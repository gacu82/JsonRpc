using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;
using System;
using System.Reflection;
using System.Threading.Tasks;
using Xunit;
using Microsoft.Extensions.Logging;
using JsonRpc.Commons.Exceptions;
using JsonRpc.Commons;

namespace JsonRpc.Host.Tests
{
    public class Test
    {
        private readonly JsonRpcProcessor processor;

        public Test()
        {
            var loggerFactory = new LoggerFactory();
            processor = new JsonRpcProcessor(loggerFactory.CreateLogger("JsonRpc.Host"),
                loggerFactory.CreateLogger("JsonRpc.Host.Diagnostics"), null);
            JsonRpcProcessor.Instance.Configure(new JsonRpcHostOptions()
            {
                AssembliesToScan = new[] { typeof(Test).GetTypeInfo().Assembly }
            });
            JsonConvert.DefaultSettings = () => new JsonSerializerSettings()
            {
                ContractResolver = new CamelCasePropertyNamesContractResolver()
            };
        }

        [Fact]
        public async Task DateTimeCall()
        {
            var request = @"{'jsonrpc': '2.0', 'method': 'dateTimeMethod', 'params': [], 'id': 1}";
            var resp = await processor.ProcessAsync(request);
            var response = JObject.Parse(resp);
        }

        [Fact]
        public async Task TestProperCall()
        {
            var request = @"{'jsonrpc': '2.0', 'method': 'add', 'params': [1,2], 'id': 1}";
            var resp = await processor.ProcessAsync(request);
            var response = JObject.Parse(resp);
            Assert.Equal(response["jsonrpc"].Value<string>(), "2.0");
            Assert.Equal(response["result"].Value<int>(), 3);
            Assert.Equal(response["id"].Type, JTokenType.Integer);
            Assert.Equal(response["id"].Value<int>(), 1);
        }

        [Fact]
        public async Task TestProperCallParamsByName()
        {
            var request = @"{'jsonrpc': '2.0', 'method': 'add', 'params': {'a': 1, 'b': 2}, 'id': 1}";
            var resp = await processor.ProcessAsync(request);
            var response = JObject.Parse(resp);
            Assert.Equal(response["jsonrpc"].Value<string>(), "2.0");
            Assert.Equal(response["result"].Value<int>(), 3);
            Assert.Equal(response["id"].Type, JTokenType.Integer);
            Assert.Equal(response["id"].Value<int>(), 1);
        }

        [Fact]
        public async Task TestNonJson()
        {
            var request = "something that is not json";
            var response = JObject.Parse(await processor.ProcessAsync(request));
            Assert.Equal(response["jsonrpc"].Value<string>(), "2.0");
            Assert.Equal(response["error"]["code"].Value<int>(), -32700);
        }

        [Fact]
        public async Task TestNonRquestObject()
        {
            var request = "[1,2,3]";
            var resp = await processor.ProcessAsync(request);
            var response = JArray.Parse(resp);
            Assert.Equal(3, response.Count);
            Assert.Equal(response[0]["jsonrpc"].Value<string>(), "2.0");
            Assert.Equal(response[0]["error"]["code"].Value<int>(), -32600);
            Assert.Equal(response[1]["jsonrpc"].Value<string>(), "2.0");
            Assert.Equal(response[1]["error"]["code"].Value<int>(), -32600);
            Assert.Equal(response[2]["jsonrpc"].Value<string>(), "2.0");
            Assert.Equal(response[2]["error"]["code"].Value<int>(), -32600);
        }

        [Fact]
        public async Task TestMethodNotFound()
        {
            var request = @"{'jsonrpc': '2.0', 'method': 'somemethod', 'id': 1}";
            var response = JObject.Parse(await processor.ProcessAsync(request));
            Assert.Equal(response["jsonrpc"].Value<string>(), "2.0");
            Assert.Equal(response["error"]["code"].Value<int>(), -32601);
            Assert.Equal(response["id"].Type, JTokenType.Integer);
            Assert.Equal(response["id"].Value<int>(), 1);
        }

        [Fact]
        public async Task TestBadParamCount()
        {
            var request = @"{'jsonrpc': '2.0', 'method': 'add', 'params': [1,2,3], 'id': 1}";
            var response = JObject.Parse(await processor.ProcessAsync(request));
            Assert.Equal(response["jsonrpc"].Value<string>(), "2.0");
            Assert.Equal(response["error"]["code"].Value<int>(), -32602);
            Assert.Equal(response["id"].Type, JTokenType.Integer);
            Assert.Equal(response["id"].Value<int>(), 1);
        }

        [Fact]
        public async Task TestBadParamType()
        {
            var request = @"{'jsonrpc': '2.0', 'method': 'add', 'params': [1,'a'], 'id': 1}";
            var response = JObject.Parse(await processor.ProcessAsync(request));
            Assert.Equal(response["jsonrpc"].Value<string>(), "2.0");
            Assert.Equal(response["error"]["code"].Value<int>(), -32602);
            Assert.Equal(response["id"].Type, JTokenType.Integer);
            Assert.Equal(response["id"].Value<int>(), 1);
        }

        [Fact]
        public async Task TestNoParamsAndStringId()
        {
            var request = @"{'jsonrpc': '2.0', 'method': 'add', 'id': '1'}";
            var response = JObject.Parse(await processor.ProcessAsync(request));
            Assert.Equal(response["jsonrpc"].Value<string>(), "2.0");
            Assert.Equal(response["error"]["code"].Value<int>(), -32602);
            Assert.Equal(response["id"].Type, JTokenType.String);
            Assert.Equal(response["id"].Value<string>(), "1");
        }

        [Fact]
        public async Task TestCrash()
        {
            var request = @"{'jsonrpc': '2.0', 'method': 'crash', 'id': 1}";
            var response = JObject.Parse(await processor.ProcessAsync(request));
            Assert.Equal(response["jsonrpc"].Value<string>(), "2.0");
            Assert.Equal(response["error"]["code"].Value<int>(), -32603);
            Assert.Equal(response["id"].Type, JTokenType.Integer);
            Assert.Equal(response["id"].Value<int>(), 1);
        }

        [Fact]
        public async Task TestNotification()
        {
            var request = @"{'jsonrpc': '2.0', 'method': 'add', 'params': [1,2]}";
            var response = await processor.ProcessAsync(request);
            Assert.Equal(null, response);
        }

        [Fact]
        public async Task TestBatch()
        {
            var request = @"[{'jsonrpc': '2.0', 'method': 'add', 'params': [1,2], 'id': 1}, {'jsonrpc': '2.0', 'method': 'sub', 'params': [1,2], 'id': 2}]";
            var resp = await processor.ProcessAsync(request);
            var response = JArray.Parse(resp);
            Assert.Equal(response[0]["jsonrpc"].Value<string>(), "2.0");
            Assert.Equal(response[0]["result"].Value<int>(), 3);
            Assert.Equal(response[0]["id"].Type, JTokenType.Integer);
            Assert.Equal(response[0]["id"].Value<int>(), 1);
            Assert.Equal(response[1]["jsonrpc"].Value<string>(), "2.0");
            Assert.Equal(response[1]["result"].Value<int>(), -1);
            Assert.Equal(response[1]["id"].Type, JTokenType.Integer);
            Assert.Equal(response[1]["id"].Value<int>(), 2);
        }

        [Fact]
        public async Task TestBatchWithCrash()
        {
            var request = @"[{'jsonrpc': '2.0', 'method': 'add', 'params': [1,2], 'id': 1}, {'jsonrpc': '2.0', 'method': 'crash', 'id': 2}]";
            var resp = await processor.ProcessAsync(request);
            var response = JArray.Parse(resp);
            Assert.Equal(response.Count, 2);
            Assert.Equal(response[0]["jsonrpc"].Value<string>(), "2.0");
            Assert.Equal(response[0]["result"].Value<int>(), 3);
            Assert.Equal(response[0]["id"].Type, JTokenType.Integer);
            Assert.Equal(response[0]["id"].Value<int>(), 1);
            Assert.Equal(response[1]["jsonrpc"].Value<string>(), "2.0");
            Assert.Equal(response[1]["error"]["code"].Value<int>(), -32603);
            Assert.Equal(response[1]["id"].Type, JTokenType.Integer);
            Assert.Equal(response[1]["id"].Value<int>(), 2);
        }

        [Fact]
        public async Task TestBatchWithNotification()
        {
            var request = @"[{'jsonrpc': '2.0', 'method': 'add', 'params': [1,2], 'id': 1}, {'jsonrpc': '2.0', 'method': 'add', 'params': [1,2]}]";
            var resp = await processor.ProcessAsync(request);
            var response = JObject.Parse(resp);
            Assert.Equal(response["jsonrpc"].Value<string>(), "2.0");
            Assert.Equal(response["result"].Value<int>(), 3);
            Assert.Equal(response["id"].Type, JTokenType.Integer);
            Assert.Equal(response["id"].Value<int>(), 1);
        }

        /*
        [TestCase]
        public async void TestAsyncTask()
        {
            var request = @"{'jsonrpc': '2.0', 'method': 'asynctest', 'id': 1} ";
            var resp = await JsonRpcProcessor.ProcessAsync(request);
            var response = JObject.Parse(resp);
            Assert.Equal(response["jsonrpc"].Value<string>(), "2.0");
            Assert.Equal(response["result"].Value<string>(), "asynctest");
            Assert.Equal(response["id"].Type, JTokenType.Integer);
            Assert.Equal(response["id"].Value<int>(), 1);
        }
        */

        [Fact]
        public async Task TestException()
        {
            var request = @"{'jsonrpc': '2.0', 'method': 'exceptiontest', 'id': 1} ";
            var resp = await processor.ProcessAsync(request);
            var response = JObject.Parse(resp);
            Assert.Equal(response["jsonrpc"].Value<string>(), "2.0");
            var data = JToken.Parse(response["error"]["data"].ToString());
            Assert.Equal(data["someData"].Value<string>(), "somedata");
            Assert.Equal(response["error"]["code"].Value<int>(), 1);
        }

        [Fact]
        public async Task TestCustomException()
        {
            JsonRpcProcessor.Instance.RegisterException<CustomException>((e,l) => new RpcError(555, "aaa"));
            var request = @"{'jsonrpc': '2.0', 'method': 'customexceptiontest', 'id': 1}";
            var resp = await processor.ProcessAsync(request);
            var response = JObject.Parse(resp);
            Assert.Equal(response["jsonrpc"].Value<string>(), "2.0");
            var message = response["error"]["message"].Value<string>();
            Assert.Equal("aaa", message);
            Assert.Equal(response["error"]["code"].Value<int>(), 555);
        }

        [Fact]
        public async Task TestMethodInServiceScope()
        {
            var request = @"{'jsonrpc': '2.0', 'method': 'methodInServiceScope', 'id': 1} ";
            var resp = await processor.ProcessAsync(request, "someService");
            var response = JObject.Parse(resp);
            Assert.Equal(response["jsonrpc"].Value<string>(), "2.0");
            Assert.NotNull(response["result"]);

            resp = await processor.ProcessAsync(request, "");
            response = JObject.Parse(resp);
            Assert.Equal(response["jsonrpc"].Value<string>(), "2.0");
            Assert.NotNull(response["error"]);
            Assert.Equal(response["error"]["code"].Value<int>(), -32601);

            resp = await processor.ProcessAsync(request, "zdupy");
            response = JObject.Parse(resp);
            Assert.Equal(response["jsonrpc"].Value<string>(), "2.0");
            Assert.NotNull(response["error"]);
            Assert.Equal(response["error"]["code"].Value<int>(), -32601);

            resp = await processor.ProcessAsync(request);
            response = JObject.Parse(resp);
            Assert.Equal(response["jsonrpc"].Value<string>(), "2.0");
            Assert.NotNull(response["error"]);
            Assert.Equal(response["error"]["code"].Value<int>(), -32601);
        }

        /*
        [Fact]
        public void GetSchemaTest()
        {
            var schema = SchemaRenderer.RenderIndex("/");
            schema = SchemaRenderer.RenderEntry("MethodWithDescriptions", null);
            // TODO: !
        }
        */

        [Fact]
        public async Task RequestHookTest()
        {
            GlobalFlags.HookHit = false;
            JsonRpcProcessor.Instance.RegisterRequestHookService<RequestHookService>();
            var request = @"{'jsonrpc': '2.0', 'method': 'add', 'id': 1, params: { 'a': 1, 'b': 3}}";
            var resp = await processor.ProcessAsync(request);
            var response = JObject.Parse(resp);
            Assert.True(GlobalFlags.HookHit);

            GlobalFlags.HookHit = false;
            JsonRpcProcessor.Instance.UnregisterRequestHookService<RequestHookService>();
        }

        [Fact]
        public async Task OptionalParamsTest()
        {
            var request = @"{'jsonrpc': '2.0', 'method': 'methodWithOptionalParams', 'id': 1, params: { 'a': 1}}";
            var resp = await processor.ProcessAsync(request);
            var response = JObject.Parse(resp);
            Assert.Equal(response["jsonrpc"].Value<string>(), "2.0");
            Assert.Equal(response["result"].Value<int>(), 3);
        }

        [Fact]
        public async Task OptionalParamsFailTest()
        {
            var request = @"{'jsonrpc': '2.0', 'method': 'methodWithOptionalParams', 'id': 1, params: { 'b': 1}}";
            var resp = await processor.ProcessAsync(request);
            var response = JObject.Parse(resp);
            Assert.Equal(response["jsonrpc"].Value<string>(), "2.0");
            Assert.NotNull(response["error"]);
            Assert.Equal(response["error"]["code"].Value<int>(), -32602);
        }
    }

    internal static class GlobalFlags
    {
        public static bool HookHit { get; set; }
    }

    internal class RequestHookService : IRequestHookService
    {
        public void Process(JToken req)
        {
            Assert.Equal(req["method"].Value<string>(), "add");
            Assert.Equal(req["id"].Value<int>(), 1);
            Assert.Equal(req["params"]["a"].Value<int>(), 1);
            Assert.Equal(req["params"]["b"].Value<int>(), 3);
            GlobalFlags.HookHit = true;
        }
    }
}