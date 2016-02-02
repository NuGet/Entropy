using Lucene.Net.Store;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Debug;
using Ng;
using NuGet.Indexing;
using NuGet.Services.Metadata.Catalog;
using NuGet.Services.Metadata.Catalog.Persistence;
using NuGet.Services.Metadata.Catalog.Registration;
using NuGet.Versioning;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace NuGet.Test.Server
{
    public class State
    {
        const string Catalog = "catalog/";
        const string Registration = "registration/";
        const string Flat = "flat";

        Uri _baseAddress;
        IDictionary<Uri, MemoryStorageEntry> _store;

        public IDictionary<string, IDictionary<NuGetVersion, XDocument>> Data
        {
            get; private set;
        }

        public NuGetSearcherManager SearcherManager
        {
            get; private set;
        }

        public State(Uri baseAddress)
        {
            Data = new Dictionary<string, IDictionary<NuGetVersion, XDocument>>();
            _baseAddress = baseAddress;
            _store = new Dictionary<Uri, MemoryStorageEntry>();
        }

        public Storage CreateStorage()
        {
            var factory = new MemoryStorageFactory(_baseAddress, _store);
            return factory.Create();
        }

        public async Task Load(string path, CancellationToken cancellationToken)
        {
            var directoryInfo = new DirectoryInfo(path);

            foreach (var fileInfo in directoryInfo.EnumerateFiles("*.nuspec"))
            {
                AddNuspec(fileInfo);
            }

            //  Catalog

            var factory = new MemoryStorageFactory(new Uri(_baseAddress, Catalog), _store);
            var storage = factory.Create();

            var catalog = new AppendOnlyCatalogWriter(storage);

            foreach (var registration in Data.Values)
            {
                foreach (var package in registration.Values)
                {
                    var metadata = new NupkgMetadata
                    {
                        Nuspec = package
                    };
                    catalog.Add(new PackageCatalogItem(metadata));
                }
            }

            await catalog.Commit(null, cancellationToken);

            Uri catalogIndex = new Uri(storage.BaseAddress, "index.json");

            Func<StorageHttpMessageHandler> handlerFunc = () => { return new StorageHttpMessageHandler(storage); };

            await CreateRegistrationBlobs(catalogIndex, handlerFunc, cancellationToken);
            await CreateFlatContainer(catalogIndex, handlerFunc, cancellationToken);
            await CreateLuceneIndex(catalogIndex, handlerFunc, cancellationToken);
            await CreateIndex(cancellationToken);
        }

        async Task CreateIndex(CancellationToken cancellationToken)
        {
            var index = new Index();

            string queryResource = new Uri(_baseAddress, "query").AbsoluteUri;
            index.Add(queryResource, "@type", "SearchQueryService/3.0.0-beta");
            index.Add(queryResource, "comment", "Query endpoint of NuGet Search service (primary)");

            string flatContainerResource = new Uri(_baseAddress, Flat).AbsoluteUri;
            index.Add(flatContainerResource, "@type", "PackageBaseAddress/3.0.0");
            index.Add(flatContainerResource, "comment", "Base URL of Azure storage where NuGet package registration info for DNX is stored");

            string registrationsBaseUrlResource = new Uri(_baseAddress, Registration).AbsoluteUri;
            index.Add(registrationsBaseUrlResource, "@type", "RegistrationsBaseUrl/3.0.0-beta");
            index.Add(registrationsBaseUrlResource, "comment", "Base URL of Azure storage where NuGet package registration info is stored used by Beta clients");

            string reportAbuseResource = "https://www.nuget.org/packages/{id}/{version}/ReportAbuse";
            index.Add(reportAbuseResource, "@type", "ReportAbuseUriTemplate/3.0.0-beta");
            index.Add(reportAbuseResource, "comment", "URI template used by NuGet Client to construct Report Abuse URL for packages");

            var factory = new MemoryStorageFactory(_baseAddress, _store);
            var storage = factory.Create();

            await storage.Save(new Uri(_baseAddress, "index.json"), new StringStorageContent(index.ToJson(), "application/json"), cancellationToken);
        }

        async Task CreateRegistrationBlobs(Uri catalogIndex, Func<StorageHttpMessageHandler> handlerFunc, CancellationToken cancellationToken)
        {
            var factory = new MemoryStorageFactory(new Uri(_baseAddress, Registration), _store);

            CommitCollector collector = new RegistrationCollector(catalogIndex, factory, handlerFunc)
            {
                ContentBaseAddress = new Uri("http://tempuri.org/content/"),
                UnlistShouldDelete = false
            };

            await collector.Run(
                new MemoryCursor(DateTime.MinValue.ToUniversalTime()),
                new MemoryCursor(DateTime.MaxValue.ToUniversalTime()),
                cancellationToken);
        }

        async Task CreateFlatContainer(Uri catalogIndex, Func<StorageHttpMessageHandler> handlerFunc, CancellationToken cancellationToken)
        {
            var factory = new MemoryStorageFactory(new Uri(_baseAddress, Flat), _store);

            await Task.FromResult(0);
        }

        async Task CreateLuceneIndex(Uri catalogIndex, Func<StorageHttpMessageHandler> handlerFunc, CancellationToken cancellationToken)
        {
            Lucene.Net.Store.Directory luceneDirectory = new RAMDirectory();

            var collector = new SearchIndexFromCatalogCollector(
                catalogIndex,
                luceneDirectory,
                null,
                handlerFunc);

            await collector.Run(
                new MemoryCursor(DateTime.MinValue.ToUniversalTime()),
                new MemoryCursor(DateTime.MaxValue.ToUniversalTime()),
                cancellationToken);

            ILogger logger = new DebugLogger("Lucene");
            ILoader loader = new AuxillaryIndexLoader();

            SearcherManager = new NuGetSearcherManager("memory", logger, luceneDirectory, loader);
            SearcherManager.RegistrationBaseAddress["http"] = new Uri(_baseAddress, Registration);
            SearcherManager.RegistrationBaseAddress["https"] = new Uri(_baseAddress, Registration);
            SearcherManager.Open();
        }

        void AddNuspec(FileInfo fileInfo)
        {
            try
            {
                using (Stream stream = fileInfo.OpenRead())
                {
                    var document = XDocument.Load(stream);

                    var id = ExtractId(document);
                    if (id == null)
                    {
                        return;
                    }

                    var version = ExtractVersion(document);
                    if (version == null)
                    {
                        return;
                    }

                    IDictionary<NuGetVersion, XDocument> versions;
                    if (!Data.TryGetValue(id, out versions))
                    {
                        versions = new Dictionary<NuGetVersion, XDocument>();
                        Data.Add(id, versions);
                    }

                    versions[version] = document;
                }
            }
            catch (Exception)
            {
            }
        }
        string ExtractId(XDocument document)
        {
            string value = Extract("id", document);
            if (value == null)
            {
                throw new InvalidDataException("missing id");
            }
            return value.ToLowerInvariant();
        }
        NuGetVersion ExtractVersion(XDocument document)
        {
            string value = Extract("version", document);
            if (value == null)
            {
                throw new InvalidDataException("missing version");
            }
            NuGetVersion version;
            if (!NuGetVersion.TryParse(value, out version))
            {
                throw new InvalidDataException("invalid version");
            }
            return version;
        }

        string Extract(string localName, XDocument document)
        {
            XElement element = document.Root.DescendantsAndSelf().Elements().Where(d => d.Name.LocalName == localName).FirstOrDefault();
            if (element == null)
            {
                return null;
            }
            return element.Value;
        }
    }
}
