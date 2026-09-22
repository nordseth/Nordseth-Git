namespace Nordseth.Git.Test;

[TestClass]
public class DescribeTests
{
    [TestMethod]
    public void Describe_PackedAnnotatedTag()
    {
        using (var fake = new FakeRepo())
        {
            var c = fake.WriteChain(3);
            var tag = fake.WriteTag(c[0], "commit", "v1.0");
            fake.WritePackedRefs($"{tag} refs/tags/v1.0", $"^{c[0]}");

            var description = fake.Open().DescribeCommit(c[2]);

            Assert.AreEqual($"v1.0-2-{FakeRepo.Short(c[2])}", description);
        }
    }

    [TestMethod]
    public void Describe_LooseAnnotatedTag_OnCommit()
    {
        using (var fake = new FakeRepo())
        {
            var c = fake.WriteChain(3);
            fake.WriteRef("refs/tags/v1.0", fake.WriteTag(c[2], "commit", "v1.0"));

            var description = fake.Open().DescribeCommit(c[2]);

            Assert.AreEqual("v1.0", description);
        }
    }

    [TestMethod]
    public void Describe_ShallowMissingParent_ReturnsShortHash()
    {
        using (var fake = new FakeRepo())
        {
            var tree = fake.WriteTree();
            var missingParent = new string('a', 40);
            var head = fake.WriteCommit(tree, new[] { missingParent });
            fake.WriteShallow(head);

            // an unrelated tag, so the history walk actually runs
            var unrelated = fake.WriteCommit(tree, null, message: "unrelated\n");
            fake.WriteRef("refs/tags/v0.1", fake.WriteTag(unrelated, "commit", "v0.1"));

            var description = fake.Open().DescribeCommit(head);

            Assert.AreEqual(FakeRepo.Short(head), description);
        }
    }

    [TestMethod]
    public void Describe_TagPointingToTree_IsIgnored()
    {
        using (var fake = new FakeRepo())
        {
            var c = fake.WriteChain(1);
            // lightweight tag pointing directly at a tree
            fake.WriteRef("refs/tags/treetag", fake.WriteTree());
            fake.WriteRef("refs/tags/v1.0", fake.WriteTag(c[0], "commit", "v1.0"));

            var description = fake.Open().DescribeCommit(c[0]);

            Assert.AreEqual("v1.0", description);
        }
    }

    [TestMethod]
    public void Describe_TagWithoutTagger()
    {
        using (var fake = new FakeRepo())
        {
            var c = fake.WriteChain(2);
            fake.WriteRef("refs/tags/v1.0", fake.WriteTag(c[1], "commit", "v1.0", tagger: null));
            // a second tag, otherwise the sort by tagger date never evaluates the key
            fake.WriteRef("refs/tags/v0.1", fake.WriteTag(c[0], "commit", "v0.1"));

            var description = fake.Open().DescribeCommit(c[1]);

            Assert.AreEqual("v1.0", description);
        }
    }

    [TestMethod]
    public void Describe_NestedTag_IsPeeled()
    {
        using (var fake = new FakeRepo())
        {
            var c = fake.WriteChain(1);
            var inner = fake.WriteTag(c[0], "commit", "v1");
            var outer = fake.WriteTag(inner, "tag", "v2");
            fake.WriteRef("refs/tags/v2", outer);

            var description = fake.Open().DescribeCommit(c[0]);

            Assert.AreEqual("v2", description);
        }
    }

    [TestMethod]
    public void Describe_NonCommit_ExceptionContainsHash()
    {
        using (var fake = new FakeRepo())
        {
            var tree = fake.WriteTree();
            var repo = fake.Open();

            var ex = Assert.Throws<InvalidOperationException>(() => repo.DescribeCommit(tree));

            StringAssert.Contains(ex.Message, tree);
        }
    }
}
