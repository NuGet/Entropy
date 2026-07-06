using System.Diagnostics;
using System.Text;

namespace NuGetDashboard;

// Implements https://git-scm.com/docs/git-credential#_typical_use_of_git_credential
internal static class GitCredentials
{
    /// <summary>
    /// Runs "git credential fill" for the given URI, allowing the credential manager
    /// to show its interactive UI (browser/dialog) if no valid credentials are cached.
    /// Returns null if git is unavailable.
    /// </summary>
    public static GitToken? Get(Uri uri)
    {
        var payload = RunCredentialCommand("fill", "url=" + uri.AbsoluteUri + "\n\n");
        if (payload is null) return null;

        string? password = null;
        foreach (var line in payload.Split('\n'))
        {
            var idx = line.IndexOf('=');
            if (idx == -1) continue;
            if (line.AsSpan(0, idx).Equals("password", StringComparison.Ordinal))
            {
                password = line[(idx + 1)..].Trim();
                break;
            }
        }

        return password is not null ? new GitToken(password, payload) : null;
    }

    /// <summary>
    /// Runs "git credential reject" to erase a stale/invalid token from the credential
    /// store, so the next call to <see cref="Get"/> will prompt for fresh credentials.
    /// </summary>
    public static void Reject(GitToken token) =>
        RunCredentialCommand("reject", token.Payload);

    private static string? RunCredentialCommand(string subcommand, string input)
    {
        var psi = new ProcessStartInfo
        {
            FileName = "git",
            Arguments = $"credential {subcommand}",
            CreateNoWindow = false,   // allow the credential manager UI to appear
            RedirectStandardInput = true,
            RedirectStandardOutput = subcommand == "fill",
        };

        var process = Process.Start(psi);
        if (process is null) return null;

        process.StandardInput.Write(input);
        process.StandardInput.Close();

        var output = subcommand == "fill" ? process.StandardOutput.ReadToEnd() : string.Empty;
        process.WaitForExit();

        return process.ExitCode == 0 ? output : null;
    }
}

internal record GitToken(string Password, string Payload);
