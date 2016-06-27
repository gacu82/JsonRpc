using System;
using System.Net;

namespace JsonRpc.Client
{
    public interface IRequestFactory
    {
        HttpWebRequest CreateHttp(Uri uri);
    }

    public class RequestFactory : IRequestFactory
    {
        public HttpWebRequest CreateHttp(Uri uri)
        {
            return WebRequest.CreateHttp(uri);
        }
    }
}
