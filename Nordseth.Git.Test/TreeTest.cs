using System.Text;

namespace Nordseth.Git.Test;

[TestClass]
public class TreeTest
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
    public void Tree_Read()
    {
        var s = _scenario;
        var (pack, obj) = s.AllPacked.First(p => p.obj.Type == "tree" && p.obj.Depth == 0);
        var packReader = new PackReader(s.Fake.GitDir);

        var (entry, stream) = packReader.ReadPackEntry(pack, (int)obj.Offset);
        Assert.AreEqual(PackObjectType.OBJ_TREE, entry.Type);

        List<Tree> tree;
        using (stream)
        {
            tree = new ObjectParser().ReadTree(stream).ToList();
        }

        var expected = s.TreeEntries[obj.Id].Select(e => $"{e.mode} {e.name} {e.hash}").ToList();
        Assert.AreSequenceEqual(expected, tree.Select(t => t.ToString()).ToList());
    }

    [TestMethod]
    public void Tree_Read_Subtree_Mode()
    {
        var s = _scenario;
        var tree = s.Open().GetTree(s.Trees[1]);
        Assert.IsNotNull(tree);
        var sub = tree.Single(t => t.Name == "sub");
        Assert.AreEqual("40000", sub.Mode);
        Assert.AreEqual(s.SubTree, sub.Ref);
    }
}
