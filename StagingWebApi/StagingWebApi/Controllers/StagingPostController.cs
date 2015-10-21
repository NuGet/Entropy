using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Http;

namespace StagingWebApi.Controllers
{
    public class StagingPostController : ApiController
    {
        public StagingPostController()
        {
            StagePersistenceFactory factory = new StagePersistenceFactory();
            Persistence = factory.Create();
        }

        private IStagePersistence Persistence { get; set; }

        [Route("create/stage")]
        [HttpPost]
        public async Task<HttpResponseMessage> PostStage()
        {
            try
            {
                Stream stream = await Request.Content.ReadAsStreamAsync();

                StageDefinition definition = StageDefinition.ReadFromStream(stream);

                if (definition.IsValid)
                {
                    return await Persistence.CreateStage(definition.OwnerName, definition.StageName, definition.BaseService);
                }
                else
                {
                    return Utils.CreateErrorResponse(HttpStatusCode.BadRequest, definition.Reason);
                }
            }
            catch (Exception e)
            {
                Trace.TraceError(e.Message);
                return new HttpResponseMessage(HttpStatusCode.InternalServerError);
            }
        }

        [Route("create/package/{stageOwner}/{stageName}")]
        [HttpPost]
        public async Task<HttpResponseMessage> PostPackage(string stageOwner, string stageName, string owner)
        {
            Uri nupkgLocation = null;
            Uri nuspecLocation = null;
            PackageStorageBase storage = null;
            bool storageSaveSuccess = false;
            bool tryStorageCleanup = false;
            HttpResponseMessage response;

            try
            {
                Stream stream = await Request.Content.ReadAsStreamAsync();

                StagePackage package = StagePackage.ReadFromStream(stream);

                if (package.IsValid)
                {
                    Uri baseAddress = Utils.CreateBaseAddress(Request.RequestUri, "stage/");

                    //TODO: currently the Location is storage URI to the blob, it might be more flexible to make this the blob name

                    storage = new AzurePackageStorage();
                    nupkgLocation = await storage.Save(stream, package.GetNupkgName(storage.Root), package.GetNupkgName(string.Empty), "application/octet-stream");
                    nuspecLocation = await storage.Save(package.NuspecStream, package.GetNuspecName(storage.Root), package.GetNuspecName(string.Empty), "text/xml");

                    storageSaveSuccess = true;

                    response = await Persistence.CreatePackage(baseAddress, stageOwner, stageName, package.Id, package.Version, owner, nupkgLocation, nuspecLocation);

                    if (response.StatusCode != HttpStatusCode.Created)
                    {
                        tryStorageCleanup = true;
                    }
                }
                else
                {
                    response = Utils.CreateErrorResponse(HttpStatusCode.BadRequest, package.Reason);
                }
            }
            catch (Exception e)
            {
                Trace.TraceError(e.Message);
                tryStorageCleanup = true;
                response = new HttpResponseMessage(HttpStatusCode.InternalServerError);
            }

            if (storageSaveSuccess && tryStorageCleanup)
            {
                try
                {
                    await storage.Delete(nupkgLocation);
                    await storage.Delete(nuspecLocation);
                }
                catch (Exception e)
                {
                    Trace.TraceWarning(e.Message);
                }
            }

            return response;
        }
    }
}
