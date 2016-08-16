using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
using NuGet.Common;
using NuGet.Protocol;

namespace MyGetMirror
{
    public class VsixPackageEnumerator
    {
        private const string AtomXmlns = "http://www.w3.org/2005/Atom";
        private const string VsixXmlns = "http://schemas.microsoft.com/developer/vsx-syndication-schema/2010";

        private readonly HttpSource _httpSource;
        private readonly ILogger _logger;
        private readonly string _source;

        public VsixPackageEnumerator(string source, HttpSource httpSource, ILogger logger)
        {
            _source = source;
            _httpSource = httpSource;
            _logger = logger;
        }

        public async Task<IReadOnlyList<VsixPackage>> EnumerateAsync(CancellationToken token)
        {
            var request = new HttpSourceRequest(_source, _logger);
            request.IgnoreNotFounds = false;

            return await _httpSource.ProcessStreamAsync(
                request,
                stream =>
                {
                    var doc = XDocument.Load(stream);
                    var entries = doc.Root.Elements(XName.Get("entry", AtomXmlns));

                    var packages = new List<VsixPackage>();

                    foreach (var entry in entries)
                    {
                        var contentElement = entry.Element(XName.Get("content", AtomXmlns));
                        var contentSrcAttribute = contentElement.Attribute("src");
                        var contentSrc = contentSrcAttribute.Value;

                        var vsix = entry.Element(XName.Get("Vsix", VsixXmlns));

                        var idElement = vsix.Element(XName.Get("Id", VsixXmlns));
                        var id = idElement.Value;

                        var versionElement = vsix.Element(XName.Get("Version", VsixXmlns));
                        var version = versionElement.Value;

                        var package = new VsixPackage
                        {
                            Id = id,
                            Version = version,
                            Url = contentSrc
                        };

                        packages.Add(package);
                    }

                    return Task.FromResult(packages);
                },
                _logger,
                token);
        }
    }
}
