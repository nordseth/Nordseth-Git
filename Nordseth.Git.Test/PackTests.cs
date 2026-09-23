namespace Nordseth.Git.Test;

[TestClass]
public class PackTests
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
    public void Pack_ReadPackEntryHeader_MatchesVerifyPack()
    {
        var s = _scenario;
        var packReader = new PackReader(s.Fake.GitDir);

        foreach (var (pack, obj) in s.AllPacked)
        {
            var entry = packReader.ReadPackEntryHeader(pack, (int)obj.Offset);

            Assert.AreEqual(pack, entry.Pack, obj.Id);
            Assert.AreEqual(obj.Offset, entry.Offset, obj.Id);
            Assert.AreEqual(obj.Size, entry.Size, obj.Id);

            if (obj.Depth == 0)
            {
                Assert.AreEqual(obj.Type, entry.Type.ToObjectType().ToString(), obj.Id);
            }
            else if (pack == s.PackA)
            {
                Assert.AreEqual(PackObjectType.OBJ_OFS_DELTA, entry.Type, obj.Id);
                Assert.IsNotNull(entry.RefOffset, obj.Id);
                var baseObject = s.VerifyA.Single(o => o.Id == obj.BaseId);
                Assert.AreEqual(baseObject.Offset, entry.Offset - entry.RefOffset.Value, obj.Id);
            }
            else
            {
                Assert.AreEqual(PackObjectType.OBJ_REF_DELTA, entry.Type, obj.Id);
                Assert.AreEqual(obj.BaseId, entry.RefObjectId!.ToHexString(), obj.Id);
            }
        }
    }

    [TestMethod]
    [DataRow("A")]
    [DataRow("B")]
    public void Pack_Read_Pack_Entry_With_Refs(string packName)
    {
        var s = _scenario;
        var verify = packName == "A" ? s.VerifyA : s.VerifyB;
        var deepest = verify.OrderByDescending(o => o.Depth).First();
        var objs = new ObjectReader(s.Fake.GitDir);
        var packReader = new PackReader(s.Fake.GitDir);

        var entries = packReader.ReadPackEntryHeaderWithRefs(deepest.Id, objs.FindPackObject)?.ToList();
        
        Assert.IsNotNull(entries);
        Assert.AreEqual(deepest.Depth + 1, entries.Count);
        Assert.IsTrue(entries.Take(entries.Count - 1).All(IsDelta));
        Assert.IsFalse(IsDelta(entries.Last()));
    }

    [TestMethod]
    [DataRow("commit")]
    [DataRow("blob")]
    [DataRow("tree")]
    public void Pack_Read_Packed_NonDelta(string type)
    {
        var s = _scenario;
        var (pack, obj) = s.AllPacked.First(p => p.obj.Type == type && p.obj.Depth == 0);
        var packReader = new PackReader(s.Fake.GitDir);

        var (entry, stream) = packReader.ReadPackEntry(pack, (int)obj.Offset);

        Assert.AreEqual(type, entry.Type.ToObjectType().ToString());
        Assert.AreEqual(obj.Id, FakeRepoGit.HashObject(type, stream.ReadAllBytes()));
    }

    private static bool IsDelta(PackEntry e) => e.Type == PackObjectType.OBJ_OFS_DELTA || e.Type == PackObjectType.OBJ_REF_DELTA;
}
