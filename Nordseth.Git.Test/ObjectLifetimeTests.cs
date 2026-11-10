namespace Nordseth.Git.Test;

[TestClass]
public class ObjectLifetimeTests
{
    private static readonly string MissingHash = new string('b', 40);

    [TestMethod]
    public void GetCommit_MissingObject_ReturnsNull()
    {
        using (var fake = new FakeRepo())
        {
            Assert.IsNull(fake.Open().GetCommit(MissingHash));
        }
    }

    [TestMethod]
    public void GetTree_MissingObject_ReturnsNull()
    {
        using (var fake = new FakeRepo())
        {
            Assert.IsNull(fake.Open().GetTree(MissingHash));
        }
    }

    [TestMethod]
    public void GetBlob_MissingObject_ReturnsNull()
    {
        using (var fake = new FakeRepo())
        {
            Assert.IsNull(fake.Open().GetBlob(MissingHash));
        }
    }

    [TestMethod]
    public void GetTag_MissingObject_ReturnsNull()
    {
        using (var fake = new FakeRepo())
        {
            Assert.IsNull(fake.Open().GetTag(MissingHash));
        }
    }

    [TestMethod]
    public void GetTree_ReleasesFileHandle()
    {
        using (var fake = new FakeRepo())
        {
            var tree = fake.WriteTree(("100644", "a.txt", fake.WriteBlob("a")));

            fake.Open().GetTree(tree);

            AssertNotLocked(fake.ObjectPath(tree));
        }
    }

    [TestMethod]
    public void GetCommit_ReleasesFileHandle()
    {
        using (var fake = new FakeRepo())
        {
            var commit = fake.WriteChain(1)[0];

            fake.Open().GetCommit(commit);

            AssertNotLocked(fake.ObjectPath(commit));
        }
    }

    [TestMethod]
    public void GetTag_ReleasesFileHandle()
    {
        using (var fake = new FakeRepo())
        {
            var tag = fake.WriteTag(fake.WriteChain(1)[0], "commit", "v1.0");

            fake.Open().GetTag(tag);

            AssertNotLocked(fake.ObjectPath(tag));
        }
    }

    private static void AssertNotLocked(string path)
    {
        try
        {
            using (new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.None))
            {
            }
        }
        catch (IOException ex)
        {
            Assert.Fail($"object file is still open: {ex.Message}");
        }
    }
}
