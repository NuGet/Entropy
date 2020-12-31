using System.Reflection;

namespace IVsTestingExtension.Models
{
    internal class ApiMethod
    {
        public ApiMethod(MethodInfo methodInfo, ApiService service)
        {
            MethodInfo = methodInfo;
            Service = service;
        }

        public MethodInfo MethodInfo { get; }
        public ApiService Service { get; }
    }
}
