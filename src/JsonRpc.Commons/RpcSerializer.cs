using JsonRpc.Commons.Exceptions;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;
using System.Collections.Generic;

namespace JsonRpc.Commons
{
    internal static class RpcSerializer
    {
        public static JsonSerializerSettings SerializerSettings { get; set; }

        static RpcSerializer()
        {
            SerializerSettings = new JsonSerializerSettings()
            {
                Formatting = Formatting.None,
                ContractResolver = new CamelCasePropertyNamesContractResolver(),
                ReferenceLoopHandling = ReferenceLoopHandling.Serialize,
                NullValueHandling = NullValueHandling.Ignore,
                DateTimeZoneHandling = DateTimeZoneHandling.Local,
                DateFormatString = "yyyy-MM-ddTHH:mm:ss.FFFK"
            };
            SerializerSettings.Converters.Add(new StringEnumConverter());
            SerializerSettings.Converters.Add(new IsoDateTimeConverter { DateTimeFormat = "yyyy-MM-ddTHH:mm:ss.FFFK" });
        }

        public static string ToJson(RpcCall call)
        {
            var dto = GetRequestContainer(call.Method, call.Id);
            if (call.Params != null)
            {
                dto["params"] = JToken.FromObject(call.Params);
            }
            return JsonConvert.SerializeObject(dto, SerializerSettings);
        }

        public static string ToJson(RpcResponse response)
        {
            var dto = RpcSerializer.GetJsonObjectResponse(response);
            return JsonConvert.SerializeObject(dto, SerializerSettings);
        }

        public static string ToJson(IList<RpcResponse> responses)
        {
            if (responses.Count == 1)
            {
                return ToJson(responses[0]);
            }
            else
            {
                var responseArray = new JArray();
                foreach (var resp in responses)
                {
                    responseArray.Add(RpcSerializer.GetJsonObjectResponse(resp));
                }
                return JsonConvert.SerializeObject(responseArray, SerializerSettings);
            }
        }

        public static string ToJson(IList<RpcCall> calls)
        {
            if (calls.Count == 1)
            {
                return ToJson(calls[0]);
            }
            else
            {
                return JsonConvert.SerializeObject(calls, SerializerSettings);
            }
        }

        public static string ToJson(JsonRpcException ex)
        {
            var dto = GetResponseContainer();
            var error = new JObject();
            error["code"] = ex.ErrorCode;
            error["message"] = ex.ErrorMessage;
            if (ex.ErrorData != null)
            {
                error["data"] = JToken.FromObject(ex.ErrorData);
            }
            dto["error"] = error;
            return JsonConvert.SerializeObject(dto);
        }

        private static JToken GetJsonObjectResponse(RpcResponse response)
        {
            var dto = GetResponseContainer(response.Id);

            if (response.Error != null)
            {
                dto["error"] = JToken.FromObject(response.Error);
            }
            else
            {
                if (response.Result != null)
                {
                    dto["result"] = JToken.FromObject(response.Result);
                }
                else
                {
                    dto["result"] = null;
                }
            }
            return dto;
        }

        private static JObject GetResponseContainer(object id = null)
        {
            var dto = new JObject();
            dto["jsonrpc"] = "2.0";
            if (id != null) dto["id"] = JToken.FromObject(id);
            return dto;
        }

        private static JObject GetRequestContainer(string method, object id)
        {
            var dto = new JObject();
            dto["jsonrpc"] = "2.0";
            dto["method"] = method;
            dto["id"] = id.ToString();
            return dto;
        }
    }
}
