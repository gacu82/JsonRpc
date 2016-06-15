using JsonRpc.Commons.Exceptions;
using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;

namespace JsonRpc.Host.Tests
{
    public class CustomException : Exception
    {
    }

    public class TestMethods
    {
        [JsonRpcMethod("add")]
        public int Add(int a, int b)
        {
            return a + b;
        }

        [JsonRpcMethod("sub")]
        public int Sub(int a, int b)
        {
            return a - b;
        }

        [JsonRpcMethod("crash")]
        public void MehodThatCrash()
        {
            var x = 0;
            var a = 1 / x;
        }

        [JsonRpcMethod("asynctest")]
        public async Task<string> AsyncTest()
        {
            return "asynctest";
        }

        [JsonRpcMethod("exceptiontest")]
        public async Task<string> ExeptionTest()
        {
            throw new JsonRpcException(1, "errortest", new { SomeData = "somedata" });
        }

        [JsonRpcMethod("customexceptiontest")]
        public async Task<string> CustomExceptionTest()
        {
            throw new CustomException();
        }

        [JsonRpcMethod("methodInServiceScope", "someService")]
        public void MethodInServiceScope()
        {
        }

        [JsonRpcMethod]
        public int MethodWithOptionalParams(int a, int b = 2)
        {
            return a + b;
        }

        [JsonRpcMethod]
        [Description("This is method that does something")]
        public AnotherObject MethodWithDescriptions([Required] int id, string anything, SomeObject someObject)
        {
            return null;
        }

        [JsonRpcMethod]
        public DateTime DateTimeMethod()
        {
            return DateTime.Now;
        }
    }
    
    [Description("Input data transfer object")]
    public class SomeObject
    {
        [Description("Date and time of some event")]
        public DateTime? DateTime { get; set; }

        [Required]
        [Description("Age of subject")]
        public int Age { get; set; }

        [Description("Name of subject")]
        public string Name { get; set; }
    }

    [Description("Resulting object")]
    public class AnotherObject
    {
        [Required]
        [Description("Acceptance date")]
        public DateTime SomeDate { get; set; }

        [Required]
        [Description("Amount of goods")]
        public float Amount { get; set; }

        [Description("Some other name")]
        public string Name { get; set; }
    }
}