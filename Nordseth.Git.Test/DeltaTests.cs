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

    [TestMethod]
    public void DeltaStream_CopyAndInsert_ProducesTarget()
    {
        // src 11, target 11, copy(offset 0, size 5), insert 6 " there"
        var delta = new List<byte> { 11, 11, 0x90, 5, 6 };
        delta.AddRange(Encoding.ASCII.GetBytes(" there"));

        var result = ReadDelta("hello world", delta.ToArray());

        Assert.AreEqual("hello there", result);
    }

    [TestMethod]
    public void DeltaStream_TruncatedInsert_Throws()
    {
        // insert 5 bytes, but only 2 are present
        var delta = new byte[] { 5, 5, 5, (byte)'a', (byte)'b' };

        var task = Task.Run(() => ReadDelta("hello", delta));
        bool completed = ((IAsyncResult)task).AsyncWaitHandle.WaitOne(TimeSpan.FromSeconds(2));

        Assert.IsTrue(completed, "DeltaStream.Read hangs on truncated delta");
        Assert.IsTrue(task.IsFaulted, "expected an exception for truncated delta");
    }

    [TestMethod]
    public void DeltaStream_ReservedOpcodeZero_Throws()
    {
        var delta = new byte[] { 5, 5, 0 };

        Assert.Throws<Exception>(() => ReadDelta("hello", delta));
    }

    private static string ReadDelta(string baseObject, byte[] delta)
    {
        var baseStream = new MemoryStream(Encoding.ASCII.GetBytes(baseObject));
        using (var reader = new StreamReader(new DeltaStream(new MemoryStream(delta), baseStream)))
        {
            return reader.ReadToEnd();
        }
    }
}
