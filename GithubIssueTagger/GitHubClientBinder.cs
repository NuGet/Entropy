﻿using Octokit;
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

        public GitHubClientBinder(Option<string> patOption)
        {
            _patOption = patOption ?? throw new ArgumentNullException(nameof(patOption));
        }

        protected override GitHubClient GetBoundValue(BindingContext bindingContext)
        {
            var client = new GitHubClient(new ProductHeaderValue("nuget-github-issue-tagger"));

            string? pat = GetPat(bindingContext);
            if (pat is not null)
            {
                client.Credentials = new Credentials(pat);
            }
            else
            {
                Console.WriteLine("Warning: Unable to get github token. Making unauthenticated HTTP requests, which has lower request limits and cannot access private repos.");
            }

            return client;
        }

        private string? GetPat(BindingContext bindingContext)
        {
            string? pat = bindingContext.ParseResult.GetValueForOption(_patOption);
            if (!string.IsNullOrEmpty(pat))
            {
                return pat;
            }

            pat = Environment.GetEnvironmentVariable("GITHUB_TOKEN");
            if (!string.IsNullOrEmpty(pat))
            {
                return pat;
            }

            Dictionary<string, string>? credentials = GetGitCredentials(new Uri("https://github.com/NuGet/Home"));
            if (credentials?.TryGetValue("password", out pat) == true && !string.IsNullOrEmpty(pat))
            {
                return pat;
            }

            return null;
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
