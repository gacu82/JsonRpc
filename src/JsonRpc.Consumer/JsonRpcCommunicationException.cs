using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace JsonRpc.Client
{
    public class JsonRpcCommunicationException : Exception
    {
        public JsonRpcCommunicationException(
            string message,
            string remoteServiceName = null,
            string reqObjString = null,
            Exception innerException = null
        )
            : base(message, innerException)
        {
            this.RemoteServiceName = remoteServiceName;
            this.RequestObjString = reqObjString;
        }

        public string RemoteServiceName { get; }
        public string RequestObjString { get; }
    }
}
