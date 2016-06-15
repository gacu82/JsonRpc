using System;
using System.Linq;
using System.Reflection;

namespace JsonRpc.Commons.Exceptions
{
    public class JsonRpcException : Exception
    {
        public JsonRpcException(int errorCode, string errorMessage = "", object data = null)
        {
            this.ErrorCode = errorCode;
            this.ErrorData = data;
            this.ErrorMessage = errorMessage;
        }

        public JsonRpcException(Enum errorCode, string errorMessage = "", object data = null)
        {
            this.ErrorCode = Convert.ToInt32(errorCode);
            this.ErrorData = data;
            this.ErrorMessage = errorMessage;
        }

        public int ErrorCode { get; private set; }
        public string ErrorMessage { get; private set; }
        public object ErrorData { get; protected set; }
    }

    public class JsonRpcParseErrorException : JsonRpcException
    {
        public JsonRpcParseErrorException()
            : base(-32700, "Parse error")
        {
        }
    }

    public class JsonRpcInvalidRequestException : JsonRpcException
    {
        public JsonRpcInvalidRequestException()
            : base(-32600, "Invalid Request")
        {
        }
    }

    public class JsonRpcMethodNotFoundException : JsonRpcException
    {
        public JsonRpcMethodNotFoundException()
            : base(-32601, "Method not found")
        {
        }
    }

    public class JsonRpcInternalErrorException : JsonRpcException
    {
        public JsonRpcInternalErrorException(Exception innerException)
            : base(-32603, "Internal error", innerException)
        {
            if (innerException != null) this.ErrorData = innerException.Message;
        }
    }

    public class JsonRpcInvalidParamsException : JsonRpcException
    {
        public JsonRpcInvalidParamsException(ParameterInfo[] requiredParams, ParameterInfo[] missingParams = null)
            : base(-32602, "Invalid params")
        {
            var requiredParamsMessage = this.GetParamInfoDesc(requiredParams);
            var missingParamsMessage = string.Empty;
            if (missingParams != null && missingParams.Length > 0)
            {
                missingParamsMessage = this.GetParamInfoDesc(missingParams);
            }
            this.ErrorData = $"Invalid params. Procedure expects params: {requiredParamsMessage}. Missing params: {missingParams}";
        }

        private string GetParamInfoDesc(ParameterInfo[] paramsInfo)
        {
            return string.Join(",", paramsInfo.Select(x => x.Name));
        }
    }
}
