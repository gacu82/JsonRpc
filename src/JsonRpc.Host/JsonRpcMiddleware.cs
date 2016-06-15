using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;

namespace JsonRpc.Host
{
    public class JsonRpcMiddleware
    {
        private static readonly Encoding utf8WithoutBom = new UTF8Encoding(false);

        private static readonly Regex regExMethod =
            new Regex(
                @"""method""\s*:\s*""([^""]*)",
                RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.IgnoreCase | RegexOptions.Singleline);

        private readonly RequestDelegate next;
        private readonly ILogger logger;
        private readonly ILoggerFactory loggerFactory;

        public JsonRpcMiddleware(RequestDelegate next, ILoggerFactory loggerFactory)
        {
            this.next = next;
            this.loggerFactory = loggerFactory;
            this.logger = loggerFactory.CreateLogger<JsonRpcMiddleware>();
        }

        public async Task Invoke(HttpContext context)
        {
            var diagnostics = new Diagnostics();

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

                var processor = new JsonRpcProcessor(this.loggerFactory, context.RequestServices);
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
                var methodName = this.ExtractMethodName(requestString);
                this.logger.LogInformation($"{request.Method} {request.Path} JSONRPC: {methodName} {diagnostics.TotalTime}");
            }
            catch (Exception ex)
            {
                this.logger.LogError("Error in JsonRpc middleware request logging.", ex);
            }
        }

        private string ExtractMethodName(string request)
        {
            var methodMatch = regExMethod.Match(request);
            if (methodMatch.Groups.Count >= 2)
            {
                return methodMatch.Groups[1].Value;
            }
            else
            {
                return null;
            }
        }
    }
}
