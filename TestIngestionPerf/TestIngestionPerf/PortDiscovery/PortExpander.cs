using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace TestIngestionPerf
{
    public class PortExpander
    {
        private static readonly TimeSpan ConnectTimeout = TimeSpan.FromSeconds(10);

        private readonly IPortDiscoverer _portDiscoverer;

        public PortExpander(IPortDiscoverer portDiscoverer)
        {
            _portDiscoverer = portDiscoverer;
        }

        public async Task<IReadOnlyList<string>> ExpandSequentialOpenPortsAsync(int startingPort, IReadOnlyList<string> urls)
        {
            // Parse all of the URLs as URIs.
            var uris = urls
                .Select(x => new Uri(x, UriKind.Absolute))
                .ToList();

            // Determine the available ports on each host.
            var hostTasks = uris
                .GroupBy(x => x.Host, StringComparer.OrdinalIgnoreCase)
                .Where(x => x
                    .Select(u => u.Scheme)
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .Single() != null)
                .Select(x => x.First())
                .Select(x => new
                {
                    x.Host,
                    PortsTask = _portDiscoverer.FindPortsAsync(
                        x.Host,
                        startingPort,
                        connectTimeout: ConnectTimeout),
                })
                .ToList();
            await Task.WhenAll(hostTasks.Select(x => x.PortsTask));

            // Build a mapping from host to list of open instance ports.
            var hostToPorts = hostTasks.ToDictionary(
                x => x.Host,
                x => x.PortsTask.Result,
                StringComparer.OrdinalIgnoreCase);

            // Build URLs to the diagnostic endpoint on all search instances.
            var expandedUrls = uris
                .SelectMany(x => hostToPorts[x.Host]
                    .Select(p => new UriBuilder(x)
                    {
                        Port = p,
                    }))
                .Select(x => x.Uri.AbsoluteUri)
                .ToList();

            return expandedUrls;
        }
    }
}
