using ICSharpCode.SharpZipLib.Zip.Compression.Streams;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Nordseth.Git
{
    public enum ObjectType
    {
        commit = 1,
        tree = 2,
        blob = 3,
        tag = 4,
    }

    public class ObjectReader
    {
        private readonly string _objectsPath;
        private readonly PackReader _packReader;

        public ObjectReader(string repoPath)
        {
            _objectsPath = Path.Combine(repoPath, "objects");
            _packReader = new PackReader(repoPath);
        }

        public IEnumerable<PackIndex> PackIndex { get; private set; }

        public void LoadIndex()
        {
            var indexFiles = Directory.EnumerateFiles(Path.Combine(_objectsPath, "pack"), "*.idx");

            var index = new List<PackIndex>();
            foreach (var indexFile in indexFiles)
            {
                using (var fileStream = File.OpenRead(indexFile))
                {
                    var packIndex = new PackIndex(Path.GetFileNameWithoutExtension(indexFile), fileStream);
                    index.Add(packIndex);
                }
            }

            PackIndex = index;
        }

        public (string packName, int offset) FindPackObject(string hash)
        {
            return FindPackObject(hash.HexToBytes());
        }

        public (string packName, int offset) FindPackObject(byte[] objectId)
        {
            if (PackIndex == null)
            {
                LoadIndex();
            }

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

        public (ObjectType objectType, Stream objectStream) GetObject(string hash)
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

        public (ObjectType objectType, Stream objectStream) GetUnpackedObject(string hash)
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
            var reader = new InflaterInputStream(stream);
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
                (objectType, baseObjectStream) = GetObjectFromPack(entry.Pack, entry.Offset - entry.RefOffset.Value);
            }
            else if (entry.Type == PackObjectType.OBJ_REF_DELTA)
            {
                // recursive
                (objectType, baseObjectStream) = GetObject(entry.RefObjectId.ToHexString());
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

        private string FindObjectIdInIndex(string pack, int offset)
        {
            if (PackIndex == null)
            {
                LoadIndex();
            }

            var index = PackIndex.FirstOrDefault(i => i.Name == pack);

            return index?.FindObjectId(offset)?.ToHexString();
        }
    }
}
