using System.Text.RegularExpressions;

namespace Nordseth.Git.Test;

[TestClass]
public class RefTests
{
    [TestMethod]
    public void EnumerateRefs_Tags_IncludesPackedTags()
    {
        using (var fake = new FakeRepo())
        {
            var c = fake.WriteChain(1);
            fake.WritePackedRefs($"{c[0]} refs/tags/v1.0");

            var refs = fake.Open().EnumerateRefs("refs/tags").ToList();

            CollectionAssert.Contains(refs, ("refs/tags/v1.0", c[0]));
        }
    }

    [TestMethod]
    public void EnumerateRefs_Tags_ExcludesPackedNonTags()
    {
        using (var fake = new FakeRepo())
        {
            var c = fake.WriteChain(1);
            fake.WritePackedRefs($"{c[0]} refs/heads/other", $"{c[0]} refs/tags/v1.0");

            var refs = fake.Open().EnumerateRefs("refs/tags").ToList();

            Assert.IsFalse(refs.Any(r => r.name == "refs/heads/other"));
        }
    }

    [TestMethod]
    public void EnumerateRefs_LooseOverridesPacked()
    {
        using (var fake = new FakeRepo())
        {
            var c = fake.WriteChain(2);
            fake.WritePackedRefs($"{c[0]} refs/heads/main");
            fake.WriteRef("refs/heads/main", c[1]);

            var mains = fake.Open().EnumerateRefs().Where(r => r.name == "refs/heads/main").ToList();

            Assert.AreEqual(1, mains.Count, string.Join(", ", mains));
            Assert.AreEqual(c[1], mains[0].hash);
        }
    }

    [TestMethod]
    public void FindRef_SymbolicRef_IsResolved()
    {
        using (var fake = new FakeRepo())
        {
            var c = fake.WriteChain(1);
            fake.WriteRef("refs/remotes/origin/main", c[0]);
            fake.WriteRef("refs/remotes/origin/HEAD", "ref: refs/remotes/origin/main");

            var hash = fake.Open().FindRef("refs/remotes/origin/HEAD");

            Assert.AreEqual(c[0], hash);
        }
    }

    [TestMethod]
    public void EnumerateRefs_AllHashesAreObjectIds()
    {
        using (var fake = new FakeRepo())
        {
            var c = fake.WriteChain(1);
            fake.WriteRef("refs/heads/main", c[0]);
            fake.WriteRef("refs/remotes/origin/main", c[0]);
            fake.WriteRef("refs/remotes/origin/HEAD", "ref: refs/remotes/origin/main");

            var refs = fake.Open().EnumerateRefs().ToList();

            foreach (var r in refs)
            {
                Assert.IsTrue(Regex.IsMatch(r.hash, "^[0-9a-f]{40}$"), $"{r.name} = {r.hash}");
            }
        }
    }

    [TestMethod]
    public void GetGitInfo_UnbornBranch_ThrowsInvalidOperation()
    {
        using (var fake = new FakeRepo())
        {
            // HEAD points to refs/heads/main, which does not exist
            var repo = fake.Open();

            Assert.Throws<InvalidOperationException>(() => repo.GetGitInfo());
        }
    }
}
