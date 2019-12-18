using EnvDTE;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace IVsTestingExtension.Tests
{
    public interface ITestMethodProvider
    {
        Func<Project, Dictionary<string, string>, Task> GetMethod();
    }
}
