using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TeraIO.Data
{
    public class TeraHash
    {
        public TeraHash(byte[] bytes)
        {

        }

        public static List<bool> ByteArrayToBoolList(IList<byte> byteArray)
        {
            List<bool> boolList = new List<bool>();

            foreach (byte b in byteArray)
            {
                boolList.Add((b & 1) != 0);
            }

            return boolList;
        }

        public static byte[] BoolArrayToByteArray(IList<bool> boolArray)
        {
            int boolCount = boolArray.Count;
            if (boolCount % 8 != 0)
            {
                throw new ArgumentException("bool[]的长度必须是8的倍数（包括0）", nameof(boolArray));
            }

            int byteCount = (boolCount + 7) / 8;
            byte[] byteArray = new byte[byteCount];

            for (int i = 0; i < boolCount; i++)
            {
                byte b = (byte)(boolArray[i] ? 1 : 0);
                int index = i / 8;
                int shift = i % 8;
                byteArray[index] |= (byte)(b << shift);
            }

            return byteArray;
        }
    }
}
