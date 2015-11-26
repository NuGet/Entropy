<<<<<<< HEAD
﻿using System;
using System.Diagnostics;
using System.IO;
=======
﻿// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.
using Newtonsoft.Json.Linq;
using System;
using System.Diagnostics;
>>>>>>> 15898dffd7c655c67c3d2a9a02c8142b328fef7d
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

<<<<<<< HEAD
=======
        // todo: secure this, this is a temporary method for demo purposes only - hey maarten - hacked this around a little
        [Route("create/owner")]
        [HttpPost]
        public async Task<HttpResponseMessage> PostOwner()
        {
            try
            {
                var json = await Request.Content.ReadAsStringAsync();
                JObject ownerRequest = null;
                try
                {
                    ownerRequest = JObject.Parse(json);
                }
                catch (Exception)
                {
                    return new HttpResponseMessage(HttpStatusCode.BadRequest);
                }

                var ownerName = (string)ownerRequest["ownerName"];
                if (ownerRequest == null)
                {
                    return new HttpResponseMessage(HttpStatusCode.BadRequest);
                }

                if (!string.IsNullOrEmpty(ownerName))
                {
                    return await Persistence.CreateOwner(ownerName);
                }

                return new HttpResponseMessage(HttpStatusCode.BadRequest);
            }
            catch (Exception e)
            {
                Trace.TraceError(e.Message);
                return new HttpResponseMessage(HttpStatusCode.InternalServerError);
            }
        }

        [Route("add")]
        [HttpPost]
        public async Task<HttpResponseMessage> Add()
        {
            try
            {
                var json = await Request.Content.ReadAsStringAsync();
                JObject obj = null;
                try
                {
                    obj = JObject.Parse(json);
                }
                catch (Exception)
                {
                    return new HttpResponseMessage(HttpStatusCode.BadRequest);
                }

                var id = (string)obj["@id"];
                if (id == null)
                {
                    return new HttpResponseMessage(HttpStatusCode.BadRequest);
                }

                //TODO: we should be able to look up the @id and determine the necessary details
                //TODO: alternatively - and even simpler - we should just be able to add the property to the persistance

                // but for now we can strip this data from the @id sad as that might sound :-(
                var fields = id.Split('/');
                if (fields.Length != 3)
                {
                    return new HttpResponseMessage(HttpStatusCode.BadRequest);
                }
                var ownerName = fields[1];
                var stageName = fields[2];

                var newOwnerName = (string)obj["owner"];
                if (newOwnerName == null)
                {
                    return new HttpResponseMessage(HttpStatusCode.BadRequest);
                }

                return await Persistence.AddStageOwner(ownerName, stageName, newOwnerName);
            }
            catch (Exception e)
            {
                Trace.TraceError(e.Message);
                return new HttpResponseMessage(HttpStatusCode.InternalServerError);
            }
        }

>>>>>>> 15898dffd7c655c67c3d2a9a02c8142b328fef7d
        [Route("create/stage")]
        [HttpPost]
        public async Task<HttpResponseMessage> PostStage()
        {
            try
            {
<<<<<<< HEAD
                Stream stream = await Request.Content.ReadAsStreamAsync();

                StageDefinition definition = StageDefinition.ReadFromStream(stream);
=======
                var stream = await Request.Content.ReadAsStreamAsync();

                var definition = StageDefinition.ReadFromStream(stream);
>>>>>>> 15898dffd7c655c67c3d2a9a02c8142b328fef7d

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

<<<<<<< HEAD
        [Route("create/package/{stageOwner}/{stageName}")]
        [HttpPost]
        public async Task<HttpResponseMessage> PostPackage(string stageOwner, string stageName, string owner)
=======
        [Route("create/package/{stageOwner}/{stageName}/{packageOwner}")]
        [HttpPost]
        public async Task<HttpResponseMessage> PostPackage(string stageOwner, string stageName, string packageOwner)
>>>>>>> 15898dffd7c655c67c3d2a9a02c8142b328fef7d
        {
            Uri nupkgLocation = null;
            Uri nuspecLocation = null;
            PackageStorageBase storage = null;
            bool storageSaveSuccess = false;
            bool tryStorageCleanup = false;
            HttpResponseMessage response;

            try
            {
<<<<<<< HEAD
                Stream stream = await Request.Content.ReadAsStreamAsync();

                StagePackage package = StagePackage.ReadFromStream(stream);
=======
                var stream = await Request.Content.ReadAsStreamAsync();

                var package = StagePackage.ReadFromStream(stream);
>>>>>>> 15898dffd7c655c67c3d2a9a02c8142b328fef7d

                if (package.IsValid)
                {
                    Uri baseAddress = Utils.CreateBaseAddress(Request.RequestUri, "stage/");

                    //TODO: currently the Location is storage URI to the blob, it might be more flexible to make this the blob name

                    storage = new AzurePackageStorage();
                    nupkgLocation = await storage.Save(stream, package.GetNupkgName(storage.Root), package.GetNupkgName(string.Empty), "application/octet-stream");
                    nuspecLocation = await storage.Save(package.NuspecStream, package.GetNuspecName(storage.Root), package.GetNuspecName(string.Empty), "text/xml");

                    storageSaveSuccess = true;

<<<<<<< HEAD
                    response = await Persistence.CreatePackage(baseAddress, stageOwner, stageName, package.Id, package.Version, owner, nupkgLocation, nuspecLocation);
=======
                    response = await Persistence.CreatePackage(baseAddress, stageOwner, stageName, package.Id, package.Version, packageOwner, nupkgLocation, nuspecLocation);
>>>>>>> 15898dffd7c655c67c3d2a9a02c8142b328fef7d

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
