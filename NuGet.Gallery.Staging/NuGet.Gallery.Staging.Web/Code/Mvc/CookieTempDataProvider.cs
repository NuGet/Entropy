using System;
using System.Collections.Generic;
using System.Globalization;
using System.Web;
using System.Web.Mvc;

namespace NuGet.Gallery.Staging.Web.Code.Mvc
{
    public class CookieTempDataProvider : ITempDataProvider
    {
        private const string _tempDataCookieKey = "__Controller::TempData";

        // Methods
        public CookieTempDataProvider(HttpContextBase httpContext)
        {
            if (httpContext == null)
            {
                throw new ArgumentNullException("httpContext");
            }
            HttpContext = httpContext;
        }

        private bool CookieHasTempData
        {
            get
            {
                return ((HttpContext.Response != null) && (HttpContext.Response.Cookies != null)) &&
                       (HttpContext.Response.Cookies[_tempDataCookieKey] != null);
            }
        }

        public HttpContextBase HttpContext { get; }

        IDictionary<string, object> ITempDataProvider.LoadTempData(ControllerContext controllerContext)
        {
            return LoadTempData(controllerContext);
        }

        void ITempDataProvider.SaveTempData(ControllerContext controllerContext, IDictionary<string, object> values)
        {
            SaveTempData(controllerContext, values);
        }

        protected virtual IDictionary<string, object> LoadTempData(ControllerContext controllerContext)
        {
            var cookie = HttpContext.Request.Cookies[_tempDataCookieKey];
            var dictionary = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
            if ((cookie == null) || String.IsNullOrEmpty(cookie.Value))
            {
                return dictionary;
            }
            foreach (var key in cookie.Values.AllKeys)
            {
                dictionary[key] = cookie[key];
            }
            cookie.Expires = DateTime.MinValue;
            cookie.Value = String.Empty;
            if (CookieHasTempData)
            {
                cookie.Expires = DateTime.MinValue;
                cookie.Value = String.Empty;
            }
            return dictionary;
        }

        protected virtual void SaveTempData(ControllerContext controllerContext, IDictionary<string, object> values)
        {
            if (values.Count > 0)
            {
                var cookie = new HttpCookie(_tempDataCookieKey);
                cookie.HttpOnly = true;
                foreach (var item in values)
                {
                    cookie[item.Key] = Convert.ToString(item.Value, CultureInfo.InvariantCulture);
                }
                HttpContext.Response.Cookies.Add(cookie);
            }
        }

        // Properties
    }
}