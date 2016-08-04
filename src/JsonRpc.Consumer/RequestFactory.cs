using System;
using System.Net.Http;
using System.Threading.Tasks;

namespace JsonRpc.Client
{
    public interface IRequestFactory
    {
        Task<HttpResponseMessage> PostAsync(Uri uri, HttpContent content);
        TimeSpan Timeout { get; set; }
    }

    public class DefaultRequestFactory : IRequestFactory
    {
        public DefaultRequestFactory()
        {
            this.HttpClient = new HttpClient();
        }
        public HttpClient HttpClient { get; }

        public TimeSpan Timeout
        {
            get
            {
                return this.HttpClient.Timeout;
            }
            set
            {
                this.HttpClient.Timeout = value;
            }
        }

        public async Task<HttpResponseMessage> PostAsync(Uri uri, HttpContent content)
        {
            return await this.HttpClient.PostAsync(uri, content);
        }
    }
}
