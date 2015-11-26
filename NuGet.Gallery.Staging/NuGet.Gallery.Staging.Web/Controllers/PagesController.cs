// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Web.Mvc;
using NuGet.Gallery.Staging.Web.Code.Mvc;

namespace NuGet.Gallery.Staging.Web.Controllers
{
    public class PagesController 
        : BaseController
    {  
        public ActionResult Terms()
        {
            return View();
        }

        public ActionResult Privacy()
        {
            return View();
        }

        public ActionResult Contact()
        {
            return View();
        }
    }
}