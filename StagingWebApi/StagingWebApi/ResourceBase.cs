using System.Net.Http;
using System.Threading.Tasks;

namespace StagingWebApi
{
    public abstract class ResourceBase
    {
        public abstract Task<HttpResponseMessage> Save();
        public abstract Task<HttpResponseMessage> Load();
        public abstract Task<HttpResponseMessage> Delete();
    }
}
