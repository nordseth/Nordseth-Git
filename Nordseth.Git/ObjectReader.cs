using System.IO.Compression;
using System.Text;

namespace Nordseth.Git;

public enum ObjectType
{
    commit = 1,
    tree = 2,
    blob = 3,
    tag = 4,
}

public class ObjectReader
{
    private IReadOnlyList<PackIndex>? _packIndex;
    private readonly string _objectsPath;
    private readonly PackReader _packReader;

    public ObjectReader(string repoPath)
    {
        _objectsPath = Path.Combine(repoPath, "objects");
        _packReader = new PackReader(repoPath);
    }

    public IReadOnlyList<PackIndex> PackIndex => _packIndex ??= LoadIndexCore();

    public void LoadIndex() => _packIndex = LoadIndexCore();

    private List<PackIndex> LoadIndexCore()
    {
        var index = new List<PackIndex>();
        foreach (var indexFile in Directory.EnumerateFiles(Path.Combine(_objectsPath, "pack"), "*.idx"))
        {
            using var fileStream = File.OpenRead(indexFile);
            index.Add(new PackIndex(Path.GetFileNameWithoutExtension(indexFile), fileStream));
        }

        return index;
    }

    public (string? packName, int offset) FindPackObject(string hash)
    {
        return FindPackObject(hash.HexToBytes());
    }

    public (string? packName, int offset) FindPackObject(byte[] objectId)
    {
        foreach (var i in PackIndex)
        {
            var result = i.FindObject(objectId);
            if (result.HasValue)
            {
                return (i.Name, result.Value);
            }
        }

        return (null, -1);
    }

    public (ObjectType objectType, Stream? objectStream) GetObject(string hash)
    {
        var (type, unpackedObject) = GetUnpackedObject(hash);
        if (unpackedObject != null)
        {
            return (type, unpackedObject);
        }

        var (pack, offset) = FindPackObject(hash);
        if (pack == null)
        {
            // not found
            return (0, null);
        }

        return GetObjectFromPack(pack, offset);
    }

    private (ObjectType objectType, Stream objectStream) GetObjectFromPack(string pack, int offset)
    {
        var (entry, stream) = _packReader.ReadPackEntry(pack, offset);

        if (entry.Type == PackObjectType.OBJ_OFS_DELTA || entry.Type == PackObjectType.OBJ_REF_DELTA)
        {
            return ReadDeltaObject(entry, stream);
        }

        return (entry.Type.ToObjectType(), stream);
    }

    public (ObjectType objectType, Stream? objectStream) GetUnpackedObject(string hash)
    {
        if (hash.Length != 40)
        {
            throw new InvalidOperationException($"invalid hash {hash}");
        }

        string filePath = Path.Combine(_objectsPath, hash.Substring(0, 2), hash.Substring(2));
        if (!File.Exists(filePath))
        {
            return (0, null);
        }

        var fileStream = File.OpenRead(filePath);
        return ReadUnpackedObject(fileStream);
    }

    public (ObjectType objectType, Stream objectStream) ReadUnpackedObject(Stream stream)
    {
        var reader = new ZLibStream(stream, CompressionMode.Decompress);
        var buffer = new byte[100];
        int i = 0;
        while (true)
        {
            int read = reader.ReadByte();
            // read til 0 byte or end
            if (read <= 0)
            {
                break;
            }

            buffer[i++] = (byte)read;
        }

        string header = Encoding.UTF8.GetString(buffer, 0, i);
        int seperator = header.IndexOf(' ');
        if (seperator > 0 && Enum.TryParse<ObjectType>(header.Substring(0, seperator), out var type))
        {
            return (type, reader);
        }
        else
        {
            return (0, reader);
        }
    }

    public (ObjectType objectType, Stream objectStream) ReadDeltaObject(PackEntry entry, Stream delta)
    {
        ObjectType objectType;
        Stream baseObjectStream;

        if (entry.Type == PackObjectType.OBJ_OFS_DELTA)
        {
            (objectType, baseObjectStream) = GetObjectFromPack(entry.Pack, entry.Offset - entry.RefOffset!.Value);
        }
        else if (entry.Type == PackObjectType.OBJ_REF_DELTA)
        {
            // recursive
            var baseId = entry.RefObjectId!.ToHexString();
            (objectType, var baseStream) = GetObject(baseId);
            baseObjectStream = baseStream ?? throw new InvalidOperationException($"Base object {baseId} for delta {entry} not found");
        }
        else
        {
            throw new InvalidOperationException($"{entry.Type} not a delta object");
        }

        try
        {
            var deltaStream = new DeltaStream(delta, baseObjectStream);
            return (objectType, deltaStream);
        }
        catch (Exception ex)
        {
            var objectId = FindObjectIdInIndex(entry.Pack, entry.Offset);
            throw new Exception($"Error creating delta stream for {entry} - objectId: {objectId ?? "NOT FOUND!"}", ex);
        }
    }

    private string? FindObjectIdInIndex(string pack, int offset)
    {
        var index = PackIndex.FirstOrDefault(i => i.Name == pack);

        return index?.FindObjectId(offset)?.ToHexString();
    }
}
