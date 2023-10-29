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

        public static List<bool> ByteArrayToBoolList(byte[] byteArray)
        {
            List<bool> boolList = new List<bool>();

            foreach (byte b in byteArray)
            {
                boolList.Add((b & 1) != 0);
            }

            return boolList;
        }

        public static List<bool[,]> ChunkBools(List<bool> bools)
        {
            List<bool[,]> result = new List<bool[,]>();
            int count = 0;
            result.Add(new bool[8, 8]);
            foreach (bool i in bools)
            {
                if (count >= 64)
                {
                    result.Add(new bool[8, 8]);
                    count = 0;
                }
                result.Last()[count % 8, count / 8] = i;
                count++;
            }
            return result;
        }
    }
}
