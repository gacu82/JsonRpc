using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using JsonRpc.Commons;
using JsonRpc.Commons.Exceptions;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;

namespace JsonRpc.Host
{
    internal class Extractor
    {
        private readonly ILogger logger;
        private readonly RpcMethodRegister register;

        public Extractor(RpcMethodRegister register, ILogger logger)
        {
            this.register = register;
            this.logger = logger;
        }

        public List<RpcCallContext> ExtractAndMatch(string json, string service)
        {
            var calls = new List<RpcCallContext>();
            var reqObj = this.ParseJsonOrThrow(json);
            var reqArray = this.ExtractCallArrayOrThrow(reqObj);

            foreach (JToken req in reqArray)
            {
                var call = new RpcCallContext(req);
                calls.Add(call);
                try
                {
                    this.ExtractAndMatchSingle(req, call, service);
                }
                catch (JsonRpcInternalErrorException ex)
                {
                    this.logger.LogError(ex.Message, ex);
                    call.Error = RpcError.FromException(ex);
                }
                catch (JsonRpcException ex)
                {
                    call.Error = RpcError.FromException(ex);
                }
                catch (Exception ex)
                {
                    this.logger.LogError($"Error while extrating JsonRpc method.", ex);
                    call.Error = RpcError.FromException(new JsonRpcInternalErrorException(ex));
                }
            }
            return calls;
        }

        private RpcCallContext ExtractAndMatchSingle(JToken request, RpcCallContext call, string service)
        {
            this.CheckIfValidJsonRpcOrThrow(request);

            call.MethodName = request["method"].ToString();
            call.Id = (JToken)request["id"];
            call.CallType = call.Id == null ? RpcType.Notification : RpcType.Call;

            var methodEntry = this.register.GetMethodEntry(call.MethodName, service);
            if (methodEntry == null)
            {
                throw new JsonRpcMethodNotFoundException();
            }
            call.RegistryEntry = methodEntry;
            var paramsInfo = methodEntry.MethodInfo.GetParameters();
            var parameters = new List<object>();

            var requestParams = (JContainer)request["params"] ?? new JArray();

            if (requestParams.Type == JTokenType.Array)
            {
                parameters = this.ExtractParamsFromArrayOrThrow(paramsInfo, (JArray)requestParams);
            }
            else
            {
                parameters = this.ExtractParamsFromObjectOrThrow(paramsInfo, (JObject)requestParams);
            }
            call.Parameters = parameters;
            return call;
        }

        private List<object> ExtractParamsFromArrayOrThrow(ParameterInfo[] paramsInfo, JArray requestParams)
        {
            var parameters = new List<object>();
            // Check if parameter count match
            if (requestParams.Count != paramsInfo.Length)
            {
                throw new JsonRpcInvalidParamsException(paramsInfo);
            }
            for (int i = 0; i < requestParams.Count; i++)
            {
                try
                {
                    parameters.Add(requestParams[i].ToObject(paramsInfo[i].ParameterType));
                }
                catch
                {
                    throw new JsonRpcInvalidParamsException(paramsInfo);
                }
            }
            return parameters;
        }

        private List<object> ExtractParamsFromObjectOrThrow(ParameterInfo[] paramsInfo, JObject requestParams)
        {
            var parameters = new List<object>();
            // Check if there are any non-optional parameters unmatched
            var requestParamNames = requestParams.Properties().Select(x => x.Name);
            var notPresentRequiredParams = paramsInfo.Where(x => x.IsOptional == false && !requestParamNames.Contains(x.Name)).ToArray();
            if (notPresentRequiredParams.Count() > 0)
            {
                throw new JsonRpcInvalidParamsException(paramsInfo, notPresentRequiredParams);
            }
            foreach (var param in paramsInfo.OrderBy(c => c.Position))
            {
                JToken reqParam = null;
                if (requestParams.TryGetValue(param.Name, out reqParam))
                {
                    try
                    {
                        parameters.Add(reqParam.ToObject(param.ParameterType));
                    }
                    catch
                    {
                        throw new JsonRpcInvalidParamsException(paramsInfo);
                    }
                }
                else
                {
                    parameters.Add(Type.Missing);
                }
            }
            return parameters;
        }

        private JToken ParseJsonOrThrow(string json)
        {
            try
            {
                return JToken.Parse(json);
            }
            catch
            {
                throw new JsonRpcParseErrorException();
            }
        }

        private JArray ExtractCallArrayOrThrow(JToken request)
        {
            if (request.Type == JTokenType.Array) return (JArray)request;
            else if (request.Type == JTokenType.Object)
            {
                return new JArray(request);
            }
            else
            {
                throw new JsonRpcInvalidRequestException();
            }
        }

        private void CheckIfValidJsonRpcOrThrow(JToken request)
        {
            if (request.Type == JTokenType.Object &&
               request["jsonrpc"]?.ToString() == "2.0" &&
               request["method"] != null) return;
            else
            {
                throw new JsonRpcInvalidRequestException();
            }
        }
    }
}