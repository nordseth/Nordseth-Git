namespace Nordseth.Git.Test;

[TestClass]
public class PackFormatTests
{
    [TestMethod]
    public void PackIndex_Version1_ThrowsNotImplemented()
    {
        using var fake = new FakeRepo();
        var commit = PackChain(fake, indexVersion: "1");

        var repo = fake.Open();

        Assert.ThrowsExactly<NotImplementedException>(() => repo.GetCommit(commit));
    }

    [TestMethod]
    public void PackIndex_LargeOffsets_ThrowsNotImplemented()
    {
        using var fake = new FakeRepo();
        // "2,0" puts every offset in the 64-bit large offset table, like in a pack > 2 GB
        var commit = PackChain(fake, indexVersion: "2,0");

        var repo = fake.Open();

        Assert.ThrowsExactly<NotImplementedException>(() => repo.GetCommit(commit));
    }

    [TestMethod]
    public void PackIndex_ShortReads_LoadsIndex()
    {
        using var fake = new FakeRepo();
        PackChain(fake, indexVersion: null);
        var idxPath = Directory.GetFiles(Path.Combine(fake.GitDir, "objects", "pack"), "*.idx").Single();

        // a stream that returns fewer bytes than requested, which Stream.Read is allowed to do
        using var stream = new TrickleStream(File.ReadAllBytes(idxPath), maxRead: 7);
        var index = new PackIndex("test", stream);

        Assert.AreEqual(2, index.Version);
        Assert.AreEqual(3, index.Objects);
    }

    [TestMethod]
    public void PackReader_TruncatedRefDeltaId_Throws()
    {
        using var s = PackedScenario.Create();
        var delta = s.VerifyB.First(o => o.Depth > 0);
        var packReader = new PackReader(s.Fake.GitDir);
        var entry = packReader.ReadPackEntryHeader(s.PackB, (int)delta.Offset);
        Assert.AreEqual(PackObjectType.OBJ_REF_DELTA, entry.Type);

        // cut the pack in the middle of the 20 byte base object id
        var packPath = Path.Combine(s.Fake.GitDir, "objects", "pack", $"{s.PackB}.pack");
        File.SetAttributes(packPath, FileAttributes.Normal);
        using (var file = new FileStream(packPath, FileMode.Open, FileAccess.Write))
        {
            file.SetLength(entry.ContentOffset - 10);
        }

        Assert.Throws<Exception>(() => packReader.ReadPackEntryHeader(s.PackB, (int)delta.Offset));
    }

    /// <summary>Writes a one commit history (blob, tree, commit) into a pack and returns the commit id.</summary>
    private static string PackChain(FakeRepo fake, string? indexVersion)
    {
        var blob = fake.WriteBlob("content\n");
        var tree = fake.WriteTree(("100644", "file.txt", blob));
        var commit = fake.WriteCommit(tree, null);
        fake.Pack([blob, tree, commit], ofsDelta: true, indexVersion);
        fake.PrunePacked();
        return commit;
    }

    private sealed class TrickleStream(byte[] data, int maxRead) : MemoryStream(data)
    {
        public override int Read(byte[] buffer, int offset, int count) => base.Read(buffer, offset, Math.Min(count, maxRead));

        public override int Read(Span<byte> buffer) => base.Read(buffer[..Math.Min(buffer.Length, maxRead)]);
    }
}
