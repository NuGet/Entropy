using Octokit;
using System;
using System.Collections.Generic;
using System.CommandLine;
using System.CommandLine.Binding;
using System.Diagnostics;

namespace GithubIssueTagger
{
    internal class GitHubClientBinder : BinderBase<GitHubClient>
    {
        private readonly Option<string> _patOption;
        private readonly Option<string> _patEnvVarOption;

        public GitHubClientBinder(Option<string> patOption, Option<string> patEnvVarOption)
        {
            _patOption = patOption ?? throw new ArgumentNullException(nameof(patOption));
            _patEnvVarOption = patEnvVarOption ?? throw new ArgumentNullException(nameof(patEnvVarOption));
        }

        protected override GitHubClient GetBoundValue(BindingContext bindingContext)
        {
            var client = new GitHubClient(new ProductHeaderValue("nuget-github-issue-tagger"));

            string? pat = bindingContext.ParseResult.GetValueForOption(_patOption);
            string? patEnvVar = bindingContext.ParseResult.GetValueForOption(_patEnvVarOption);

            if (!string.IsNullOrEmpty(pat) && !string.IsNullOrEmpty(patEnvVar))
            {
                throw new Exception($"Only one of {_patOption.Name} or {_patEnvVarOption.Name} can be specified");
            }

            if (!string.IsNullOrEmpty(pat))
            {
                client.Credentials = new Credentials(pat);
            }
            else if (!string.IsNullOrEmpty(patEnvVar))
            {
                var value = Environment.GetEnvironmentVariable(patEnvVar);
                if (string.IsNullOrEmpty(value))
                {
                    Console.WriteLine("Warning: Environment variable {0} does not have a value. Making unauthenticated HTTP requests");
                }
                else
                {
                    client.Credentials = new Credentials(value);
                }
            }
            else
            {
                Dictionary<string, string>? credentials = GetGitCredentials(new Uri("https://github.com/NuGet/Home"));
                if (credentials?.TryGetValue("password", out string? password) == true)
                {
                    client.Credentials = new Credentials(password);
                }
                else
                {
                    Console.WriteLine("Warning: Unable to get github token. Making unauthenticated HTTP requests, which has lower request limits and cannot access private repos.");
                }
            }

            return client;
        }

        // Implement https://git-scm.com/docs/git-credential#_typical_use_of_git_credential
        private static Dictionary<string, string>? GetGitCredentials(Uri uri)
        {
            string description = "url=" + uri.AbsoluteUri + "\n\n";

            ProcessStartInfo processStartInfo = new()
            {
                FileName = "git",
                Arguments = "credential fill",
                CreateNoWindow = true,
                RedirectStandardInput = true,
                RedirectStandardOutput = true,
            };
            processStartInfo.Environment["GIT_TERMINAL_PROMPT"] = "0";

            Process? process = Process.Start(processStartInfo);
            if (process == null)
            {
                throw new Exception("Unable to start git process");
            }
            process.StandardInput.Write(description);

            process.WaitForExit();

            if (process.ExitCode != 0)
            {
                // unable to get credentials
                return null;
            }

            Dictionary<string, string> result = new();
            string? line;
            while ((line = process.StandardOutput.ReadLine()) != null)
            {
                int index = line.IndexOf('=');
                if (index == -1)
                {
                    continue;
                }

                string key = line.Substring(0, index);
                string value = line.Substring(index + 1);
                result[key] = value;
            }

            return result.Count > 0 ? result : null;
        }

    }
}
