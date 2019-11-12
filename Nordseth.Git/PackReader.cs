using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Nordseth.Git
{
    public enum PackObjectType
    {
        OBJ_COMMIT = 1,
        OBJ_TREE = 2,
        OBJ_BLOB = 3,
        OBJ_TAG = 4,
        OBJ_OFS_DELTA = 6,
        OBJ_REF_DELTA = 7,
    }

    public class PackEntry
    {
        public PackEntry(string pack, PackObjectType type, int size, int offset)
        {
            Pack = pack;
            Type = type;
            Size = size;
            Offset = offset;
        }

        public string Pack { get; }
        public PackObjectType Type { get; }
        public int Size { get; }

        public int? RefOffset { get; set; }
        public byte[] RefObjectId { get; set; }

        public int Offset { get; }
        public long ContentOffset { get; set; }

        public override string ToString()
        {
            return $"{Type}, size: {Size}, offset: {Offset}/{ContentOffset}, ref: {RefObjectId?.ToHexString()}{RefOffset} - in {Pack}";
        }
    }

    public class PackReader
    {
        private readonly string _packPath;

        public PackReader(string repoPath)
        {
            _packPath = Path.Combine(repoPath, "objects", "pack");
        }

        public PackEntry ReadPackEntryHeader(string packName, int offset)
        {
            using (var fileStream = File.OpenRead(Path.Combine(_packPath, $"{packName}.pack")))
            {
                return ReadPackEntryHeader(packName, offset, fileStream);
            }
        }

        private PackEntry ReadPackEntryHeader(string pack, int offset, Stream stream)
        {
            stream.Seek(offset, SeekOrigin.Begin);

            int header = (byte)stream.ReadByte();

            var type = (PackObjectType)((header & 0b_0111_0000) >> 4);
            int size;
            if (header >= 128)
            {
                size = stream.ReadMbsInt(header & 0b_0000_1111, 4);
            }
            else
            {
                size = header & 0b_0000_1111;
            }

            var entry = new PackEntry(pack, type, size, offset);
            if (type == PackObjectType.OBJ_OFS_DELTA)
            {
                // negative relative offset
                entry.RefOffset = stream.ReadMbsOffsetInt();
            }
            else if (type == PackObjectType.OBJ_REF_DELTA)
            {
                // read 20 byte ref id
                var objectId = new byte[20];
                stream.Read(objectId, 0, 20);
                entry.RefObjectId = objectId;
            }

            entry.ContentOffset = stream.Position;
            return entry;
        }

        public (PackEntry, Stream) ReadPackEntry(string packName, int offset)
        {
            var fileStream = File.OpenRead(Path.Combine(_packPath, $"{packName}.pack"));

            var entry = ReadPackEntryHeader(packName, offset, fileStream);
            var entryStream = new ICSharpCode.SharpZipLib.Zip.Compression.Streams.InflaterInputStream(fileStream);

            return (entry, entryStream);
        }

        public IEnumerable<PackEntry> ReadPackEntryHeaderWithRefs(string hash, Func<string, (string, int)> findObject)
        {
            var (pack, offset) = findObject(hash);
            if (pack == null)
            {
                return null;
            }

            var result = new List<PackEntry>();
            while (true)
            {
                using (var fileStream = File.OpenRead(Path.Combine(_packPath, $"{pack}.pack")))
                {
                    var entry = ReadPackEntryHeader(pack, offset, fileStream);

                    result.Add(entry);

                    if (entry.Type == PackObjectType.OBJ_REF_DELTA)
                    {
                        (pack, offset) = findObject(entry.RefObjectId.ToHexString());
                    }
                    else if (entry.Type == PackObjectType.OBJ_OFS_DELTA)
                    {
                        offset = offset - entry.RefOffset.Value;
                    }
                    else
                    {
                        break;
                    }
                }
            }

            return result;
        }
    }
}
