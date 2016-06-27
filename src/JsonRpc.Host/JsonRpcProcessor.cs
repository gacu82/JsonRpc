using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using JsonRpc.Commons;
using JsonRpc.Commons.Exceptions;
using Newtonsoft.Json;

namespace JsonRpc.Host
{
    public class JsonRpcProcessor
    {
        private readonly Dictionary<Type, Func<Exception, RpcError>> exceptionHandlers
           = new Dictionary<Type, Func<Exception, RpcError>>();
        private readonly List<Type> requestHookServices = new List<Type>();
        private readonly IServiceProvider serviceProvider;
        private readonly ILogger logger;
        private readonly ILogger loggerDiag;
        private readonly RpcMethodRegister register;
        private readonly Extractor parser;


        public JsonRpcProcessor(ILogger logger,
            ILogger loggerDiag,
            IServiceProvider serviceProvider)
        {
            this.serviceProvider = serviceProvider;
            this.logger = logger;
            this.loggerDiag = loggerDiag;
            this.register = new RpcMethodRegister(this.logger);
            this.parser = new Extractor(this.register, this.logger);
            JsonRpcProcessor.instance = this;
        }

        private static JsonRpcProcessor instance;
        public static JsonRpcProcessor Instance => instance;

        public void ScanAssemblies(Assembly[] assemblies)
        {
            this.register.ScanAssemblies(assemblies);
        }

        public void Configure(JsonRpcHostOptions options)
        {
            if(options.SerializerSettings != null) RpcSerializer.SerializerSettings = options.SerializerSettings;
            if(options.AssembliesToScan != null) this.register.ScanAssemblies(options.AssembliesToScan);
        }

        public void ConfigureRequestId(string requestIdRegex)
        {
            
        }

        public void RegisterException<T>(Func<T, RpcError> handler) where T : Exception
        {
            this.exceptionHandlers[typeof(T)] = e => handler.Invoke((T)e);
        }

        public void UnregisterExcetpion<T>()
        {
            if (this.exceptionHandlers.ContainsKey(typeof(T)))
            {
                this.exceptionHandlers.Remove(typeof(T));
            }
        }

        public void RegisterRequestHookService<T>() where T : IRequestHookService
        {
            this.requestHookServices.Add(typeof(T));
        }

        public void UnregisterRequestHookService<T>() where T : IRequestHookService
        {
            this.requestHookServices.Remove(typeof(T));
        }

        public async Task<string> ProcessAsync(string json, string service = null)
        {
            try
            {
                var calls = this.parser.ExtractAndMatchCalls(json, service);
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
            foreach (var hookService in this.requestHookServices)
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
                if (call.CallType == RpcCallType.Notification) continue;

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
            var watch = new Stopwatch();
            var dateStart = DateTime.Now;
            foreach (var call in rpcCalls)
            {
                dateStart = DateTime.Now;
                watch.Start();
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
                this.loggerDiag.LogInformation(JsonConvert.SerializeObject(new
                {
                    dateStart = dateStart,
                    methodName = call.MethodName,
                    processingTime = watch.ElapsedMilliseconds
                }, RpcSerializer.SerializerSettings));
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
                if (this.exceptionHandlers.ContainsKey(ex.GetType()))
                {
                    var handler = this.exceptionHandlers[ex.GetType()];
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