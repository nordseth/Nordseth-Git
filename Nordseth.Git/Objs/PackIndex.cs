namespace Nordseth.Git;

public class PackIndex
{
    private static byte[] _v2Header = new byte[] { 255, 116, 79, 99, 0, 0, 0, 2 };
    private readonly int[] _fanOutTable;
    private readonly byte[] _objectIds;
    private readonly int[] _offsets;

    public PackIndex(string name, Stream stream)
    {
        Name = name;

        var header = new byte[8];
        stream.ReadExactly(header);

        Version = header.AsSpan().SequenceEqual(_v2Header) ? 2 : 1;
        if (Version != 2)
        {
            throw new NotSupportedException("Pack index version 1 is not supported");
        }

        _fanOutTable = ReadFanoutTable(stream);
        _objectIds = ReadObjectIds(stream, _fanOutTable[255]);
        // skip crc
        stream.Seek(_fanOutTable[255] * 4, SeekOrigin.Current);
        _offsets = ReadOffsets(stream, _fanOutTable[255]);
        // 8 byte offers not supported
    }

    public string Name { get; }
    public int Version { get; }
    public int Objects => _fanOutTable[255];

    public int? FindObject(byte[] objectId)
    {
        if (objectId.Length != 20)
        {
            throw new InvalidOperationException($"invalid objectId {objectId}");
        }

        // check _fanOutTable for range to scan
        byte firstByte = objectId[0];
        int count = _fanOutTable[firstByte];
        if (firstByte == 0 && count == 0)
        {
            return null;
        }
        else if (firstByte == 0)
        {
            return ScanRange(objectId, 0, count);
        }

        int countBefore = _fanOutTable[firstByte - 1];
        if (countBefore == count)
        {
            return null;
        }
        else
        {
            return ScanRange(objectId, countBefore, count);
        }
    }

    public byte[]? FindObjectId(int offset)
    {
        for (int i = 0; i < _fanOutTable[255]; i++)
        {
            if (_offsets[i] == offset)
            {
                var result = new byte[20];
                Array.Copy(_objectIds, i * 20, result, 0, 20);
                return result;
            }
        }

        return null;
    }

    private int? ScanRange(byte[] objectId, int start, int end)
    {
        // todo: binary search
        // scan range in _objectNames
        for (int i = start; i < end; i++)
        {
            if (CompareObjectId(_objectIds, i, objectId))
            {
                // return value from _offsets
                return _offsets[i];
            }
        }

        return null;
    }

    private bool CompareObjectId(byte[] objectIds, int index, byte[] objectId)
    {
        for (int i = 0; i < 20; i++)
        {
            if (objectIds[index * 20 + i] != objectId[i])
            {
                return false;
            }
        }

        return true;
    }

    private static int[] ReadOffsets(Stream stream, int objects)
    {
        var buffer = new byte[objects * 4];
        stream.ReadExactly(buffer, 0, buffer.Length);

        var offsets = new int[objects];
        for (int i = 0; i < objects; i++)
        {
            int int32 = BitConverter.ToInt32(buffer, i * 4);
            offsets[i] = System.Net.IPAddress.NetworkToHostOrder(int32);
        }
        return offsets;
    }

    private static byte[] ReadObjectIds(Stream stream, int objects)
    {
        var objectIds = new byte[20 * objects];
        stream.ReadExactly(objectIds, 0, objectIds.Length);
        return objectIds;
    }

    private static int[] ReadFanoutTable(Stream stream)
    {
        var buffer = new byte[256 * 4];
        stream.ReadExactly(buffer, 0, buffer.Length);

        var fanOutTable = new int[256];
        for (int i = 0; i < 256; i++)
        {
            int int32 = BitConverter.ToInt32(buffer, i * 4);
            fanOutTable[i] = System.Net.IPAddress.NetworkToHostOrder(int32);
        }
        return fanOutTable;
    }
}
