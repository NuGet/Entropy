using System;
using System.Threading.Tasks;

namespace TestIngestionPerf
{
    public interface IPortTester
    {
        Task<bool> IsPortOpenAsync(string host, int port, TimeSpan connectTimeout);
    }
}
