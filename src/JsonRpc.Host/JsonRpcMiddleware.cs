﻿using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;

namespace JsonRpc.Host
{
    internal class JsonRpcMiddleware
    {
        private static readonly Encoding utf8WithoutBom = new UTF8Encoding(false);

        private readonly RequestDelegate next;
        private readonly ILogger logger;
        private readonly ILogger loggerDiag;
        private readonly JsonRpcProcessor processor;
        private readonly IJsonRpcRequestLogger requestLogger;

        public JsonRpcMiddleware(RequestDelegate next,
            ILoggerFactory loggerFactory, 
            IServiceScopeFactory serviceScopeFactory,
            IJsonRpcRequestLogger requestLogger)
        {
            this.next = next;
            this.logger = loggerFactory.CreateLogger("JsonRpc.Host");
            this.loggerDiag = loggerFactory.CreateLogger("JsonRpc.Host.Diagnostics");
            this.processor =  new JsonRpcProcessor(logger, loggerDiag, serviceScopeFactory);
            this.requestLogger = requestLogger;
        }

        public async Task Invoke(HttpContext context)
        {
            var requestStartDate = DateTime.Now;
            var diagnostics = new Diagnostics() {StartDate = requestStartDate};

            var request = context.Request;
            var response = context.Response;

            string requestString = null;
            string responseString = null;

            var watch = System.Diagnostics.Stopwatch.StartNew();

            // Forward the request if it's not POST method
            if (!request.Method.Equals("POST", StringComparison.CurrentCultureIgnoreCase))
            {
                await this.next.Invoke(context);
                return;
            }

            try
            {
                using (var reader = new StreamReader(request.Body))
                {
                    if (request.ContentLength.HasValue && request.ContentLength > 0)
                    {
                        requestString = await reader.ReadToEndAsync();
                    }
                }
                diagnostics.ReadTime = watch.ElapsedMilliseconds;
                var service = request.Path.ToString().Trim('/');
                responseString = await processor.ProcessAsync(requestString, service);
                diagnostics.ProcessTime = watch.ElapsedMilliseconds - diagnostics.ReadTime;
                response.Headers.Add("Content-Type", "application/json");
                response.ContentLength = utf8WithoutBom.GetByteCount(responseString);

                using (var writer = new StreamWriter(response.Body, utf8WithoutBom))
                {
                    await writer.WriteAsync(responseString);
                }
                diagnostics.WriteTime = watch.ElapsedMilliseconds - diagnostics.ProcessTime;
            }
            catch (Exception ex)
            {
                this.logger.LogError("Error in JsonRpc middleware: {Exception}", ex);
            }

            try
            {
                this.requestLogger.Log(requestStartDate, requestString, responseString, diagnostics);
            }
            catch (Exception ex)
            {
                this.logger.LogError("Error in JsonRpc middleware request logging: {Exception}", ex);
            }
        }
    }
}
