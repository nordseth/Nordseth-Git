using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Nordseth.Git
{
    public class PackIndex
    {
        private static byte[] _v2Header = new byte[] { 255, 116, 79, 99, 0, 0, 0, 2 };
        private int[] _fanOutTable;
        private byte[] _objectIds;
        private int[] _offsets;

        public PackIndex(string name, Stream stream)
        {
            Name = name;
            Read(stream);
        }

        public string Name { get; }
        public int Version { get; set; }
        public int Objects => _fanOutTable == null ? throw new InvalidOperationException("index not read") : _fanOutTable[255];

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

        public byte[] FindObjectId(int offset)
        {
            for (int i = 0; i < _fanOutTable[255];i++)
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

        private void Read(Stream stream)
        {
            var buffer = new byte[8];
            stream.Read(buffer, 0, 8);

            Version = GetIndexVersion(buffer);
            if (Version == 2)
            {
                ReadVersion2(stream);
            }
            else
            {
                ReadVersion1(buffer, stream);
            }
        }

        private void ReadVersion2(Stream stream)
        {
            ReadFanoutTable(stream);
            ReadObjectIds(stream, _fanOutTable[255]);
            // skip crc
            stream.Seek(_fanOutTable[255] * 4, SeekOrigin.Current);
            ReadOffsets(stream, _fanOutTable[255]);
            // 8 byte offers not supported
        }

        private void ReadVersion1(byte[] oldBuffer, Stream stream)
        {
            ReadFanoutTable(stream);

            int objects = _fanOutTable[255];

            var buffer = new byte[24 * objects];
            Array.Copy(oldBuffer, 0, buffer, 0, oldBuffer.Length);
            var toRead = buffer.Length - oldBuffer.Length;
            int read = stream.Read(buffer, oldBuffer.Length, toRead);
            if (read != toRead)
            {
                throw new Exception($"error reading index data, read {read} bytes, expected {toRead}");
            }

            _objectIds = new byte[20 * objects];
            _offsets = new int[objects];
            for (int i = 0; i < objects; i++)
            {
                int int32 = BitConverter.ToInt32(buffer, i * 24);
                _offsets[i] = System.Net.IPAddress.NetworkToHostOrder(int32);
                Array.Copy(buffer, i * 24 + 4, _objectIds, i * 20, 20);
            }

            throw new NotImplementedException($"version 1 index not tested!");
        }

        private void ReadOffsets(Stream stream, int objects)
        {
            var buffer = new byte[objects * 4];
            int read = stream.Read(buffer, 0, buffer.Length);
            if (read != buffer.Length)
            {
                throw new Exception($"error reading offsets, read {read} bytes, expected {buffer.Length}");
            }

            _offsets = new int[objects];
            for (int i = 0; i < objects; i++)
            {
                int int32 = BitConverter.ToInt32(buffer, i * 4);
                _offsets[i] = System.Net.IPAddress.NetworkToHostOrder(int32);
            }
        }

        private void ReadObjectIds(Stream stream, int objects)
        {
            _objectIds = new byte[20 * objects];
            int read = stream.Read(_objectIds, 0, _objectIds.Length);
            if (read != _objectIds.Length)
            {
                throw new Exception($"error reading object names, read {read} bytes, expected {_objectIds.Length}");
            }
        }

        private void ReadFanoutTable(Stream stream)
        {
            var buffer = new byte[256 * 4];
            int read = stream.Read(buffer, 0, buffer.Length);
            if (read != buffer.Length)
            {
                throw new Exception($"error reading fanout table, read {read} bytes, expected {buffer.Length}");
            }

            _fanOutTable = new int[256];
            for (int i = 0; i < 256; i++)
            {
                int int32 = BitConverter.ToInt32(buffer, i * 4);
                _fanOutTable[i] = System.Net.IPAddress.NetworkToHostOrder(int32);
            }
        }

        private int GetIndexVersion(byte[] buffer)
        {
            if (buffer.SequenceEqual(_v2Header))
            {
                return 2;
            }
            else
            {
                return 1;
            }
        }
    }
}
