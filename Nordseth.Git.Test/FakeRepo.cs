using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;

namespace Nordseth.Git.Test;

/// <summary>
/// Builds a minimal synthetic git repo (loose objects + refs) in a temp folder.
/// </summary>
public sealed class FakeRepo : IDisposable
{
    public const string DefaultSignature = "A U Thor <author@example.com> 1700000000 +0100";

    public FakeRepo()
    {
        WorkDir = Path.Combine(Path.GetTempPath(), "nordseth-git-tests", Guid.NewGuid().ToString("N"));
        GitDir = Path.Combine(WorkDir, ".git");

        Directory.CreateDirectory(Path.Combine(GitDir, "objects", "pack"));
        Directory.CreateDirectory(Path.Combine(GitDir, "refs", "heads"));
        Directory.CreateDirectory(Path.Combine(GitDir, "refs", "tags"));
        File.WriteAllText(Path.Combine(GitDir, "HEAD"), "ref: refs/heads/main\n");
        File.WriteAllText(Path.Combine(GitDir, "config"),
            "[core]\n\trepositoryformatversion = 0\n\tbare = false\n" +
            "[remote \"origin\"]\n\turl = https://example.com/repo.git\n\tfetch = +refs/heads/*:refs/remotes/origin/*\n");
    }

    public string WorkDir { get; }
    public string GitDir { get; }

    public static string Short(string hash) => hash.Substring(0, 7);

    public Repo Open() => new Repo(WorkDir);

    public string ObjectPath(string hash) => Path.Combine(GitDir, "objects", hash.Substring(0, 2), hash.Substring(2));

    public string WriteObject(string type, byte[] content)
    {
        var header = Encoding.ASCII.GetBytes($"{type} {content.Length}\0");
        var raw = header.Concat(content).ToArray();
        var hash = Convert.ToHexStringLower(SHA1.HashData(raw));

        var path = ObjectPath(hash);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        using (var file = File.Create(path))
        using (var zlib = new ZLibStream(file, CompressionLevel.Optimal))
        {
            zlib.Write(raw, 0, raw.Length);
        }

        return hash;
    }

    public string WriteBlob(string text) => WriteObject("blob", Encoding.UTF8.GetBytes(text));

    public string WriteTree(params (string mode, string name, string hash)[] entries)
    {
        var content = new MemoryStream();
        foreach (var (mode, name, hash) in entries)
        {
            var prefix = Encoding.UTF8.GetBytes($"{mode} {name}\0");
            content.Write(prefix, 0, prefix.Length);
            var id = Convert.FromHexString(hash);
            content.Write(id, 0, id.Length);
        }

        return WriteObject("tree", content.ToArray());
    }

    public string WriteCommit(
        string tree,
        string[]? parents,
        string? extraHeaders = null,
        string author = DefaultSignature,
        string message = "subject\n\nbody\n")
    {
        var builder = new StringBuilder();
        builder.Append($"tree {tree}\n");
        foreach (var parent in parents ?? Array.Empty<string>())
        {
            builder.Append($"parent {parent}\n");
        }

        builder.Append($"author {author}\n");
        builder.Append($"committer {author}\n");
        builder.Append(extraHeaders ?? string.Empty);
        builder.Append('\n');
        builder.Append(message);

        return WriteObject("commit", Encoding.UTF8.GetBytes(builder.ToString()));
    }

    public string WriteTag(string obj, string type, string name, string? tagger = DefaultSignature, string message = "tag message\n")
    {
        var builder = new StringBuilder();
        builder.Append($"object {obj}\n");
        builder.Append($"type {type}\n");
        builder.Append($"tag {name}\n");
        if (tagger != null)
        {
            builder.Append($"tagger {tagger}\n");
        }

        builder.Append('\n');
        builder.Append(message);

        return WriteObject("tag", Encoding.UTF8.GetBytes(builder.ToString()));
    }

    /// <summary>Creates a linear history C1 &lt;- C2 &lt;- ... and returns the commit ids oldest first.</summary>
    public string[] WriteChain(int count)
    {
        var tree = WriteTree();
        var commits = new List<string>();
        for (int i = 0; i < count; i++)
        {
            var parents = commits.Count == 0 ? Array.Empty<string>() : new[] { commits[commits.Count - 1] };
            commits.Add(WriteCommit(tree, parents, message: $"commit {i + 1}\n"));
        }

        return commits.ToArray();
    }

    public void WriteRef(string name, string content)
    {
        var path = Path.Combine(GitDir, name);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, content + "\n");
    }

    public void WritePackedRefs(params string[] lines)
    {
        File.WriteAllText(
            Path.Combine(GitDir, "packed-refs"),
            "# pack-refs with: peeled fully-peeled sorted \n" + string.Join("\n", lines) + "\n");
    }

    public void WriteShallow(params string[] hashes)
    {
        File.WriteAllText(Path.Combine(GitDir, "shallow"), string.Join("\n", hashes) + "\n");
    }

    public void Dispose()
    {
        try
        {
            Directory.Delete(WorkDir, true);
        }
        catch (IOException)
        {
            // leaked file handles (see ObjectLifetimeTests) can block cleanup, ignore
        }
        catch (UnauthorizedAccessException)
        {
        }
    }
}
