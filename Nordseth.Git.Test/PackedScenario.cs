using System.Text;

namespace Nordseth.Git.Test;

/// <summary>
/// A repo with history spread over two packs and loose objects:
/// c1, c2 in pack A (OFS deltas), c3, c4 + tag v1.0 in pack B (REF deltas), c5 loose.
/// HEAD -> refs/heads/main -> c5 -> c4 -> c3 -> c2 -> c1.
/// </summary>
public sealed class PackedScenario : IDisposable
{
    public const string Signature = "A U Thor <author@example.com> 1700000000 +0000";
    public const string OriginUrl = "https://example.com/repo.git";

    private PackedScenario()
    {
    }

    public FakeRepo Fake { get; } = new();

    public string Readme { get; private set; } = "";
    public string Nested { get; private set; } = "";
    public string SubTree { get; private set; } = "";

    /// <summary>data.txt blob for each commit, index 0 = c1.</summary>
    public string[] Blobs { get; } = new string[5];
    public string[] Trees { get; } = new string[5];
    public string[] Commits { get; } = new string[5];

    public string Tag { get; private set; } = "";
    public string OldTag { get; private set; } = "";

    public string PackA { get; private set; } = "";
    public string PackB { get; private set; } = "";
    public IReadOnlyList<PackedObject> VerifyA { get; private set; } = [];
    public IReadOnlyList<PackedObject> VerifyB { get; private set; } = [];

    /// <summary>Objects that are only stored loose.</summary>
    public IReadOnlyList<string> LooseObjects { get; private set; } = [];

    public IDictionary<string, string> BlobTexts { get; } = new Dictionary<string, string>();
    public IDictionary<string, (string mode, string name, string hash)[]> TreeEntries { get; } = new Dictionary<string, (string, string, string)[]>();

    public IEnumerable<(string pack, PackedObject obj)> AllPacked =>
        VerifyA.Select(o => (PackA, o)).Concat(VerifyB.Select(o => (PackB, o)));

    public Repo Open() => Fake.Open();

    public static PackedScenario Create()
    {
        var s = new PackedScenario();
        var fake = s.Fake;

        s.Readme = s.WriteBlob("readme\n");
        s.Nested = s.WriteBlob("nested file\n");
        s.SubTree = s.WriteTree(("100644", "nested.txt", s.Nested));

        // similar ~4 KB texts, so git will store them as deltas
        var baseLines = Enumerable.Range(1, 200).Select(i => $"line {i}: the quick brown fox jumps over the lazy dog").ToList();
        var texts = new List<string>();
        var lines = baseLines.ToList();
        texts.Add(Join(lines));
        lines[99] = "line 100: changed in version 2";
        texts.Add(Join(lines));
        lines.AddRange(Enumerable.Range(201, 20).Select(i => $"line {i}: appended in version 3"));
        texts.Add(Join(lines));
        lines[49] = "line 50: changed in version 4";
        texts.Add(Join(lines));
        lines[9] = "line 10: changed in version 5";
        texts.Add(Join(lines));

        // c1..c4
        for (int i = 0; i < 4; i++)
        {
            s.WriteCommitAt(i, texts[i]);
        }

        s.Tag = fake.WriteTag(s.Commits[2], "commit", "v1.0", Signature, "release 1.0\n");

        s.PackA = fake.Pack(new[] { s.Readme, s.Nested, s.SubTree, s.Blobs[0], s.Blobs[1], s.Trees[0], s.Trees[1], s.Commits[0], s.Commits[1] }, ofsDelta: true);
        s.PackB = fake.Pack(new[] { s.Blobs[2], s.Blobs[3], s.Trees[2], s.Trees[3], s.Commits[2], s.Commits[3], s.Tag }, ofsDelta: false);
        fake.PrunePacked();

        // c5 and an older annotated tag stay loose
        s.WriteCommitAt(4, texts[4]);
        s.OldTag = fake.WriteTag(s.Commits[0], "commit", "v0.9", Signature, "release 0.9\n");
        s.LooseObjects = new[] { s.Blobs[4], s.Trees[4], s.Commits[4], s.OldTag };

        fake.WriteRef("refs/heads/main", s.Commits[4]);
        fake.WriteRef("refs/tags/v1.0", s.Tag);
        fake.WritePackedRefs(
            $"{s.Commits[0]} refs/heads/old",
            $"{s.Commits[1]} refs/tags/lw",
            $"{s.OldTag} refs/tags/v0.9",
            $"^{s.Commits[0]}");

        s.VerifyA = fake.VerifyPack(s.PackA);
        s.VerifyB = fake.VerifyPack(s.PackB);

        // make sure the scenario actually covers deltas
        if (!s.VerifyA.Any(o => o.Depth > 0 && o.Type == "blob") || !s.VerifyB.Any(o => o.Depth > 0 && o.Type == "blob"))
        {
            s.Dispose();
            throw new InvalidOperationException("git did not create blob deltas in both packs");
        }

        return s;
    }

    public static string Short(string hash) => FakeRepo.Short(hash);

    public void Dispose() => Fake.Dispose();

    private void WriteCommitAt(int i, string text)
    {
        Blobs[i] = WriteBlob(text);

        var entries = new List<(string, string, string)>
        {
            ("100644", "README", Readme),
            ("100644", "data.txt", Blobs[i]),
        };
        if (i == 1)
        {
            entries.Add(("40000", "sub", SubTree));
        }

        Trees[i] = WriteTree(entries.ToArray());

        var parents = i == 0 ? Array.Empty<string>() : new[] { Commits[i - 1] };
        Commits[i] = Fake.WriteCommit(Trees[i], parents, author: Signature, message: $"commit {i + 1}\n\nbody {i + 1}\n");
    }

    private string WriteBlob(string text)
    {
        var hash = Fake.WriteBlob(text);
        BlobTexts[hash] = text;
        return hash;
    }

    private string WriteTree(params (string mode, string name, string hash)[] entries)
    {
        var hash = Fake.WriteTree(entries);
        TreeEntries[hash] = entries;
        return hash;
    }

    private static string Join(IEnumerable<string> lines) => string.Join("\n", lines) + "\n";
}
