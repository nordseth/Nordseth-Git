using ICSharpCode.SharpZipLib.Zip.Compression.Streams;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Nordseth.Git
{
    public class DeltaStream : Stream
    {
        private Stream _deltaReader;
        private byte[] _baseObject;

        public DeltaStream(Stream delta, Stream baseObject)
        {
            _deltaReader = delta;

            // read the whole base object!
            using (baseObject)
            {
                var memStream = new MemoryStream();
                baseObject.CopyTo(memStream);
                _baseObject = memStream.ToArray();
            }
        }

        public static string DescribeDelta(Stream fileStream)
        {
            var writer = new StringBuilder();
            using (var stream = new InflaterInputStream(fileStream))
            {
                int sourceLen = stream.ReadMbsInt();
                int targetLen = stream.ReadMbsInt();
                writer.AppendLine($"Delta sourceLen: {sourceLen}, targetLen: {targetLen}");
                while (true)
                {
                    int read = stream.ReadByte();
                    if (read == -1)
                    {
                        break;
                    }

                    if (read >= 128)
                    {
                        var (copyOffset, copySize) = ReadCopy(stream, (byte)read);

                        writer.AppendLine($"COPY {copyOffset} {copySize}");
                    }
                    else
                    {
                        // insert
                        writer.Append($"Insert {read} - ");

                        for (int i = 0; i < read; i++)
                        {
                            var x = stream.ReadByteWithCheck();
                            writer.Append((char)x);
                        }

                        writer.AppendLine();
                    }
                }
            }

            return writer.ToString();
        }

        public override bool CanRead => true;
        public override bool CanSeek => false;
        public override bool CanWrite => false;
        public override long Length => throw new NotImplementedException();
        public override long Position { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

        public int SourceLength { get; private set; }
        public int TargetLenght { get; private set; }

        private bool _init;
        private bool _inCopy;
        private int _copyOffset;
        private int _copyCount;
        private bool _inInsert;
        private int _insertCount;

        public override int Read(byte[] buffer, int offset, int count)
        {
            if (!_init)
            {
                SourceLength = _deltaReader.ReadMbsInt();
                TargetLenght = _deltaReader.ReadMbsInt();
                _init = true;
            }

            int readCount = 0;

            while (readCount < count)
            {
                if (_inCopy)
                {
                    // copy up to count-readCount from _baseObject to buffer
                    int toCopy = Math.Min(count - readCount, _copyCount);
                    Array.Copy(_baseObject, _copyOffset, buffer, offset + readCount, toCopy);
                    _copyOffset += toCopy;
                    _copyCount -= toCopy;
                    readCount += toCopy;

                    if (_copyCount == 0)
                    {
                        _inCopy = false;
                    }
                }
                else if (_inInsert)
                {
                    // copy up to count-readCount from _deltaStream to buffer 
                    int toInsert = Math.Min(count - readCount, _insertCount);
                    int inserted = _deltaReader.Read(buffer, offset + readCount, toInsert);

                    _insertCount -= inserted;
                    readCount += inserted;

                    if (_insertCount == 0)
                    {
                        _inInsert = false;
                    }
                }
                else
                {
                    // read next instruction
                    int read = _deltaReader.ReadByte();
                    if (read == -1)
                    {
                        break;
                    }

                    if (read >= 128)
                    {
                        (_copyOffset, _copyCount) = ReadCopy(_deltaReader, (byte)read);
                        _inCopy = true;
                    }
                    else
                    {
                        _insertCount = read;
                        _inInsert = true;
                    }
                }
            }

            return readCount;
        }

        public override void Flush()
        {
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            throw new NotImplementedException();
        }

        public override void SetLength(long value)
        {
            throw new NotImplementedException();
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            throw new NotImplementedException();
        }

        protected override void Dispose(bool disposing)
        {
            _deltaReader?.Dispose();
            _deltaReader = null;
            if (disposing)
            {
                _baseObject = null;
            }
        }

        private static (int offset, int size) ReadCopy(Stream stream, byte read)
        {
            int offset = 0;
            int size = 0;

            if ((read & 0b_0000_0001) > 0)
            {
                offset = stream.ReadByteWithCheck();
            }

            if ((read & 0b_0000_0010) > 0)
            {
                offset |= (stream.ReadByteWithCheck() << 8);
            }

            if ((read & 0b_0000_0100) > 0)
            {
                offset |= (stream.ReadByteWithCheck() << 16);
            }

            if ((read & 0b_0000_1000) > 0)
            {
                offset |= (stream.ReadByteWithCheck() << 24);
            }

            if ((read & 0b_0001_0000) > 0)
            {
                size = stream.ReadByteWithCheck();
            }

            if ((read & 0b_0010_0000) > 0)
            {
                size |= (stream.ReadByteWithCheck() << 8);
            }

            if ((read & 0b_0100_0000) > 0)
            {
                size |= (stream.ReadByteWithCheck() << 16);
            }

            if (size == 0)
            {
                size = 0x10000;
            }

            return (offset, size);
        }
    }
}