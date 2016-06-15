using Microsoft.AspNetCore.Builder;

namespace JsonRpc.Host
{
    public static class Extensions
    {
        public static void UseJsonRpc(this IApplicationBuilder builder, string prefix)
        {
            builder.Map(prefix, (b) => { b.UseMiddleware<JsonRpcMiddleware>(); });
        }
    }
}