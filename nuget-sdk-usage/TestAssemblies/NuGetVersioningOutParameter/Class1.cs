using NuGet.Versioning;

namespace NuGetVersioningOutParameter
{
    public class Class1
    {
        public void Method()
        {
            NuGetVersion.TryParse("1.2.3", out _);
        }
    }
}
