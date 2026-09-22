using System.Text;

namespace Nordseth.Git.Test;

[TestClass]
public class DeltaTests
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
    [DataRow("A", DisplayName = "ofs delta")]
    [DataRow("B", DisplayName = "ref delta")]
    public void Delta_Read_Object(string packName)
    {
        var s = _scenario;
        var verify = packName == "A" ? s.VerifyA : s.VerifyB;
        var delta = verify.First(o => o.Type == "blob" && o.Depth > 0);
        var objs = new ObjectReader(s.Fake.GitDir);

        var (type, stream) = objs.GetObject(delta.Id);
        Assert.IsNotNull(stream);

        using (var reader = new StreamReader(stream))
        {
            Assert.AreEqual(ObjectType.blob, type);
            Assert.AreEqual(s.BlobTexts[delta.Id], reader.ReadToEnd());
        }
    }
}
