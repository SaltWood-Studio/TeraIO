using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TeraIO.Data
{
    public class MD5
    {
        public const uint hashValue1 = 0x67452301;
        public const uint hashValue2 = 0xEFCDAB89;
        public const uint hashValue3 = 0x98BACDFE;
        public const uint hashValue4 = 0x10325476;

        public static readonly uint[] constK = {
            0xd76aa478, 0xf57c0faf, 0xe8c7b756, 0x4787c62a,
            0x698098d8, 0x8b44f7af, 0x242070db, 0xc1bdceee,
            0xa8304613, 0xfd469501, 0xffff5bb1, 0x895cd7be,
            0x6b901122, 0x49b40821, 0x265e5a51, 0xe9b6c7aa,
            0xfd987193, 0xa679438e, 0xf61e2562, 0xc040b340,
            0xd8a1e681, 0xe7d3fbc8, 0xd62f105d, 0x02441453,
            0x21e1cde6, 0xc33707d6, 0xf4d50d87, 0x455a14ed,
            0xa9e3e905, 0xfcefa3f8, 0x676f02d9, 0x8d2a4c8a,
            0x6d9d6122, 0xfde5380c, 0xfffa3942, 0x8771f681,
            0xa4beea44, 0x4bdecfa9, 0xf6bb4b60, 0xbebfbc70,
            0x289b7ec6, 0xeaa127fa, 0xd4ef3085, 0x04881d05,
            0xd9d4d039, 0xe6db99e5, 0x1fa27cf8, 0xc4ac5665,
            0xf4292244, 0x432aff97, 0xfc93a039, 0xab9423a7,
            0x655b59c3, 0x6fa87e4f, 0xf7537e82, 0xbd3af235,
            0x2ad7d2bb, 0xeb86d391, 0xffeff47d, 0x8f0ccc92,
            0x85845dd1, 0xfe2ce6e0, 0xa3014314, 0x4e0811a1
        };

        public static readonly int[] constS =
        {
            7, 12, 17, 22, 7, 12, 17, 22, 7, 12, 17, 22, 7, 12, 17, 22,
            5, 9,  14, 20, 5, 9,  14, 20, 5, 9,  14, 20, 5, 9,  14, 20,
            4, 11, 16, 23, 4, 11, 16, 23, 4, 11, 16, 23, 4, 11, 16, 23,
            6, 10, 15, 21, 6, 10, 15, 21, 6, 10, 15, 21, 6, 10, 15, 21
        };

        public static byte[] Pad(byte[] data)
        {
            byte[] result = new byte[GetLength(data) * 64];
            data.CopyTo(result, 0);
            result[data.LongLength] = 0x80;
            GetByteArrayFromLong(data.LongLength).CopyTo(result, result.LongLength - 8);
            return result;
        }

        public static byte[] ComputeMD5(byte[] data)
        {
            byte[] paddedData = Pad(data);
            var hashSet = (
                hashValue1,
                hashValue2,
                hashValue3,
                hashValue4
            );
            var (a, b, c, d) = hashSet;
            byte[] last = new byte[4];
            for (int i = 0; i < paddedData.Length; i += 64)
            {
                last = Compute64(paddedData[i..(i + 64)],
                    ref hashSet,
                    hashSet
                );
            }
            return last;
        }

        public static byte[] Compute64
        (byte[] data,
            ref (uint, uint, uint, uint) endValues,
            (uint, uint, uint, uint) startValues = default
        )
        {
            uint a, b, c, d;
            if (startValues == default)
            {
                a = hashValue1;
                b = hashValue2;
                c = hashValue3;
                d = hashValue4;
            }
            else
                (a, b, c, d) = startValues;

            for (int i = 0; i < 64; i++)
            {
                uint f;
                uint g;

                if (0 <= i && i <= 15)
                {
                    int index = i * 4;
                    f = (uint)((b & c) | ((-b) & d));
                    g = ByteArrayToUInt32LittleEndian(data[index..(index + 4)]);
                }
                else if (16 <= i && i <= 31)
                {
                    int index = (5 * i + 1) % 16;
                    f = (uint)((d & b) | ((-d) & c));
                    g = ByteArrayToUInt32LittleEndian(data[index..(index + 4)]);
                }
                else if (32 <= i && i <= 47)
                {
                    int index = (3 * i + 5) % 16;
                    f = b ^ c ^ d;
                    g = ByteArrayToUInt32LittleEndian(data[index..(index + 4)]);
                }
                else
                {
                    int index = (7 * i) % 16;
                    f = (uint)(c ^ (b | (-d)));
                    g = ByteArrayToUInt32LittleEndian(data[index..(index + 4)]);
                }
                a = ((a + f + constK[i] + g) << constS[i]) + b;

                (b, c, d, a) = (a, b, c, d);
            }

            endValues = (a, b, c, d);

            byte[] result = new byte[16];

            UInt32ToByteArray(hashValue1 + a).CopyTo(result, 0);
            UInt32ToByteArray(hashValue2 + b).CopyTo(result, 4);
            UInt32ToByteArray(hashValue3 + c).CopyTo(result, 8);
            UInt32ToByteArray(hashValue4 + d).CopyTo(result, 12);

            return result;
        }

        public static byte[] UInt32ToByteArray(uint value)
        {
            byte[] byteArray = new byte[4];
            byteArray[3] = (byte)(value >> 24);
            byteArray[2] = (byte)((value >> 16) & 0xFF);
            byteArray[1] = (byte)((value >> 8) & 0xFF);
            byteArray[0] = (byte)(value & 0xFF);
            return byteArray;
        }

        public static uint ByteArrayToUInt32LittleEndian(byte[] bytes)
        {
            if (bytes == null || bytes.Length != 4)
            {
                throw new ArgumentException("Byte array must be 4 bytes long.");
            }

            return (uint)((bytes[0] << 24) |
                           (bytes[1] << 16) |
                           (bytes[2] << 8) |
                           bytes[3]);
        }

        public static byte[] GetByteArrayFromLong(long value)
        {
            byte[] byteArray = new byte[8];
            byteArray[7] = (byte)((value >> 56) & 0xFF);
            byteArray[6] = (byte)((value >> 48) & 0xFF);
            byteArray[5] = (byte)((value >> 40) & 0xFF);
            byteArray[4] = (byte)((value >> 32) & 0xFF);
            byteArray[3] = (byte)((value >> 24) & 0xFF);
            byteArray[2] = (byte)((value >> 16) & 0xFF);
            byteArray[1] = (byte)((value >> 8) & 0xFF);
            byteArray[0] = (byte)(value & 0xFF);
            return byteArray;
        }

        public static void GetByteArrayFromLong(long value, byte[] byteArray)
        {
            byteArray[0] = (byte)(value >> 56);
            byteArray[1] = (byte)(value >> 48);
            byteArray[2] = (byte)(value >> 40);
            byteArray[3] = (byte)(value >> 32);
            byteArray[4] = (byte)(value >> 24);
            byteArray[5] = (byte)(value >> 16);
            byteArray[6] = (byte)(value >> 8);
            byteArray[7] = (byte)value;
        }

        public static void GetByteArrayFromLong(long value, out byte[] byteArray)
        {
            byteArray = new byte[8];
            byteArray[0] = (byte)(value >> 56);
            byteArray[1] = (byte)(value >> 48);
            byteArray[2] = (byte)(value >> 40);
            byteArray[3] = (byte)(value >> 32);
            byteArray[4] = (byte)(value >> 24);
            byteArray[5] = (byte)(value >> 16);
            byteArray[6] = (byte)(value >> 8);
            byteArray[7] = (byte)value;
        }

        public static int GetLength(byte[] data)
        {
            return (int)(Math.Ceiling((data.Length + 9) / 64.0));
        }
    }
}
