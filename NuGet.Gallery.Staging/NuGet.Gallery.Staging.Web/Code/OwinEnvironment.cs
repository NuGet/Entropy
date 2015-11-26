using System;
using System.IO;
using System.Linq;
using System.Reflection;

namespace NuGet.Gallery.Staging.Web.Code
{
    public static class OwinEnvironment
    {
        private static Func<string, string> _mapPathImpl;

        public static Func<string, string> MapPath
        {
            get { return _mapPathImpl ?? (_mapPathImpl = InitializeMapPath()); }
        }

        private static Func<string, string> InitializeMapPath()
        {
            var systemWeb = TryGetSystemWebAssembly();
            if (systemWeb != null)
            {
                var hostingEnvironment = systemWeb.GetType("System.Web.Hosting.HostingEnvironment");
                if (hostingEnvironment != null)
                {
                    var method = hostingEnvironment.GetMethod("MapPath");

                    var func = (Func<string, string>)method.CreateDelegate(typeof(Func<string, string>));

                    return path => func(path) ?? OwinMapPath(path);
                }
            }

            return OwinMapPath;
        }

        private static Assembly TryGetSystemWebAssembly()
        {
            return AppDomain.CurrentDomain.GetAssemblies().FirstOrDefault(assembly => assembly.FullName.StartsWith("System.Web,"));
        }

        private static string OwinMapPath(string virtualPath)
        {
            var assembly = Assembly.GetEntryAssembly() ?? Assembly.GetCallingAssembly();
            var path = Path.GetDirectoryName(assembly.GetPath());

            if (path == null) throw new Exception("Unable to determine executing assembly path.");

            if (path.EndsWith("bin"))
            {
                path = Path.GetFullPath(Path.Combine(path, "../"));
            }

            return Path.Combine(path, virtualPath.Replace("~/", "").TrimStart('/').Replace('/', Path.DirectorySeparatorChar));
        }

        private static string GetPath(this Assembly assembly)
        {
            return new Uri(assembly.EscapedCodeBase).LocalPath;
        }
    }
}