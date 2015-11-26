<<<<<<< HEAD
﻿using System;
=======
﻿// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.
using System;
>>>>>>> 15898dffd7c655c67c3d2a9a02c8142b328fef7d
using System.Diagnostics;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using System.Web.Http;

namespace StagingWebApi.Controllers
{
    public class StagingController : ApiController
    {
        public StagingController()
        {
            StagePersistenceFactory factory = new StagePersistenceFactory();
            Persistence = factory.Create();
        }

        private IStagePersistence Persistence { get; set; }

        private HttpResponseMessage RedirectHtml()
        {
            HttpResponseMessage redirect = new HttpResponseMessage(HttpStatusCode.Redirect);
            redirect.Headers.Location = new Uri(Request.RequestUri.GetLeftPart(UriPartial.Authority) + "/content/show.html#" + Request.RequestUri.AbsoluteUri);
            return redirect;
        }

        private bool IsHTMLRequest()
        {
            return Request.Headers.Accept.Contains(new MediaTypeWithQualityHeaderValue("text/html"));
        }

<<<<<<< HEAD
=======
        private string MakeV3SourceBaseAddress()
        {
            return string.Format("{0}://{1}/source/v3/", Request.RequestUri.Scheme, Request.RequestUri.Authority);
        }

>>>>>>> 15898dffd7c655c67c3d2a9a02c8142b328fef7d
        // basic GETs to access the state of the staging area - if HTML is requested they all redirect to a single show.html page which calls back for the JSON

        [Route("stage/{owner}")]
        [HttpGet]
        public async Task<HttpResponseMessage> GetOwner(string owner)
        {
            try
            {
                if (IsHTMLRequest())
                {
                    return RedirectHtml();
                }
                else
                {
<<<<<<< HEAD
                    return await Persistence.GetOwner(owner);
=======
                    return await Persistence.GetOwner(owner, MakeV3SourceBaseAddress());
>>>>>>> 15898dffd7c655c67c3d2a9a02c8142b328fef7d
                }
            }
            catch (Exception e)
            {
                Trace.TraceError(e.Message);
                return new HttpResponseMessage(HttpStatusCode.InternalServerError);
            }
        }

        [Route("stage/{owner}/{name}")]
        [HttpGet]
        public async Task<HttpResponseMessage> GetStage(string owner, string name)
        {
            try
            {
                if (IsHTMLRequest())
                {
                    return RedirectHtml();
                }
                else
                {
<<<<<<< HEAD
                    return await Persistence.GetStage(owner, name);
=======
                    return await Persistence.GetStage(owner, name, MakeV3SourceBaseAddress());
>>>>>>> 15898dffd7c655c67c3d2a9a02c8142b328fef7d
                }
            }
            catch (Exception e)
            {
                Trace.TraceError(e.Message);
                return new HttpResponseMessage(HttpStatusCode.InternalServerError);
            }
        }

        [Route("stage/{owner}/{name}/{id}")]
        [HttpGet]
        public async Task<HttpResponseMessage> GetPackage(string owner, string name, string id)
        {
            try
            {
                if (IsHTMLRequest())
                {
                    return RedirectHtml();
                }
                else
                {
                    return await Persistence.GetPackage(owner, name, id);
                }
            }
            catch (Exception e)
            {
                Trace.TraceError(e.Message);
                return new HttpResponseMessage(HttpStatusCode.InternalServerError);
            }
        }

        [Route("stage/{owner}/{name}/{id}/{version}")]
        [HttpGet]
        public async Task<HttpResponseMessage> GetPackageVersion(string owner, string name, string id, string version)
        {
            try
            {
                if (IsHTMLRequest())
                {
                    return RedirectHtml();
                }
                else
                {
                    return await Persistence.GetPackageVersion(owner, name, id, version);
                }
            }
            catch (Exception e)
            {
                Trace.TraceError(e.Message);
                return new HttpResponseMessage(HttpStatusCode.InternalServerError);
            }
        }

        //  These DELETE are on the same routes and so the same Controller

        [Route("stage/{owner}/{name}")]
        [HttpDelete]
        public async Task<HttpResponseMessage> DeleteStage(string owner, string name)
        {
            try
            {
                var result = await Persistence.DeleteStage(owner, name);
                if (result.Item1.IsSuccessStatusCode)
                {
                    await Utils.CleanUpArtifacts(result.Item2);
                }
                return result.Item1;
            }
            catch (Exception e)
            {
                Trace.TraceError(e.Message);
                return new HttpResponseMessage(HttpStatusCode.InternalServerError);
            }
        }

        [Route("stage/{owner}/{name}/{id}")]
        [HttpDelete]
        public async Task<HttpResponseMessage> DeletePackage(string owner, string name, string id)
        {
            try
            {
                var result = await Persistence.DeletePackage(owner, name, id);
                if (result.Item1.IsSuccessStatusCode)
                {
                    await Utils.CleanUpArtifacts(result.Item2);
                }
                return result.Item1;
            }
            catch (Exception e)
            {
                Trace.TraceError(e.Message);
                return new HttpResponseMessage(HttpStatusCode.InternalServerError);
            }
        }

        [Route("stage/{owner}/{name}/{id}/{version}")]
        [HttpDelete]
        public async Task<HttpResponseMessage> DeletePackageVersion(string owner, string name, string id, string version)
        {
            try
            {
                var result = await Persistence.DeletePackageVersion(owner, name, id, version);
                if (result.Item1.IsSuccessStatusCode)
                {
                    await Utils.CleanUpArtifacts(result.Item2);
                }
                return result.Item1;
            }
            catch (Exception e)
            {
                Trace.TraceError(e.Message);
                return new HttpResponseMessage(HttpStatusCode.InternalServerError);
            }
        }
    }
}
