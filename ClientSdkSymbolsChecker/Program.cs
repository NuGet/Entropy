using ClientSdkSymbolsChecker;
using Microsoft.SymbolStore;
using Microsoft.SymbolStore.SymbolStores;
using NuGet.Packaging.Core;
using NuGet.Protocol.Core.Types;
using NuGet.Repositories;
using NuGet.Versioning;
using System.Runtime.ExceptionServices;

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

try
{
    CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();
    Console.CancelKeyPress += (o, e) =>
    {
        cancellationTokenSource.Cancel();
        e.Cancel = true;
    };
    AppDomain.CurrentDomain.UnhandledException += (e1, e2) => Console.WriteLine("Unhandled exception " + e2.ExceptionObject);
    TaskScheduler.UnobservedTaskException += (e1, e2) => Console.WriteLine("Unhandled task exception " + e2.Exception);

    UserAgent.SetUserAgentString(new UserAgentStringBuilder("NuGetSdkSymbolChecker"));

    var globalContext = new GlobalContext();
    Dictionary<string, IReadOnlyList<NuGetVersion>> allPackages = await GetAllPackageVersionsAsync(packageIds, globalContext, cancellationTokenSource.Token);
    Console.WriteLine("Checking {0} versions from {1} packages", allPackages.Sum(p => p.Value.Count), allPackages.Count);

    var missingPackages = await GetMissingPackageVersionsAsync(allPackages, globalContext, cancellationTokenSource.Token);
    Console.WriteLine("Need to download {0} packages", missingPackages.Count);
    if (missingPackages.Count > 0)
    {
        int downloaded = 0;
        Action<PackageIdentity> callback = (package) =>
        {
            var completed = Interlocked.Increment(ref downloaded);
            Console.WriteLine("{0}/{1} Downloaded {2}", completed, missingPackages.Count, package);
        };
        await DownloadMissingPackagesAsync(missingPackages, globalContext, callback, cancellationTokenSource.Token);
    }

    var httpClient = new HttpClient();

    foreach (var package in allPackages)
    {
        foreach (var version in package.Value)
        {
            var dir = globalContext.GlobalPackagesFolder.PathResolver.GetInstallPath(package.Key, version);
            var dlls = Directory.EnumerateFiles(dir, "*.dll", SearchOption.AllDirectories);
            foreach (var dll in dlls)
            {
                using (var file = File.OpenRead(dll))
                {
                    var key = new SymbolStoreFile(file, dll);
                    var keyFileGenerator = new Microsoft.SymbolStore.KeyGenerators.FileKeyGenerator(null, key);
                    foreach (var argh in keyFileGenerator.GetKeys(Microsoft.SymbolStore.KeyGenerators.KeyTypeFlags.SymbolKey))
                    {
                        var request = new HttpRequestMessage(HttpMethod.Head, "http://msdl.microsoft.com/download/symbols/" + argh.Index);
                        var response = await httpClient.SendAsync(request, cancellationTokenSource.Token);
                        if (!response.IsSuccessStatusCode)
                        {
                            Console.WriteLine("PDB missing for {0} version {1}: {2}", package.Key, version, dll.Substring(dir.Length));
                        }
                    }
                }
            }
        }
    }
}
catch (Exception e)
{
    Console.WriteLine("Unhandled exception:");
    Console.WriteLine(e);
}

Console.WriteLine("Finished");

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

static async Task DownloadMissingPackagesAsync(IReadOnlyCollection<PackageIdentity> packages, GlobalContext context, Action<PackageIdentity> finishedCallback, CancellationToken cancellationToken)
{
    const int maxParallel = 4;
    var tasks = new List<Task>(maxParallel);
    // Used to cancel pending requests when a task fails.
    var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
    cts.Token.Register(() => Console.WriteLine("Download canceled"));
    NuGetDownloader downloader = await NuGetDownloader.CreateAsync(cancellationToken);

    using (var enumerator = packages.GetEnumerator())
    {
        // Ramp-up
        while (tasks.Count < maxParallel && enumerator.MoveNext())
        {
            var package = enumerator.Current;
            Task downloadPackageTask = DownloadPackageAsync(package, context.GlobalPackagesFolder, downloader, finishedCallback, cts.Token);
            tasks.Add(downloadPackageTask);
        }

        // Steady state
        while (enumerator.MoveNext())
        {
            var finishedTask = await Task.WhenAny(tasks);
            if (finishedTask.IsFaulted)
            {
                cts.Cancel();
                await Task.WhenAll(tasks);
                ExceptionDispatchInfo.Capture(finishedTask.Exception.InnerException).Throw();
            }

            int taskIndex;
            for (taskIndex = 0; taskIndex < tasks.Count; taskIndex++)
            {
                if (finishedTask == tasks[taskIndex])
                {
                    break;
                }
            }

            tasks[taskIndex] = DownloadPackageAsync(enumerator.Current, context.GlobalPackagesFolder, downloader, finishedCallback, cts.Token);
        }

        // Ramp-down
        while (tasks.Count > 0)
        {
            var finishedTask = await Task.WhenAny(tasks);
            if (finishedTask.IsFaulted)
            {
                cts.Cancel();
                await Task.WhenAll(tasks);
                ExceptionDispatchInfo.Capture(finishedTask.Exception.InnerException).Throw();
            }

            int taskIndex;
            for (taskIndex = 0; taskIndex < tasks.Count; taskIndex++)
            {
                if (finishedTask == tasks[taskIndex])
                {
                    break;
                }
            }

            tasks.RemoveAt(taskIndex);
        }
    }
}

static async Task DownloadPackageAsync(PackageIdentity package, NuGetv3LocalRepository globalPackagesFolder, NuGetDownloader downloader, Action<PackageIdentity> finishedCallback, CancellationToken cancellationToken)
{
    try
    {
        await downloader.DownloadPackageAsync(package, globalPackagesFolder, cancellationToken);
        finishedCallback?.Invoke(package);
    }
    catch (Exception e)
    {
        Console.WriteLine("Package " + package + " exception " + e.Message + " (" + e.GetType().Name + ")");
    }
}
