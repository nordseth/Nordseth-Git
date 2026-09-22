using System.Text;

namespace Nordseth.Git.Test;

[TestClass]
public class RepoTests
{
    private static PackedScenario _scenario = null!;

    [ClassInitialize]
    public static void Init(TestContext context)
    {
        _scenario = PackedScenario.Create();
    }

    [ClassCleanup]
    public static void Cleanup()
    {
        _scenario.Dispose();
    }

    [TestMethod]
    public void Repo_Open_WorkDir()
    {
        var repo = new Repo(_scenario.Fake.WorkDir);

        Assert.AreEqual(_scenario.Fake.GitDir, repo.RepoPath);
    }

    [TestMethod]
    public void Repo_Open_GitDir()
    {
        var repo = new Repo(_scenario.Fake.GitDir);

        Assert.AreEqual(_scenario.Fake.GitDir, repo.RepoPath);
    }

    [TestMethod]
    public void Repo_Open_Invalid()
    {
        var emptyDir = Directory.CreateTempSubdirectory("nordseth-git-tests-");
        try
        {
            Assert.Throws<InvalidOperationException>(() => new Repo(emptyDir.FullName));
        }
        finally
        {
            emptyDir.Delete(true);
        }
    }

    [TestMethod]
    public void Repo_Read_Config()
    {
        var config = _scenario.Open().LoadConfig();

        CollectionAssert.AreEqual(new[] { PackedScenario.OriginUrl }, config["remote", "origin", "url"].ToList());
    }

    [TestMethod]
    public void Repo_Enumerate_Refs()
    {
        var s = _scenario;
        var refs = s.Open().EnumerateRefs().OrderBy(r => r.name, StringComparer.Ordinal).ToList();

        var expected = new List<(string, string)>
        {
            ("refs/heads/main", s.Commits[4]),
            ("refs/heads/old", s.Commits[0]),
            ("refs/tags/lw", s.Commits[1]),
            ("refs/tags/v0.9", s.OldTag),
            ("refs/tags/v1.0", s.Tag),
        };
        CollectionAssert.AreEqual(expected, refs);
    }

    [TestMethod]
    public void Repo_Enumerate_Refs_MatchesGit()
    {
        var refs = _scenario.Open().EnumerateRefs()
            .Select(r => $"{r.hash} {r.name}")
            .OrderBy(r => r, StringComparer.Ordinal)
            .ToList();

        var expected = _scenario.Fake.Git(null, "for-each-ref", "--format=%(objectname) %(refname)")
            .Split('\n', StringSplitOptions.RemoveEmptyEntries)
            .OrderBy(r => r, StringComparer.Ordinal)
            .ToList();
        CollectionAssert.AreEqual(expected, refs);
    }

    [TestMethod]
    public void Repo_Enumerate_PackedRefs()
    {
        var s = _scenario;
        var refs = s.Open().EnumeratePackedRefs().ToList();

        var expected = new List<(string, string)>
        {
            ("refs/heads/old", s.Commits[0]),
            ("refs/tags/lw", s.Commits[1]),
            ("refs/tags/v0.9", s.OldTag),
        };
        CollectionAssert.AreEqual(expected, refs);
    }

    [TestMethod]
    public void Repo_Get_Head()
    {
        var (refName, hash) = _scenario.Open().GetHead();

        Assert.AreEqual("refs/heads/main", refName);
        Assert.AreEqual(_scenario.Commits[4], hash);
    }

    [TestMethod]
    public void Repo_Get_Head_Detached()
    {
        using (var fake = new FakeRepo())
        {
            var commit = fake.WriteChain(1)[0];
            File.WriteAllText(Path.Combine(fake.GitDir, "HEAD"), commit + "\n");

            var (refName, hash) = fake.Open().GetHead();

            Assert.IsNull(refName);
            Assert.AreEqual(commit, hash);
        }
    }

    [TestMethod]
    [DataRow("refs/heads/main", 4)]
    [DataRow("refs/heads/old", 0)]
    [DataRow("refs/tags/lw", 1)]
    public void Repo_Get_Ref(string refName, int commitIndex)
    {
        var hash = _scenario.Open().FindRef(refName);

        Assert.AreEqual(_scenario.Commits[commitIndex], hash);
    }

