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
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;

namespace JsonRpc.Host
{
    public class JsonRpcProcessor
    {

        public JsonRpcProcessor(ILogger logger,
            ILogger loggerDiag,
            IServiceScopeFactory serviceScopeFactory)
        {
            this.serviceScopeFactory = serviceScopeFactory;
            this.logger = logger;
            this.loggerDiag = loggerDiag;
            this.register = new RpcMethodRegister(this.logger);
            this.parser = new Extractor(this.register, this.logger);
            JsonRpcProcessor.instance = this;
        }

        private readonly Dictionary<Type, Func<Exception, RpcError>> exceptionHandlers
           = new Dictionary<Type, Func<Exception, RpcError>>();
        private readonly List<Type> requestHookServices = new List<Type>();
        private readonly IServiceScopeFactory serviceScopeFactory;
        private readonly ILogger logger;
        private readonly ILogger loggerDiag;
        private readonly RpcMethodRegister register;
        private readonly Extractor parser;

        private static JsonRpcProcessor instance;
        public static JsonRpcProcessor Instance => instance;

        public void ScanAssemblies(Assembly[] assemblies)
        {
            this.register.ScanAssemblies(assemblies);
        }

        public void Configure(JsonRpcHostOptions options)
        {
            if (options.SerializerSettings != null) RpcSerializer.SerializerSettings = options.SerializerSettings;
            if (options.AssembliesToScan != null) this.register.ScanAssemblies(options.AssembliesToScan);
        }

        public void RegisterException<T>(Func<T, ILogger, RpcError> handler) where T : Exception
        {
            this.exceptionHandlers[typeof(T)] = e => handler.Invoke((T)e, this.logger);
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
                this.logger.LogError(0, ex, ex.Message);
                var err = new JsonRpcInternalErrorException(ex);
                return RpcSerializer.ToJson(err);
            }
        }

        private void FireRequestHook(IServiceProvider serviceProvider, JToken request)
        {
            foreach (var hookService in this.requestHookServices)
            {
                IRequestHookService service = (IRequestHookService)this.CreateServiceInstance(serviceProvider, hookService);
                service.Process(request);
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

        private object CreateServiceInstance(IServiceProvider serviceProvider, Type type)
        {
            object obj = null;
            if (serviceProvider != null)
            {
                obj = serviceProvider.GetService(type);
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
            var startDate = DateTime.Now;
            foreach (var call in rpcCalls)
            {
                var scope = this.serviceScopeFactory?.CreateScope();
                this.FireRequestHook(scope?.ServiceProvider, call.RawRequestJson);
                startDate = DateTime.Now;
                watch.Start();
                try
                {
                    if (call.Error != null) return;
                    var entry = call.RegistryEntry;
                    var service = this.CreateServiceInstance(scope?.ServiceProvider, entry.ClassType);
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
                this.loggerDiag.LogInformation("dateStart: {startDate} method: {method} processTime: {processTime}",
                    startDate,
                    call.MethodName,
                    watch.ElapsedMilliseconds);

                scope?.Dispose();
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
                    this.logger.LogError(0, ex, ex.Message);
                    call.Error = RpcError.FromException(new JsonRpcInternalErrorException(ex));
                }
            }
        }
    }
}