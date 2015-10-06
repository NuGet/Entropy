using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using WebApplication1.Models;

namespace WebApplication1.ViewModels
{
    public class HistoryViewModel
    {
        public History History { get; set; }
        public string EditedBy { get; set; }
    }
}