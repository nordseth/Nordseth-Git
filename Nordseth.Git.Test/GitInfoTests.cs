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
        string description = s.Open().DescribeCommit(s.Commits[i]);

        var expected = expectedPrefix.EndsWith("-") ? expectedPrefix + PackedScenario.Short(s.Commits[i]) : expectedPrefix;
        Assert.AreEqual(expected, description);
    }

    [TestMethod]
    public void GitInfo_ConfigReadFailure_ReportedInOriginUrl()
    {
        // own repo, since this test deletes the config
        using (var fake = new FakeRepo())
        {
            var c = fake.WriteChain(1);
            fake.WriteRef("refs/heads/main", c[0]);
            var repo = fake.Open();
            File.Delete(Path.Combine(fake.GitDir, "config"));

            var info = repo.GetGitInfo();

            StringAssert.Contains(info.OriginUrl ?? string.Empty, "failed to read origin url");
            Assert.AreEqual(FakeRepo.Short(c[0]), info.CommitDescription);
        }
    }

    // Guard: passes today only because ParseSignature (A4) keeps the UTC wall clock in When.DateTime.
    // Once A4 is fixed, CommitDate must be formatted from When.UtcDateTime.
    [TestMethod]
    [DataRow("+0100")]
    [DataRow("-0530")]
    public void GitInfo_CommitDate_IsUtc(string timezone)
    {
        using (var fake = new FakeRepo())
        {
            var commit = fake.WriteCommit(fake.WriteTree(), null, author: $"A U Thor <author@example.com> 1700000000 {timezone}");
            fake.WriteRef("refs/heads/main", commit);

            var info = fake.Open().GetGitInfo();

            Assert.AreEqual("2023-11-14 22:13:20Z", info.CommitDate);
        }
    }
}
