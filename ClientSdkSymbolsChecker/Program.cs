using ClientSdkSymbolsChecker;
using NuGet.Common;
using NuGet.Configuration;
using NuGet.Packaging;
using NuGet.Packaging.Core;
using NuGet.Packaging.Signing;
using NuGet.Protocol;
using NuGet.Protocol.Core.Types;
using NuGet.Repositories;
using NuGet.Versioning;

string[] packageIds = new[] {
    "Microsoft.Build.NuGetSdkResolver",
    "NuGet.Build.Tasks",
    "NuGet.Build.Tasks.Console",
    "NuGet.Build.Tasks.Pack",
    "NuGet.CommandLine",
    "NuGet.CommandLine.XPlat",
    "NuGet.Commands",
    "NuGet.Common",
    "NuGet.Configuration",
    "NuGet.Credentials",
    "NuGet.DependencyResolver.Core",
    "NuGet.Frameworks",
    "NuGet.Indexing",
    "NuGet.LibraryModel",
    "NuGet.Localization",
    "NuGet.PackageManagement",
    "NuGet.Packaging",
    "NuGet.Packaging.Core",
    "NuGet.Packaging.Extraction",
    "NuGet.ProjectModel",
    "NuGet.Protocol",
    "NuGet.Resolver",
    "NuGet.Versioning",
    "NuGet.VisualStudio",
    "NuGet.VisualStudio.Contracts",
    };

UserAgent.SetUserAgentString(new UserAgentStringBuilder("NuGetSdkSymbolChecker"));

var globalContext = new GlobalContext();
Dictionary<string, IReadOnlyList<NuGetVersion>> allPackages = await GetAllPackageVersionsAsync(packageIds, globalContext, CancellationToken.None);

static async Task<Dictionary<string, IReadOnlyList<NuGetVersion>>> GetAllPackageVersionsAsync(IReadOnlyList<string> packageIds, GlobalContext globalContext, CancellationToken cancellationToken)
{
    Task<IReadOnlyList<NuGetVersion>>[] tasks = new Task<IReadOnlyList<NuGetVersion>>[packageIds.Count];
    NuGetDownloader downloader = await NuGetDownloader.CreateAsync(cancellationToken);

    using var linkedCancellationToken = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

    int packageIdIndex = 0;

    for (int i = 0; i <  Math.Min(tasks.Length, packageIds.Count); i++)
    {
        tasks[i] = downloader.GetAllVersionsAsync(packageIds[packageIdIndex], linkedCancellationToken.Token);
    }

    var result = new Dictionary<string, IReadOnlyList<>>

    while (packageIdIndex < packageIds.Count)
    {
        await Task.WhenAny(tasks);
        Task<IReadOnlyList<NuGetVersion>> finishedTask;
        for (int finishedIndex = 0; ; finishedIndex++)
        {
            finishedTask = tasks[finishedIndex];
            if (finishedTask.IsCompleted)
            {
                break;
            }
        }

        if (finishedTask.IsFaulted)
        {
            linkedCancellationToken.Cancel();
            await Task.WhenAll(tasks);
            await finishedTask;
            // above should throw, but let's be explicit about program flow exiting here
            throw null;
        }


    }
}


var settings = Settings.LoadDefaultSettings(Environment.CurrentDirectory);
var gpfPath = SettingsUtility.GetGlobalPackagesFolder(settings);
NuGetv3LocalRepository gpf = new NuGetv3LocalRepository(gpfPath);

var sourceCacheContext = new SourceCacheContext();
var clientPolicy = ClientPolicyContext.GetClientPolicy(settings, NullLogger.Instance);
var packageExtractionContext = new PackageExtractionContext(PackageSaveMode.Defaultv3, XmlDocFileSaveMode.None, clientPolicy, NullLogger.Instance);

var packageSource = new PackageSource("https://api.nuget.org/v3/index.json");
var source = Repository.Factory.GetCoreV3(packageSource);

var fpbir = await source.GetResourceAsync<FindPackageByIdResource>();
var packageMetadataResource = await source.GetResourceAsync<PackageMetadataResource>();

foreach (var packageId in packageIds)
{
    var versions = (await fpbir.GetAllVersionsAsync(packageId, sourceCacheContext, NullLogger.Instance, CancellationToken.None)).ToList();
    foreach (var version in versions)
    {
        
        if (!gpf.Exists(packageId, version))
        {
            Console.WriteLine("{0} version {1} is not downloaded", packageId, version.OriginalVersion);
            var packageIdentity = new PackageIdentity(packageId, version);
            using (var packageDownloader = await fpbir.GetPackageDownloaderAsync(packageIdentity, sourceCacheContext, NullLogger.Instance, CancellationToken.None))
            {
                await PackageExtractor.InstallFromSourceAsync(packageIdentity, packageDownloader, gpf.PathResolver, packageExtractionContext, CancellationToken.None);
            }
        }
        else
        {

        }
    }
}
