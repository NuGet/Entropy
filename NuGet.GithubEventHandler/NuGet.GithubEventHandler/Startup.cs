using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;

[assembly: FunctionsStartup(typeof(NuGet.GithubEventHandler.Startup))]

namespace NuGet.GithubEventHandler
{

    public class Startup : FunctionsStartup
    {
        public override void Configure(IFunctionsHostBuilder builder)
        {
            builder.Services.AddSingleton<IEnvironment, Environment>();
            builder.Services.AddSingleton<IAzDOClient, AzDOClient>();
        }
    }
}
