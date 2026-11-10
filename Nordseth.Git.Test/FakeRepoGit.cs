using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;

namespace Nordseth.Git.Test;

/// <summary>One line of <c>git verify-pack -v</c>.</summary>
public record PackedObject(string Id, string Type, long Size, long Offset, int Depth, string? BaseId);

/// <summary>
/// Uses the git CLI on a <see cref="FakeRepo"/>, to create packs and as a reference for expected values.
/// </summary>
public static class FakeRepoGit
{
    public static string Git(this FakeRepo repo, string? stdin, params string[] args)
    {
        var emptyConfig = Path.Combine(repo.WorkDir, "empty.gitconfig");
        if (!File.Exists(emptyConfig))
        {
            File.WriteAllText(emptyConfig, string.Empty);
        }

        var startInfo = new ProcessStartInfo("git")
        {
            RedirectStandardInput = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            WorkingDirectory = repo.WorkDir,
        };
        startInfo.ArgumentList.Add($"--git-dir={repo.GitDir}");
        foreach (var arg in args)
        {
            startInfo.ArgumentList.Add(arg);
        }

        // keep the user's own git config out of the tests
        startInfo.Environment["GIT_CONFIG_NOSYSTEM"] = "1";
        startInfo.Environment["GIT_CONFIG_GLOBAL"] = emptyConfig;

        using (var process = Process.Start(startInfo)!)
        {
            var stdout = process.StandardOutput.ReadToEndAsync();
            var stderr = process.StandardError.ReadToEndAsync();
            if (stdin != null)
            {
                process.StandardInput.Write(stdin);
            }

            process.StandardInput.Close();
            process.WaitForExit();

            if (process.ExitCode != 0)
            {
                throw new InvalidOperationException($"git {string.Join(" ", args)} failed ({process.ExitCode}): {stderr.Result}");
            }

            return stdout.Result;
        }
    }

    /// <summary>
    /// Packs the given objects into a new pack and returns its name (pack-&lt;sha&gt;).
    /// Deltas use OBJ_OFS_DELTA when <paramref name="ofsDelta"/> is set, otherwise OBJ_REF_DELTA.
    /// <paramref name="indexVersion"/> is passed to --index-version, e.g. "1", or "2,0" to put every offset in the 64-bit table.
    /// </summary>
    public static string Pack(this FakeRepo repo, IEnumerable<string> hashes, bool ofsDelta, string? indexVersion = null)
    {
        var args = new List<string> { "pack-objects" };
        if (ofsDelta)
        {
            args.Add("--delta-base-offset");
        }

        if (indexVersion != null)
        {
            args.Add($"--index-version={indexVersion}");
        }

        args.Add(Path.Combine(repo.GitDir, "objects", "pack", "pack"));

        var packHash = repo.Git(string.Join("\n", hashes) + "\n", args.ToArray()).Trim();
        return $"pack-{packHash}";
    }

    /// <summary>Removes loose objects that are also in a pack.</summary>
    public static void PrunePacked(this FakeRepo repo) => repo.Git(null, "prune-packed");

    public static IReadOnlyList<PackedObject> VerifyPack(this FakeRepo repo, string pack)
    {
        var output = repo.Git(null, "verify-pack", "-v", Path.Combine(repo.GitDir, "objects", "pack", $"{pack}.idx"));

        var result = new List<PackedObject>();
        foreach (var line in output.Split('\n'))
        {
            // sha type size size-in-pack offset [depth base-sha]
            var parts = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length < 5 || parts[0].Length != 40)
            {
                continue;
            }

            result.Add(new PackedObject(
                parts[0],
                parts[1],
                long.Parse(parts[2]),
                long.Parse(parts[4]),
                parts.Length >= 7 ? int.Parse(parts[5]) : 0,
                parts.Length >= 7 ? parts[6] : null));
        }

        return result;
    }

    public static string HashObject(string type, byte[] content)
    {
        var header = Encoding.ASCII.GetBytes($"{type} {content.Length}\0");
        return Convert.ToHexStringLower(SHA1.HashData(header.Concat(content).ToArray()));
    }

    public static byte[] ReadAllBytes(this Stream stream)
    {
        using (stream)
        {
            var memoryStream = new MemoryStream();
            stream.CopyTo(memoryStream);
            return memoryStream.ToArray();
        }
    }
}
