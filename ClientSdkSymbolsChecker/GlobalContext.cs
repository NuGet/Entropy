using NuGet.Configuration;
using NuGet.Repositories;

namespace ClientSdkSymbolsChecker
{
    internal class GlobalContext
    {
        public ISettings Settings { get; }
        public NuGetv3LocalRepository GlobalPackagesFolder { get; }

        public GlobalContext()
        {
            var currentDirectory = Environment.CurrentDirectory;
            Settings = NuGet.Configuration.Settings.LoadDefaultSettings(currentDirectory);

            GlobalPackagesFolder = new NuGetv3LocalRepository(SettingsUtility.GetGlobalPackagesFolder(Settings));
        }
    }
}
