using JsonRpc.Client;
using System;
using Microsoft.Extensions.Logging;

namespace Test
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var loggerFactory = new LoggerFactory();
            loggerFactory.AddConsole();
            var logger = loggerFactory.CreateLogger("Test");
            var cli = new RpcClient(loggerFactory, timeout: TimeSpan.FromMilliseconds(200));
            while(true)
            {
                try
                {
                    var uri = new Uri("http://localhost:5000/api");
                    cli.CallAsync(uri, "add", 1, new { a = 1, b = 2 }).Wait();
                }
                catch(Exception ex)
                {
                    logger.LogError(0, ex, "Error") ;
                }
            }
        }
    }
}
