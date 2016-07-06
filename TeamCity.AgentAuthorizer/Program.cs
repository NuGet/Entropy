using System.Threading.Tasks;

namespace NuGet.TeamCity.AgentAuthorizer
{
    public class Program
    {
        public static int Main(string[] args)
        {
            return MainAsync(args).Result;
        }

        public static async Task<int> MainAsync(string[] args)
        {
            var application = new Application();
            var result = await application.RunAsync(args);
            return result == ApplicationResult.Success ? 0 : 1;
        }
    }
}
