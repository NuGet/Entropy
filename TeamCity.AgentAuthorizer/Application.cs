using System;
using System.Linq;
using System.Threading.Tasks;

namespace NuGet.TeamCity.AgentAuthorizer
{
    public class Application
    {
        public async Task<ApplicationResult> RunAsync(string[] args)
        {
            Options options;
            if (!TryParseOptions(args, out options))
            {
                return ApplicationResult.InvalidArguments;
            }

            var runTask = RunAsync(options);
            var timeoutTask = Task.Delay(options.Timeout);
            if (await Task.WhenAny(runTask, timeoutTask) != runTask)
            {
                Console.WriteLine($"The application has taken more than {options.Timeout.TotalSeconds} seconds and has timed out.");
                return ApplicationResult.Timeout;
            }

            return await runTask;
        }

        private static async Task<ApplicationResult> RunAsync(Options options)
        {
            var client = new TeamCityClient(new Uri(options.Server));

            // Wait for the agent to appear in the list.
            Agent agent = null;
            while (agent == null)
            {
                Console.WriteLine("Waiting for the agent to appear in the agent list. Fetching the list of agents...");
                var agents = await client.GetAgentsAsync();
                agent = agents.FirstOrDefault(x => string.Equals(
                    x.Name,
                    options.AgentName,
                    StringComparison.OrdinalIgnoreCase));

                if (agent == null)
                {
                    await Task.Delay(TimeSpan.FromSeconds(1));
                }
            }

            // Authorize the agent.
            if (!agent.Authorized)
            {
                Console.WriteLine($"Authorizing agent '{agent.Name}'...");
                await client.SetAgentAuthorizationAsync(agent.Id, true);
            }
            else
            {
                Console.WriteLine($"The agent '{agent.Name}' is already authorized.");
            }

            // Wait for the agent to be connected.
            agent = null;
            while (agent == null)
            {
                Console.WriteLine("Waiting for the agent to be connected. Fetching the list of agents...");
                var agents = await client.GetAgentsAsync();
                agent = agents.FirstOrDefault(x => string.Equals(
                    x.Name,
                    options.AgentName,
                    StringComparison.OrdinalIgnoreCase));

                if (!agent.Connected)
                {
                    Console.WriteLine($"Agent '{agent.Name}' is not yet connected. Waiting...");
                    agent = null;
                }

                if (agent == null)
                {
                    await Task.Delay(TimeSpan.FromSeconds(1));
                }
            }

            // Add the agent to the agent pool.
            if (options.AgentPoolName != null)
            {
                var agentPools = await client.GetAgentPoolsAsync();
                var agentPool = agentPools.FirstOrDefault(x => string.Equals(
                    x.Name,
                    options.AgentPoolName,
                    StringComparison.OrdinalIgnoreCase));

                if (agentPool == null)
                {
                    Console.WriteLine($"No agent pool '{options.AgentPoolName}' was found.");
                    return ApplicationResult.NoMatchingAgentPool;
                }

                var agents = await client.GetAgentPoolAgentsAsync(agentPool.Id);
                if (!agents.Any(x => x.Id == agent.Id))
                {
                    Console.WriteLine($"Adding agent '{agent.Name}' to agent pool '{agentPool.Name}'...");
                    await client.AddAgentToAgentPoolAsync(agentPool.Id, agent.Id);
                }
                else
                {
                    Console.WriteLine($"The agent '{agent.Name}' is already in agent pool '{agentPool.Name}'.");
                }
            }

            return ApplicationResult.Success;
        }

        public static bool TryParseOptions(string[] args, out Options options)
        {
            options = null;

            if (args.Length < 3)
            {
                Console.WriteLine("At least three arguments are required.");
                Console.WriteLine();
                Console.WriteLine($"{AppDomain.CurrentDomain.FriendlyName} TC_SERVER TC_AGENT_NAME TIMEOUT_SEC [TC_AGENT_POOL]");
                Console.WriteLine();
                Console.WriteLine("  TC_SERVER       The base HTTP URL for the TeamCity server.");
                Console.WriteLine("  TC_AGENT_NAME   The name of the TeamCity agent to authorize.");
                Console.WriteLine("  TIMEOUT_SEC     The maximum number of seconds to take before failing.");
                Console.WriteLine("  TC_AGENT_POOL   (optional) The pool to move the agent to once authorized.");
                Console.WriteLine();
                Console.WriteLine("This application blocks until an agent appears in the provided TeamCity server.");
                Console.WriteLine("Once the agent appears, the agent with be authorized and the application will");
                Console.WriteLine("then block until the agent is updated (if necessary) and in the desired pool.");
                Console.WriteLine();
                Console.WriteLine("This application uses Windows Authentication to access the TeamCity server.");

                return false;
            }

            options = new Options();
            options.Server = args[0];
            options.AgentName = args[1];
            options.Timeout = TimeSpan.FromSeconds(int.Parse(args[2]));

            if (args.Length > 3)
            {
                options.AgentPoolName = args[3];
            }

            return true;
        }
    }
}
