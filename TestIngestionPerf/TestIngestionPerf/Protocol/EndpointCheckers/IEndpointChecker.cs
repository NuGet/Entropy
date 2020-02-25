using System.Threading.Tasks;
using NuGet.Versioning;

namespace TestIngestionPerf
{
    public interface IEndpointChecker
    {
        string Name { get; }
        Task<bool> DoesPackageExistAsync(string id, NuGetVersion version);
    }
}
