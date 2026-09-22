namespace Nordseth.Git.Test;

[TestClass]
public class ObjectTests
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
    public void Objects_Read_UnpackedObject()
    {
        var head = _scenario.Commits[4];
        var objs = new ObjectReader(_scenario.Fake.GitDir);

        var (type, stream) = objs.GetUnpackedObject(head);

        Assert.AreEqual(ObjectType.commit, type);
        Assert.AreEqual(head, FakeRepoGit.HashObject("commit", stream.ReadAllBytes()));
    }

    [TestMethod]
    public void Objects_Read_UnpackedObject_NotLoose()
    {
        var objs = new ObjectReader(_scenario.Fake.GitDir);

        var (_, stream) = objs.GetUnpackedObject(_scenario.Commits[0]);

        Assert.IsNull(stream);
    }

    [TestMethod]
    public void Objects_AllObjects_RoundTrip()
    {
        var s = _scenario;
        var objs = new ObjectReader(s.Fake.GitDir);

        var expected = s.AllPacked.Select(p => (p.obj.Id, (string?)p.obj.Type))
            .Concat(s.LooseObjects.Select(id => (id, (string?)null)));

        var failures = new List<string>();
        foreach (var (id, expectedType) in expected)
        {
            var (type, stream) = objs.GetObject(id);
            if (stream == null)
            {
                failures.Add($"{id}: not found");
                continue;
            }

            var actual = FakeRepoGit.HashObject(type.ToString(), stream.ReadAllBytes());
            if (actual != id || (expectedType != null && expectedType != type.ToString()))
            {
                failures.Add($"{id}: read as {type} {actual}, expected {expectedType}");
            }
        }

        Assert.AreEqual(0, failures.Count, string.Join(Environment.NewLine, failures));
    }

    [TestMethod]
    public void PackIndex_Read_Indices()
    {
        var s = _scenario;
        var objs = new ObjectReader(s.Fake.GitDir);
        objs.LoadIndex();

        var indices = objs.PackIndex.OrderBy(i => i.Name == s.PackA ? 0 : 1).ToList();

        CollectionAssert.AreEqual(new[] { s.PackA, s.PackB }, indices.Select(i => i.Name).ToList());
        Assert.IsTrue(indices.All(i => i.Version == 2));
        Assert.AreEqual(s.VerifyA.Count, indices[0].Objects);
        Assert.AreEqual(s.VerifyB.Count, indices[1].Objects);
    }

    [TestMethod]
    public void PackIndex_Find_ObjectId()
    {
        var objs = new ObjectReader(_scenario.Fake.GitDir);

        foreach (var (pack, obj) in _scenario.AllPacked)
        {
            var (foundPack, offset) = objs.FindPackObject(obj.Id);

            Assert.AreEqual(pack, foundPack, obj.Id);
            Assert.AreEqual(obj.Offset, offset, obj.Id);
        }
    }

    [TestMethod]
    public void PackIndex_Find_ObjectId_Missing()
    {
        var objs = new ObjectReader(_scenario.Fake.GitDir);

        var (pack, offset) = objs.FindPackObject(new string('c', 40));

        Assert.IsNull(pack);
        Assert.AreEqual(-1, offset);
    }
}
