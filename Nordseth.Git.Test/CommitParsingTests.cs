namespace Nordseth.Git.Test;

[TestClass]
public class CommitParsingTests
{
    // same shape as commits signed by GitHub: the armor has a blank line, encoded as " "
    private const string GpgSigHeader =
        "gpgsig -----BEGIN PGP SIGNATURE-----\n" +
        " \n" +
        " wsFcBAABCAAQBQJqss7OCRC1aQ7uu5UhlAAAhKQQAKWlI6xsfys41jHWc7JFQ3eK\n" +
        " =vuqf\n" +
        " -----END PGP SIGNATURE-----\n" +
        " \n";

    private const string MergeTagHeader =
        "mergetag object 1111111111111111111111111111111111111111\n" +
        " type commit\n" +
        " tag v1.0\n" +
        " tagger A U Thor <author@example.com> 1700000000 +0100\n" +
        " \n" +
        " release v1.0\n";

    [TestMethod]
    public void Commit_WithGpgSig_MessageShortIsSubject()
    {
        using (var fake = new FakeRepo())
        {
            var hash = fake.WriteCommit(fake.WriteTree(), null, extraHeaders: GpgSigHeader);

            var commit = fake.Open().GetCommit(hash);

            Assert.AreEqual("subject", commit.MessageShort);
            Assert.IsFalse(commit.Message.Contains("PGP"), commit.Message);
            Assert.AreEqual("author@example.com", commit.Author?.Email);
            Assert.AreEqual("author@example.com", commit.Committer?.Email);
        }
    }

    [TestMethod]
    public void Commit_WithMergeTag_MessageShortIsSubject()
    {
        using (var fake = new FakeRepo())
        {
            var hash = fake.WriteCommit(fake.WriteTree(), null, extraHeaders: MergeTagHeader);

            var commit = fake.Open().GetCommit(hash);

            Assert.AreEqual("subject", commit.MessageShort);
            Assert.IsFalse(commit.Message.Contains("release v1.0"), commit.Message);
        }
    }

    [TestMethod]
    [DataRow("+0100", 60)]
    [DataRow("-0530", -330)]
    [DataRow("+0000", 0)]
    [DataRow("-0030", -30)]
    [DataRow("+1345", 825)]
    public void ParseSignature_Timezone(string tz, int expectedOffsetMinutes)
    {
        var signature = new ObjectParser().ParseSignature($"A U Thor <a@example.com> 1700000000 {tz}");

        Assert.AreEqual(expectedOffsetMinutes, signature.When.Offset.TotalMinutes);
        Assert.AreEqual(1700000000, signature.When.ToUnixTimeSeconds());
    }

    [TestMethod]
    public void ParseSignature_NameIsTrimmed()
    {
        var signature = new ObjectParser().ParseSignature("A U Thor <a@example.com> 1700000000 +0100");

        Assert.AreEqual("A U Thor", signature.Name);
        Assert.AreEqual("a@example.com", signature.Email);
    }

    [TestMethod]
    public void ParseSignature_WithoutTimestamp_DoesNotThrow()
    {
        var signature = new ObjectParser().ParseSignature("A <a@example.com>");

        Assert.AreEqual("a@example.com", signature.Email);
    }
}
