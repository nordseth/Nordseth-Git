using System.Text;

namespace Nordseth.Git.Test;

[TestClass]
public class GitInfoTests
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
    public void GitInfo_Head()
    {
        var s = _scenario;
        var info = s.Open().GetGitInfo();

        Assert.AreEqual(s.Commits[4], info.CommitId);
        Assert.AreEqual("refs/heads/main", info.Branch);
        Assert.AreEqual("commit 5", info.CommitMessage);
        StringAssert.Contains(info.CommitAuthor, "<author@example.com>");
        Assert.AreEqual("2023-11-14 22:13:20Z", info.CommitDate);
        Assert.AreEqual(PackedScenario.OriginUrl, info.OriginUrl);
        Assert.AreEqual($"v1.0-2-{PackedScenario.Short(s.Commits[4])}", info.CommitDescription);
    }

    [TestMethod]
    [DataRow(4, "v1.0-2-")]
    [DataRow(3, "v1.0-1-")]
    [DataRow(2, "v1.0")]
    public void GitInfo_Describe_Commit(int i, string expectedPrefix)
    {
        var s = _scenario;
        string? description = s.Open().DescribeCommit(s.Commits[i]);

        var expected = expectedPrefix.EndsWith("-") ? expectedPrefix + PackedScenario.Short(s.Commits[i]) : expectedPrefix;
        Assert.AreEqual(expected, description);
    }
}
