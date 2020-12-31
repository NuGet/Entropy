using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;

namespace IVsTestingExtension.Models
{
    internal class ApiService
    {
        public ApiService(string name, IReadOnlyList<MethodInfo> methods, Func<Task<object>> factory)
        {
            var apiMethods = new List<ApiMethod>(methods.Count);
            for (int i = 0; i < methods.Count; i++)
            {
                var apiMethod = new ApiMethod(methods[i], this);
                apiMethods.Add(apiMethod);
            }

            Name = name;
            Methods = apiMethods;
            Factory = factory;
        }

        public string Name { get; }
        public IReadOnlyList<ApiMethod> Methods { get; }
        public Func<Task<object>> Factory { get; }
    }
}
