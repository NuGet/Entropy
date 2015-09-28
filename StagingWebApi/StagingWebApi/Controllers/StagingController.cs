using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using System.Web.Http;

namespace StagingWebApi.Controllers
{
    public class StagingController : ApiController
    {
        [Route("stage/{owner}/{name}")]
        [HttpPut]
        public async Task<HttpResponseMessage> PutStage(string owner, string name)
        {
            ResourceBase resource = new StageResource(owner, name);
            return await resource.Save();
        }

        [Route("stage/{owner}/{name}")]
        [HttpGet]
        public async Task<HttpResponseMessage> GetStage(string owner, string name)
        {
            if (Request.Headers.Accept.Contains(new MediaTypeWithQualityHeaderValue("text/html")))
            {
                HttpResponseMessage redirect = new HttpResponseMessage(HttpStatusCode.Redirect);
                redirect.Headers.Location = new Uri(Request.RequestUri.GetLeftPart(UriPartial.Authority) + "/content/show.html#" + Request.RequestUri.AbsoluteUri);
                return redirect;
            }
            else
            {
                ResourceBase resource = new StageResource(owner, name);
                return await resource.Load();
            }
        }

        [Route("stage/{owner}/{name}")]
        [HttpDelete]
        public async Task<HttpResponseMessage> DeleteStage(string owner, string name)
        {
            StageResource stageResource = new StageResource(owner, name);
            HttpResponseMessage response = await stageResource.Delete();

            if (response.IsSuccessStatusCode)
            {
                PackageStorageBase storage = new AzurePackageStorage();
                foreach (PackageResource packageResource in stageResource.Packages)
                {
                    try
                    {
                        await storage.Delete(packageResource.NupkgLocation);
                        await storage.Delete(packageResource.NuspecLocation);
                    }
                    catch (Exception e)
                    {
                        Trace.TraceWarning(e.Message);
                    }
                }
            }

            return response;
        }

        [Route("upload/{owner}/{name}")]
        [HttpPost]
        public async Task<HttpResponseMessage> PostPackage(string owner, string name)
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
                    //TODO: currently the Location is storage URI to the blob, it might be more flexible to make this the blob name

                    storage = new AzurePackageStorage();

                    nupkgLocation = await storage.Save(stream, package.GetNupkgName(storage.Root), package.GetNupkgName(string.Empty), "application/octet-stream");
                    nuspecLocation = await storage.Save(package.NuspecStream, package.GetNuspecName(storage.Root), package.GetNuspecName(string.Empty), "text/xml");

                    storageSaveSuccess = true;

                    ResourceBase resource = new PackageResource(owner, name, package, nupkgLocation, nuspecLocation);
                    response = await resource.Save();

                    if (response.StatusCode != HttpStatusCode.Created)
                    {
                        tryStorageCleanup = true;
                    }
                }
                else
                {
                    response = new HttpResponseMessage(HttpStatusCode.BadRequest);
                    response.Content = Utils.CreateErrorContent(package.Reason);
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

        [Route("stage/{owner}/{name}/{id}/{version}")]
        [HttpGet]
        public async Task<HttpResponseMessage> GetPackage(string owner, string name, string id, string version)
        {
            if (Request.Headers.Accept.Contains(new MediaTypeWithQualityHeaderValue("text/html")))
            {
                HttpResponseMessage redirect = new HttpResponseMessage(HttpStatusCode.Redirect);
                redirect.Headers.Location = new Uri(Request.RequestUri.GetLeftPart(UriPartial.Authority) + "/content/show.html#" + Request.RequestUri.AbsoluteUri);
                return redirect;
            }
            else
            {
                PackageResource resource = new PackageResource(owner, name, new StagePackage { Id = id, Version = version });
                HttpResponseMessage response = await resource.Load();

                if (response.IsSuccessStatusCode)
                {
                    HttpResponseMessage redirect = new HttpResponseMessage(HttpStatusCode.Redirect);
                    redirect.Headers.Location = resource.NupkgLocation;
                    return redirect;
                }
                else
                {
                    return response;
                }
            }
        }

        [Route("stage/{owner}/{name}/{id}/{version}")]
        [HttpDelete]
        public async Task<HttpResponseMessage> DeletePackage(string owner, string name, string id, string version)
        {
            PackageResource resource = new PackageResource(owner, name, new StagePackage { Id = id, Version = version });
            HttpResponseMessage response = await resource.Delete();

            if (response.IsSuccessStatusCode)
            {
                try
                {
                    PackageStorageBase storage = new AzurePackageStorage();
                    await storage.Delete(resource.NupkgLocation);
                    await storage.Delete(resource.NuspecLocation);

                    //TODO: consider two phase updates on database
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