    [TestMethod]
    public void Repo_Get_Ref_Missing()
    {
        Assert.IsNull(_scenario.Open().FindRef("refs/heads/missing"));
    }

    [TestMethod]
    [DataRow(4, DisplayName = "Commit loose")]
    [DataRow(2, DisplayName = "Commit pack B (ref delta)")]
    [DataRow(0, DisplayName = "Commit pack A (ofs delta)")]
    public void Repo_Read_Commit(int i)
    {
        var s = _scenario;
        var commit = s.Open().GetCommit(s.Commits[i]);

        Assert.IsNotNull(commit);
        Assert.AreEqual(s.Commits[i], commit.Id);
        Assert.AreEqual(s.Trees[i], commit.Tree);
        CollectionAssert.AreEqual(i == 0 ? new string[0] : new[] { s.Commits[i - 1] }, commit.Parents.ToList());
        Assert.AreEqual("A U Thor", commit.Author.Name?.Trim());
        Assert.AreEqual("author@example.com", commit.Author.Email);
        Assert.AreEqual(1700000000, commit.Author.When.ToUnixTimeSeconds());
        Assert.AreEqual("author@example.com", commit.Committer.Email);
        Assert.AreEqual($"commit {i + 1}", commit.MessageShort);
        Assert.AreEqual($"commit {i + 1}\n\nbody {i + 1}", commit.Message.Replace("\r\n", "\n"));
    }

    [TestMethod]
    public void Repo_Read_Commit_NotACommit()
    {
        Assert.IsNull(_scenario.Open().GetCommit(_scenario.Trees[0]));
    }

    [TestMethod]
    [DataRow(4, DisplayName = "Tree loose")]
    [DataRow(3, DisplayName = "Tree pack B")]
    [DataRow(1, DisplayName = "Tree pack A, with subtree")]
    public void Repo_Read_Tree(int i)
    {
        var s = _scenario;
        var tree = s.Open().GetTree(s.Trees[i]);

        var expected = s.TreeEntries[s.Trees[i]].Select(e => $"{e.mode} {e.name} {e.hash}").ToList();
        CollectionAssert.AreEqual(expected, tree.Select(t => t.ToString()).ToList());
    }

    [TestMethod]
    [DataRow(4, DisplayName = "Blob loose")]
    [DataRow(3, DisplayName = "Blob pack B")]
    [DataRow(0, DisplayName = "Blob pack A")]
    public void Repo_Read_Blob(int i)
    {
        var s = _scenario;
        var content = s.Open().GetBlob(s.Blobs[i]).ReadAllBytes();

        Assert.AreEqual(s.BlobTexts[s.Blobs[i]], Encoding.UTF8.GetString(content));
    }

    [TestMethod]
    public void Repo_Read_Tag_Annotated()
    {
        var s = _scenario;
        var tag = s.Open().GetTag(s.Tag);

        Assert.AreEqual(s.Tag, tag.Id);
        Assert.AreEqual("v1.0", tag.Name);
        Assert.AreEqual(s.Commits[2], tag.Commit);
        Assert.AreEqual("author@example.com", tag.Tagger.Email);
        Assert.AreEqual("release 1.0", tag.MessageShort);
    }

    [TestMethod]
    public void Repo_Read_Tag_Lightweight()
    {
        var s = _scenario;
        var tag = s.Open().GetTag(s.Commits[1]);

        Assert.AreEqual(s.Commits[1], tag.Commit);
        Assert.IsNull(tag.Name);
    }

    [TestMethod]
    public void Repo_Read_Commit_Log()
    {
        var repo = _scenario.Open();

        var log = new List<string>();
        var commitId = repo.GetHead().hash;
        while (commitId != null)
        {
            log.Add(commitId);
            commitId = repo.GetCommit(commitId).Parents.FirstOrDefault();
        }

        CollectionAssert.AreEqual(_scenario.Commits.Reverse().ToList(), log);

        var gitLog = _scenario.Fake.Git(null, "rev-list", "HEAD").Split('\n', StringSplitOptions.RemoveEmptyEntries);
        CollectionAssert.AreEqual(gitLog, log);
    }
}
