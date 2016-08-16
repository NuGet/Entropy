using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace MyGetMirror
{
    public interface IVsixPackagePusher
    {
        Task PushAsync(Stream packageStream, CancellationToken token);
    }
}