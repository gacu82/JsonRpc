using System;
using Newtonsoft.Json.Linq;
using System.Linq;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using Newtonsoft.Json.Converters;
using Xunit;
using Moq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using JsonRpc.Commons;
using Microsoft.Extensions.Logging;

namespace JsonRpc.Client.Tests
{
    public class ClientTest
    {
        public ClientTest()
        {
            JsonConvert.DefaultSettings = () =>
            {
                var settings = new JsonSerializerSettings
                {
                    Formatting = Formatting.None,
                    ContractResolver = new CamelCasePropertyNamesContractResolver()
                };
                settings.Converters.Add(new StringEnumConverter());
                return settings;
            };
        }

        [Fact]
        public void Test()
        {
            // Prepare mocks
            var uri = new Uri("http://test.com"); // Not used
            Stream requestStream = new MemoryStream();
            Stream responseStream = new MemoryStream();
            var requestFactoryMock = new Mock<IRequestFactory>();
            var requestMock = new Mock<HttpWebRequest>();
            var responseMock = new Mock<WebResponse>();
            requestMock.Setup(x => x.GetRequestStreamAsync()).Returns(Task.FromResult(requestStream));
            requestMock.Setup(x => x.GetResponseAsync()).Returns(Task.FromResult(responseMock.Object));
            responseMock.Setup(x => x.GetResponseStream()).Returns(responseStream);
            requestFactoryMock.Setup(x => x.CreateHttp(uri)).Returns(requestMock.Object);
            // Prepare response whatever the request is
            using (TextWriter writer = new StreamWriter(responseStream, Encoding.UTF8))
            {
                var rpcResponse = new JsonRpc.Commons.RpcResponse("test", 1);
                writer.WriteLine(JsonConvert.SerializeObject(rpcResponse));
            }
            responseStream.Seek(0, SeekOrigin.Begin);

            var logger = new Mock<ILogger<JsonRpcClient>>();

            var client = new JsonRpcClient(requestFactoryMock.Object, TimeSpan.FromSeconds(10), logger.Object);

            var resp = client.CallAsync(new Uri("http://test.com"), new RpcCall("somemethod", 1, new {a = 1, b = 2})).Result;


        }
    }
}
