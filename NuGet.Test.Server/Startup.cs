using Microsoft.Owin;
using NuGet.Services.BasicSearch;
using NuGet.Services.Metadata.Catalog.Persistence;
using Owin;
using System;
using System.IO;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

[assembly: OwinStartup(typeof(NuGet.Test.Server.Startup))]

namespace NuGet.Test.Server
{
    public class Startup
    {
        State _state;

        public void Configuration(IAppBuilder app)
        {
            app.Run(Invoke);
        }

        async Task Invoke(IOwinContext context)
        {
            if (_state == null)
            {
                _state = new State(GetBaseAddress(context));
                await _state.Load(@"c:\data\test\nuspecs", CancellationToken.None);
            }

            try
            {
                switch (context.Request.Path.Value)
                {
                    case "/":
                        context.Response.StatusCode = (int)HttpStatusCode.OK;
                        await context.Response.WriteAsync("READY");
                        break;
                    case "/debug":
                        await Debug(context);
                        break;
                    // basically
                    case "/v3/query":
                        await ServiceEndpoints.V3SearchAsync(context, _state.SearcherManager);
                        break;
                    case "/v3/autocomplete":
                        await ServiceEndpoints.AutoCompleteAsync(context, _state.SearcherManager);
                        break;
                    default:
                        await Lookup(context);
                        break;
                }
            }
            catch (Exception e)
            {
                throw e;
            }
        }

        Uri GetBaseAddress(IOwinContext context)
        {
            UriBuilder baseAddress = new UriBuilder();
            baseAddress.Scheme = context.Request.Uri.Scheme;
            baseAddress.Host = context.Request.Uri.Host;
            baseAddress.Port = context.Request.Uri.Port;
            baseAddress.Path = "/v3/";
            return baseAddress.Uri;
        }

        async Task Lookup(IOwinContext context)
        {
            Storage storage = _state.CreateStorage();
            StorageContent content = await storage.Load(context.Request.Uri, CancellationToken.None);
            if (content == null)
            {
                await context.Response.WriteAsync("NOT FOUND");
                context.Response.StatusCode = (int)HttpStatusCode.NotFound;
            }
            else
            {
                Stream stream = content.GetContentStream();
                if (stream is MemoryStream)
                {
                    byte[] data = ((MemoryStream)stream).ToArray();
                    await context.Response.WriteAsync(data);
                    context.Response.ContentType = "application/json";
                    context.Response.ContentLength = data.Length;
                    context.Response.StatusCode = (int)HttpStatusCode.OK;
                }
                else
                {
                    context.Response.StatusCode = (int)HttpStatusCode.NotImplemented;
                }
            }
        }

        async Task Debug(IOwinContext context)
        {
            context.Response.StatusCode = (int)HttpStatusCode.OK;

            await context.Response.WriteAsync("<html><body>");
            await context.Response.WriteAsync("<ul>");
            foreach (var registration in _state.Data)
            {
                await context.Response.WriteAsync("<li>");
                await context.Response.WriteAsync(registration.Key);
                await context.Response.WriteAsync("<ul>");
                foreach (var package in registration.Value)
                {
                    await context.Response.WriteAsync("<li>");
                    await context.Response.WriteAsync(package.Key.ToNormalizedString());
                    await context.Response.WriteAsync("</li>");
                }
                await context.Response.WriteAsync("</ul>");
                await context.Response.WriteAsync("</li>");
            }
            await context.Response.WriteAsync("</ul>");
            await context.Response.WriteAsync("</body></html>");
        }
    }
}

