using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Nordseth.Git
{
    public static class Helpers
    {
        public static string ToHexString(this byte[] hash) => BitConverter.ToString(hash).Replace("-", string.Empty).ToLowerInvariant();

        public static byte[] HexToBytes(this string hex)
        {
            if (hex.Length % 2 == 1)
            {
                throw new Exception("The binary key cannot have an odd number of digits");
            }

            byte[] arr = new byte[hex.Length >> 1];

            for (int i = 0; i < hex.Length >> 1; ++i)
            {
                arr[i] = (byte)((GetHexVal(hex[i << 1]) << 4) + (GetHexVal(hex[(i << 1) + 1])));
            }

            return arr;
        }

        private static int GetHexVal(char hex)
        {
            int val = (int)hex;
            //For uppercase A-F letters:
            //return val - (val < 58 ? 48 : 55);
            //For lowercase a-f letters:
            //return val - (val < 58 ? 48 : 87);
            //Or the two combined, but a bit slower:
            return val - (val < 58 ? 48 : (val < 97 ? 55 : 87));
        }

        public static ObjectType ToObjectType(this PackObjectType packObjectType)
        {
            switch (packObjectType)
            {
                case PackObjectType.OBJ_COMMIT:
                case PackObjectType.OBJ_TREE:
                case PackObjectType.OBJ_BLOB:
                case PackObjectType.OBJ_TAG:
                    return (ObjectType)packObjectType;
                case PackObjectType.OBJ_OFS_DELTA:
                case PackObjectType.OBJ_REF_DELTA:
                default:
                    return 0;
            }
        }

        public static int ReadByteWithCheck(this Stream s)
        {
            int read = s.ReadByte();
            if (read == -1)
            {
                throw new NotImplementedException($"Read past end of stream");
            }

            return read;
        }

        public static int ReadMbsInt(this Stream stream, int initialValue = 0, int initialBit = 0)
        {
            int value = initialValue;
            int currentBit = initialBit;
            while (true)
            {
                var read = ReadByteWithCheck(stream);

                int byteRead = (read & 0b_0111_1111) << currentBit;
                value |= byteRead;
                currentBit += 7;

                if (read < 128)
                {
                    break;
                }
            }

            return value;
        }

        // https://github.com/ChimeraCoder/gitgo/blob/master/verify-pack.go#L188
        public static int ReadMbsOffsetInt(this Stream stream)
        {
            int offset = 0;
            int bytesRead = 0;
            bool mbs = true;

            while (mbs)
            {
                bytesRead++;

                var byteRead = stream.ReadByteWithCheck();

                offset = (offset << 7) + (byteRead & 0b_0111_1111);
                mbs = byteRead >= 128;
            }

            if (bytesRead >= 2)
            {
                offset += (1 << (7 * (bytesRead - 1)));
            }

            return offset;
        }
    }
}
