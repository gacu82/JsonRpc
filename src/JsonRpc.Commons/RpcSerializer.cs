using JsonRpc.Commons.Exceptions;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;
using System;
using System.Collections.Generic;
using System.Linq;

namespace JsonRpc.Commons
{
    public static class RpcSerializer
    {
        private static JsonSerializerSettings serializerSettings;

        static RpcSerializer()
        {
            serializerSettings = new JsonSerializerSettings()
            {
                Formatting = Formatting.None,
                ContractResolver = new CamelCasePropertyNamesContractResolver(),
                ReferenceLoopHandling = ReferenceLoopHandling.Serialize,
                NullValueHandling = NullValueHandling.Ignore,
                DateTimeZoneHandling = DateTimeZoneHandling.Local,
                DateFormatString = "yyyy-MM-ddTHH:mm:ss.FFFK"
            };
            serializerSettings.Converters.Add(new StringEnumConverter());
            serializerSettings.Converters.Add(new IsoDateTimeConverter() { DateTimeFormat = "yyyy-MM-ddTHH:mm:ss.FFFK" });
        }

        public static void Configure(Func<JsonSerializerSettings> settings)
        {
            serializerSettings = settings();
        }

        public static string ToJson(RpcCall call)
        {
            var dto = GetRequestContainer(call.Method, call.Id);
            if (call.Params != null)
            {
                dto["params"] = JToken.FromObject(call.Params);
            }
            return JsonConvert.SerializeObject(dto, serializerSettings);
        }


        public static string ToJson(RpcResponse response)
        {
            var dto = RpcSerializer.GetJsonObjectResponse(response);
            return JsonConvert.SerializeObject(dto, serializerSettings);
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
                return JsonConvert.SerializeObject(responseArray, serializerSettings);
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
                return JsonConvert.SerializeObject(calls, serializerSettings);
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
            JObject dto = new JObject();
            dto["jsonrpc"] = "2.0";
            if (id != null) dto["id"] = JToken.FromObject(id);
            return dto;
        }

        private static JObject GetRequestContainer(string method, object id)
        {
            JObject dto = new JObject();
            dto["jsonrpc"] = "2.0";
            dto["method"] = method;
            dto["id"] = id.ToString();
            return dto;
        }
    }
}
