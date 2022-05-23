using ClientSdkSymbolsChecker;
using NuGet.Packaging.Core;
using NuGet.Protocol.Core.Types;
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
Console.WriteLine("Checking {0} versions from {1} packages", allPackages.Sum(p => p.Value.Count), allPackages.Count);

var missingPackages = await GetMissingPackageVersionsAsync(allPackages, globalContext, CancellationToken.None);
Console.WriteLine("Need to download {0} packages", missingPackages.Count);
if (missingPackages.Count > 0)
{
    await DownloadMissingPackagesAsync(missingPackages, globalContext, CancellationToken.None);
}

static async Task<Dictionary<string, IReadOnlyList<NuGetVersion>>> GetAllPackageVersionsAsync(IReadOnlyList<string> packageIds, GlobalContext globalContext, CancellationToken cancellationToken)
{
    Task<(string packageId, IReadOnlyList<NuGetVersion> versions)>[] tasks = new Task<(string, IReadOnlyList<NuGetVersion>)>[packageIds.Count];
    NuGetDownloader downloader = await NuGetDownloader.CreateAsync(cancellationToken);

    for (int i = 0; i <  packageIds.Count; i++)
    {
        tasks[i] = downloader.GetAllVersionsAsync(packageIds[i], cancellationToken);
    }

    await Task.WhenAll(tasks);
    var fauledTasks = tasks.Count(t => t.IsFaulted);
    if (fauledTasks > 0)
    {
        var exceptions = new Exception[fauledTasks];
        int index = 0;
        for (int i =0; i < tasks.Length; i++)
        {
            if (tasks[i].IsFaulted)
            {
#pragma warning disable CS8602 // Dereference of a possibly null reference.
#pragma warning disable CS8601 // Possible null reference assignment.
                exceptions[index] = tasks[i].Exception.InnerException;
#pragma warning restore CS8601 // Possible null reference assignment.
#pragma warning restore CS8602 // Dereference of a possibly null reference.
                index++;
            }
        }
        throw new AggregateException(exceptions);
    }

    var packageVersions = new Dictionary<string, IReadOnlyList<NuGetVersion>>();

    for (int i = 0; i < tasks.Length; i++)
    {
        var (packageId, versions) = tasks[i].Result;
        packageVersions[packageId] = versions;
    }

    return packageVersions;
}

static Task<IReadOnlyList<PackageIdentity>> GetMissingPackageVersionsAsync(Dictionary<string, IReadOnlyList<NuGetVersion>> allPackages, GlobalContext globalContext, CancellationToken cancellationToken)
{
    var needToDownload = new List<PackageIdentity>();
    var gpf = globalContext.GlobalPackagesFolder;
    foreach (var (packageId, packageVersions) in allPackages)
    {
        foreach (var packageVersion in packageVersions)
        {
            if (!gpf.Exists(packageId, packageVersion))
            {
                var packageIdentity = new PackageIdentity(packageId, packageVersion);
                needToDownload.Add(packageIdentity);
            }
        }
    }

    IReadOnlyList<PackageIdentity> result = needToDownload;
    return Task.FromResult(result);
}

static async Task DownloadMissingPackagesAsync(IReadOnlyCollection<PackageIdentity> packages, GlobalContext context, CancellationToken cancellationToken)
{
    const int maxParallel = 4;
    var tasks = new List<Task>(maxParallel);
    // Used to cancel pending requests when a task fails.
    var tcs = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

    using (var enumerator = packages.GetEnumerator())
    {
        for (int i = 0; i < maxParallel; i++)
        {
            if (!enumerator.MoveNext())
            {
                break;
            }
            //            using (var packageDownloader = await fpbir.GetPackageDownloaderAsync(packageIdentity, sourceCacheContext, NullLogger.Instance, CancellationToken.None))
            //            {
            //                await PackageExtractor.InstallFromSourceAsync(packageIdentity, packageDownloader, gpf.PathResolver, packageExtractionContext, CancellationToken.None);
            //            }
        }
    }
}

