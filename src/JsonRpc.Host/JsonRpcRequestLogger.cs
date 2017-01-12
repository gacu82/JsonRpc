using System;
using Microsoft.Extensions.Logging;


namespace JsonRpc.Host
{
    public interface IJsonRpcRequestLogger
    {
        void Log(DateTime requestDate, string requestBody, string responseBody, Diagnostics diagnostics);
    }

    public class DefaultJsonRpcRequestLogger : IJsonRpcRequestLogger
    {
        public DefaultJsonRpcRequestLogger()
        {
        }
        public void Log(DateTime requestDate, string requestBody, string responseBody, Diagnostics diagnostics)
        {
        }
    }

    public class RichInfoJsonRpcRequestLogger : IJsonRpcRequestLogger
    {
        public RichInfoJsonRpcRequestLogger(ILoggerFactory loggerFactory)
        {
            this.logger = loggerFactory.CreateLogger("JsonRpc.Host.Requests");
        }

        private readonly ILogger logger;

        public void Log(DateTime requestDate, string requestBody, string responseBody, Diagnostics diagnostics)
        {
            this.logger.LogInformation(
                "startDate: {startDate} request: {request} response: {response} " +
                "processTime: {processTime} readTime: {readTime} writeTime: {writeTime} totalTime: {totalTime}",
                requestDate,
                requestBody,
                responseBody,
                diagnostics.ProcessTime,
                diagnostics.ReadTime,
                diagnostics.WriteTime,
                diagnostics.TotalTime
            );
        }
    }
}
