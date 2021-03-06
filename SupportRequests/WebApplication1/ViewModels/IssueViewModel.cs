﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using WebApplication1.Models;
using System.Web.Mvc;

namespace WebApplication1.ViewModels
{
    public class IssueViewModel
    {
        public Issue Issue { get; set; }
        public List<SelectListItem> AssignedTo { get; set; }
        public List<SelectListItem> IssueStatusName { get; set; }

        public string AssignedToLabel { get; set; }
        public string IssueStatusNameLabel { get; set; }

        public string OwnerLink { get; set; }
        public string PackageLink { get; set; }

    }
}