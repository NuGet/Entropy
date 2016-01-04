using NuGet.Services.Metadata.Catalog;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace CatalogReader
{
    class Program
    {
        static async Task Test0()
        {
            var index = new Uri("http://tempuri.org/index.json");
            Func<HttpMessageHandler> handlerFunc = () =>
            {
                return new FileSystemEmulatorHandler
                {
                    RootFolder = @"c:\data\data20151209",
                    BaseAddress = new Uri("http://tempuri.org")
                };
            };

            //SimpleCollector collector = new SimpleCollector(index, handlerFunc);
            SimpleCollector collector = new SimpleCollector(new Uri("https://nugetjohtaylo.blob.core.windows.net/baselinecatalog/index.json"));

            ReadWriteCursor front = new MemoryCursor();
            ReadCursor back = MemoryCursor.Max;

            while (true)
            {
                bool run = false;
                do
                {
                    run = await collector.Run(front, back, CancellationToken.None);
                }
                while (run);

                Thread.Sleep(1 * 1000);
            }
        }

        static void Main(string[] args)
        {
            try
            {
                Test0().Wait();
            }
            catch (Exception e)
            {
                Trace.Listeners.Add(new ConsoleTraceListener());
                Trace.AutoFlush = true;

                Utils.TraceException(e);
            }
        }
    }
}
