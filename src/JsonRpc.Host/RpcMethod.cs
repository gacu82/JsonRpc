using System;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace JsonRpc.Host
{
    internal class RpcMethod
    {
        public RpcMethod(Type classType, MethodInfo methodInfo)
        {
            this.ClassType = classType;
            this.MethodInfo = methodInfo;
            this.IsAsync = (AsyncStateMachineAttribute)methodInfo.GetCustomAttribute(typeof(AsyncStateMachineAttribute)) != null;
        }

        public Type ClassType { get; private set; }
        public MethodInfo MethodInfo { get; private set; }
        public bool IsAsync { get; private set; }
    }
}
