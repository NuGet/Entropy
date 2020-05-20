using NuGet.Versioning;

namespace NuGetVersioningOperators
{
    public class Class1
    {
        public int Method()
        {
            var v1 = NuGetVersion.Parse("1.0.0");
            var v2 = NuGetVersion.Parse("2.0.0");

            // Use variable to make sure compiler doesn't optimise away method call to operators.
            int result = 0;
            result += v1 == v2 ? 1 : 0;
            result += v1 != v2 ? 1 : 0;
            result += v1 > v2 ? 1 : 0;
            result += v1 >= v2 ? 1 : 0;
            result += v1 < v2 ? 1 : 0;
            result += v1 <= v2 ? 1 : 0;

            return result;
        }
    }
}
