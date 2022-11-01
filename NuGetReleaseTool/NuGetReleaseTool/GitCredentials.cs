using System.Diagnostics;

namespace NuGetReleaseTool
{
    internal class GitCredentials
    {
        // Implement https://git-scm.com/docs/git-credential#_typical_use_of_git_credential
        public static Dictionary<string, string> Get(Uri uri)
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

            Process process = Process.Start(processStartInfo);
            process.StandardInput.Write(description);

            process.WaitForExit();

            if (process.ExitCode != 0)
            {
                // unable to get credentials
                return null;
            }

            Dictionary<string, string> result = new();
            string line;
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