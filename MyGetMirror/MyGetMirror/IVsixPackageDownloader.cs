using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace MyGetMirror
{
    public interface IVsixPackageDownloader
    {
        Task<bool> IsAvailableAsync(string id, string version, CancellationToken token);
        Task<T> ProcessAsync<T>(string id, string version, Func<StreamResult, Task<T>> processAsync, CancellationToken token);
    }
}
