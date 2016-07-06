using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using JsonRpc.Commons;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace JsonRpc.Host
{
    internal class JsonRpcMiddleware
    {
        private static readonly Encoding utf8WithoutBom = new UTF8Encoding(false);

        private readonly RequestDelegate next;
        private readonly ILogger logger;
        private readonly ILogger loggerRequest;
        private readonly ILogger loggerDiag;
        private readonly JsonRpcProcessor processor;

        public JsonRpcMiddleware(RequestDelegate next,
            ILoggerFactory loggerFactory, 
            IServiceProvider serviceProvider)
        {
            this.next = next;
            this.logger = loggerFactory.CreateLogger("JsonRpc.Host");
            this.loggerRequest = loggerFactory.CreateLogger("JsonRpc.Host.Requests");
            this.loggerDiag = loggerFactory.CreateLogger("JsonRpc.Host.Diagnostics");
            this.processor =  new JsonRpcProcessor(logger, loggerDiag, serviceProvider);
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
                diagnostics.ReadingTime = watch.ElapsedMilliseconds;
                var service = request.Path.ToString().Trim('/');
                responseString = await processor.ProcessAsync(requestString, service);
                diagnostics.ProcessingTime = watch.ElapsedMilliseconds - diagnostics.ReadingTime;
                response.Headers.Add("Content-Type", "application/json");
                response.Headers.Add("X-ProcessingTime", diagnostics.ProcessingTime.ToString());
                response.ContentLength = utf8WithoutBom.GetByteCount(responseString);

                using (var writer = new StreamWriter(response.Body, utf8WithoutBom))
                {
                    await writer.WriteAsync(responseString);
                }
                diagnostics.WritingTime = watch.ElapsedMilliseconds - diagnostics.ProcessingTime;
            }
            catch (Exception ex)
            {
                this.logger.LogError("Error in JsonRpc middleware", ex);
            }

            try
            {
                this.logger.LogInformation($"{request.Method} {request.Path} {diagnostics.TotalTime}");
                this.loggerRequest.LogInformation(JsonConvert.SerializeObject(
                    new
                    {
                        startDate = requestStartDate,
                        request = requestString,
                        response = responseString,
                        processingTime = diagnostics.TotalTime,
                        readingTime = diagnostics.ReadingTime,
                        writingTime = diagnostics.WritingTime,
                        totalTime = diagnostics.TotalTime
                    }, RpcSerializer.SerializerSettings));
            }
            catch (Exception ex)
            {
                this.logger.LogError("Error in JsonRpc middleware request logging.", ex);
            }
        }
    }
}
