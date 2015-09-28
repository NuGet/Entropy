// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Http;

namespace StagingWebApi.Controllers
{
    public class StagingPushController : ApiController
    {
        public StagingPushController()
        {
            StagePersistenceFactory factory = new StagePersistenceFactory();
            Persistence = factory.Create();
        }

        private IStagePersistence Persistence { get; set; }

        [Route("push/package/{stageOwner}/{stageName}")]
        [HttpPut]
        public async Task<HttpResponseMessage> PushPackage(string stageOwner, string stageName)
        {
            var apiKey = Request.Headers.GetValues("X-NuGet-ApiKey").FirstOrDefault();
            if (apiKey == null)
            {
                return new HttpResponseMessage(HttpStatusCode.Forbidden);
            }

            // basic model implemented here: the package owner must be a co-owner of the stage
            var packageOwner = await Persistence.CheckAccess(stageName, apiKey);
            if (packageOwner == null)
            {
                return new HttpResponseMessage(HttpStatusCode.Forbidden);
            }

            if (!Request.Content.IsMimeMultipartContent())
            {
                return new HttpResponseMessage(HttpStatusCode.BadRequest);
            }

            var multiPart = await Request.Content.ReadAsMultipartAsync();
            if (multiPart.Contents.Count != 1)
            {
                return new HttpResponseMessage(HttpStatusCode.BadRequest);
            }

            using (var stream = await multiPart.Contents[0].ReadAsStreamAsync())
            {
                return await ProcessPackage(stream, stageOwner, stageName, packageOwner);
            }
        }

        private async Task<HttpResponseMessage> ProcessPackage(Stream stream, string stageOwner, string stageName, string packageOwner)
        {
            Uri nupkgLocation = null;
            Uri nuspecLocation = null;
            PackageStorageBase storage = null;
            bool storageSaveSuccess = false;
            bool tryStorageCleanup = false;
            HttpResponseMessage response;

            try
            {
                var package = StagePackage.ReadFromStream(stream);

                if (package.IsValid)
                {
                    Uri baseAddress = Utils.CreateBaseAddress(Request.RequestUri, "stage/");

                    //TODO: currently the Location is storage URI to the blob, it might be more flexible to make this the blob name

                    storage = new AzurePackageStorage();
                    nupkgLocation = await storage.Save(stream, package.GetNupkgName(storage.Root), package.GetNupkgName(string.Empty), "application/octet-stream");
                    nuspecLocation = await storage.Save(package.NuspecStream, package.GetNuspecName(storage.Root), package.GetNuspecName(string.Empty), "text/xml");

                    storageSaveSuccess = true;

                    response = await Persistence.CreatePackage(baseAddress, stageOwner, stageName, package.Id, package.Version, packageOwner, nupkgLocation, nuspecLocation);

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
