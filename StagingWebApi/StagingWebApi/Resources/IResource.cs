using System.Net.Http;
using System.Threading.Tasks;

namespace StagingWebApi.Resources
{
    public interface IResource
    {
        Task<HttpResponseMessage> Save();
        Task<HttpResponseMessage> Load();
        Task<HttpResponseMessage> Delete();
    }
}
