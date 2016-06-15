using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using JsonRpc.Commons;
using JsonRpc.Commons.Exceptions;

namespace JsonRpc.Host
{
    public class JsonRpcProcessor
    {
        private static Dictionary<Type, Func<Exception, RpcError>> exceptionHandlers
           = new Dictionary<Type, Func<Exception, RpcError>>();
        private static List<Type> requestHookServices = new List<Type>();
        private readonly IServiceProvider serviceProvider;
        private readonly ILogger logger;
        private readonly RpcMethodRegister register;
        private readonly Extractor parser;

        public JsonRpcProcessor(
            ILoggerFactory loggerFactory,
            IServiceProvider serviceProvider = null)
        {
            this.serviceProvider = serviceProvider;
            this.logger = loggerFactory.CreateLogger("JsonRpc.Host");
            this.register = new RpcMethodRegister(this.logger);
            this.register.ScanAssemblies(assemblyScan());
            this.parser = new Extractor(this.register, this.logger);
        }

        private static Func<Assembly[]> assemblyScan;

        public static void ScanAssemblies(Func<Assembly[]> assemblies)
        {
            assemblyScan = assemblies;
        }

        public static void RegisterException<T>(Func<T, RpcError> handler) where T : Exception
        {
            JsonRpcProcessor.exceptionHandlers[typeof(T)] = (e) => handler.Invoke((T)e);
        }

        public static void UnregisterExcetpion<T>()
        {
            if (JsonRpcProcessor.exceptionHandlers.ContainsKey(typeof(T)))
            {
                JsonRpcProcessor.exceptionHandlers.Remove(typeof(T));
            }
        }

        public static void RegisterRequestHookService<T>() where T : IRequestHookService
        {
            JsonRpcProcessor.requestHookServices.Add(typeof(T));
        }

        public static void UnregisterRequestHookService<T>() where T : IRequestHookService
        {
            JsonRpcProcessor.requestHookServices.Remove(typeof(T));
        }

        public async Task<string> ProcessAsync(string json, string service = null)
        {
            try
            {
                var calls = this.parser.ExtractAndMatch(json, service);
                this.FireRequestHooks(calls.Select(x => x.RawRequestJson));
                await this.CallAsync(calls.Where(x => x.Error == null).ToList());
                var responses = this.PrepareResponses(calls);
                if (responses == null) return null;
                return RpcSerializer.ToJson(responses);
            }
            catch (JsonRpcException ex)
            {
                return RpcSerializer.ToJson(ex);
            }
            catch (Exception ex)
            {
                this.logger.LogError(ex.Message, ex);
                var err = new JsonRpcInternalErrorException(ex);
                return RpcSerializer.ToJson(err);
            }
        }

        private void FireRequestHooks(IEnumerable<JToken> requests)
        {
            foreach (var hookService in JsonRpcProcessor.requestHookServices)
            {
                IRequestHookService service = (IRequestHookService)this.CreateServiceInstance(hookService);
                foreach (var request in requests)
                {
                    service.Process(request);
                }
            }
        }

        private IList<RpcResponse> PrepareResponses(IEnumerable<RpcCallContext> rpcCalls)
        {
            var responses = new List<RpcResponse>();
            foreach (var call in rpcCalls)
            {
                if (call.CallType == RpcType.Notification) continue;

                if (call.Error != null)
                {
                    responses.Add(new RpcResponse(null, call.Id, call.Error));
                }
                else
                {
                    responses.Add(new RpcResponse(call.Result, call.Id));
                }
            }

            if (responses.Count == 0)
                return null;

            return responses;
        }


        private object CreateServiceInstance(Type type)
        {
            object obj = null;
            if (this.serviceProvider != null)
            {
                obj = this.serviceProvider.GetService(type);
                if (obj == null)
                {
                    throw new Exception($"Unable to create instance of {type.Name}. Check IoC container registration.");
                }
            }
            else
            {
                obj = Activator.CreateInstance(type);
            }
            return obj;
        }

        private async Task CallAsync(List<RpcCallContext> rpcCalls)
        {
            var awaitableCalls = new List<RpcCallContext>();

            foreach (var call in rpcCalls)
            {
                try
                {
                    if (call.Error != null) return;
                    var entry = call.RegistryEntry;
                    var service = this.CreateServiceInstance(entry.ClassType);
                    if (entry.IsAsync)
                    {
                        var task = (Task)entry.MethodInfo.Invoke(service, call.Parameters.ToArray());
                        if (task.IsFaulted)
                        {
                            this.HandleExeption(task.Exception.InnerException, call);
                        }
                        else
                        {
                            await task;
                            call.Result = ((dynamic)task).Result;
                        }
                    }
                    else
                    {
                        call.Result = entry.MethodInfo.Invoke(service, call.Parameters.ToArray());
                    }
                }
                catch (Exception ex)
                {
                    this.HandleExeption(ex, call);
                }
            }
        }

        private void HandleExeption(Exception ex, RpcCallContext call)
        {
            if (ex is JsonRpcException)
            {
                call.Error = RpcError.FromException((JsonRpcException)ex);
            }
            else
            {
                if (JsonRpcProcessor.exceptionHandlers.ContainsKey(ex.GetType()))
                {
                    var handler = JsonRpcProcessor.exceptionHandlers[ex.GetType()];
                    call.Error = handler(ex);
                }
                else
                {
                    this.logger.LogError(ex.Message, ex);
                    call.Error = RpcError.FromException(new JsonRpcInternalErrorException(ex));
                }
            }
        }
    }
}