using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TeraIO.Data
{
    public class HammingCode
    {
        // 输入的原始数据
        private List<bool> data;

        // 生成的汉明码
        public List<bool> hammingCode;

        // 构造函数，接收一个List<bool>作为输入数据
        public HammingCode(List<bool> data)
        {
            this.data = data;
            this.hammingCode = GenerateHammingCode();
        }

        // 生成汉明码的私有方法
        private List<bool> GenerateHammingCode()
        {
            List<bool[]> bools = GroupBoolsBy64(this.data);
            List<bool> result = new List<bool>();
            foreach (bool[] i in bools)
            {
                bool[][] array = ConvertTo8x8(i);
                bool[] check = new bool[7];
                check[0] = i.Count(it => it) % 2 == 1;
                check[1] = (array[1].Count(it => it) + array[3].Count(it => it)) % 2 == 1;
                check[2] = ((array[2].Count(it => it) + array[3].Count(it => it)) + (array[6].Count(it => it) + array[7].Count(it => it))) % 2 == 1;
                check[3] = ((array[4].Count(it => it) + array[5].Count(it => it)) + (array[6].Count(it => it) + array[7].Count(it => it))) % 2 == 1;
                check[4] = (array.Select(it => it[1]).Count(it => it) + array.Select(it => it[3]).Count(it => it)) % 2 == 1;
                check[5] = (array.Select(it => it[2]).Count(it => it) + array.Select(it => it[3]).Count(it => it)) % 2 == 1;
                check[6] = (array.Select(it => it[4]).Count(it => it) + array.Select(it => it[5]).Count(it => it) + array.Select(it => it[6]).Count(it => it) + array.Select(it => it[7]).Count(it => it)) % 2 == 1;
                foreach (var item in check)
                {
                    result.Add(item);
                }
            }
            return result;
        }

        public static bool[][] ConvertTo8x8(bool[] bools)
        {
            if (bools == null || bools.Length != 64)
            {
                throw new ArgumentException("Input array must have 64 elements.", nameof(bools));
            }

            bool[][] result = new bool[8][];
            for (int i = 0; i < 8; i++)
            {
                result[i] = new bool[8];
            }

            for (int i = 0; i < 64; i++)
            {
                int row = i / 8;
                int col = i % 8;
                result[row][col] = bools[i];
            }
            return result;
        }

        public static List<bool[]> GroupBoolsBy64(List<bool> bools)
        {
            List<bool[]> groupedBools = new List<bool[]>();
            int count = 0;

            while (count < bools.Count)
            {
                bool[] group = new bool[64];
                for (int i = 0; i < 64 && count < bools.Count; i++)
                {
                    group[i] = bools[count];
                    count++;
                }
                groupedBools.Add(group);
            }

            return groupedBools;
        }

        // 获取汉明码的公共方法
        public List<bool> GetHammingCode()
        {
            return hammingCode;
        }

        public bool CheckData(List<bool> data)
        {
            // 计算原始数据的长度n和校验位数k
            int n = data.Count;
            int k = (int)Math.Ceiling(n / 2.0);

            // 构建校验矩阵，初始值为k个false
            List<bool> parityCheckMatrix = new List<bool>();
            for (int i = 0; i < k; i++)
            {
                parityCheckMatrix.Add(false);
            }

            // 遍历原始数据中的每一位i（从k开始）
            for (int i = k; i < n; i++)
            {
                // 构建当前编码（从i-k开始，长度为k）
                List<bool> code = new List<bool>(data.GetRange(i - k, k));
                code.Add(false); // 添加校验位

                // 判断当前编码是否为校验位
                bool isParity = true;
                for (int j = 0; j < k; j++)
                {
                    if (code[j] == data[i])
                    {
                        isParity = false;
                        break;
                    }
                }

                // 如果当前编码是校验位，则将其添加到校验矩阵中
                if (isParity)
                {
                    parityCheckMatrix.Add(true);
                }
                else
                {
                    parityCheckMatrix.Add(false);
                }
            }

            // 检查校验矩阵是否与汉明码中的校验矩阵相同
            if (parityCheckMatrix.Count != k)
            {
                return false;
            }

            for (int i = 0; i < k; i++)
            {
                if (parityCheckMatrix[i] != this.hammingCode[i])
                {
                    return false;
                }
            }

            return true;
        }

        public List<bool> Repair(List<bool> data, List<bool> repairMatrix)
        {
            // 计算原始数据的长度n和校验位数k
            int n = data.Count;
            int k = (int)Math.Ceiling(n / 2.0);

            // 检查修复矩阵的长度是否与原始数据长度相同
            if (repairMatrix.Count != n)
            {
                throw new ArgumentException("The repair matrix must have the same length as the original data.");
            }

            // 遍历原始数据中的每一位i（从0开始）
            for (int i = 0; i < n; i++)
            {
                // 如果原始数据中的第i位是错误的，则使用修复矩阵中的第i位进行修复
                if (!data[i])
                {
                    data[i] = repairMatrix[i];
                }
            }

            // 生成新的汉明码
            hammingCode = GenerateHammingCode();

            return data;
        }

        public override string ToString()
        {
            // 创建一个空的字符串列表，用于存储每6个bool值转换成的二进制字符串
            List<string> binaryStrings = new List<string>();

            // 遍历hammingCode列表中的每个bool值
            for (int i = 0; i < hammingCode.Count; i++)
            {/*
                // 如果当前索引是6的倍数，则在字符串列表中添加一个空格
                if (i > 0 && i % 6 == 0)
                {
                    binaryStrings.Add(" ");
                }*/

                // 将当前bool值转换为二进制字符串，并添加到字符串列表中
                binaryStrings.Add(hammingCode[i] ? "1" : "0");
            }

            // 将字符串列表中的所有字符串连接在一起，形成一个完整的字符串
            string result = string.Join("", binaryStrings);

            // 计算剩余的字符数量，并使用空字符串填充
            int remainder = result.Length % 6;
            if (remainder > 0)
            {
                result += new string('0', 6 - remainder);
            }

            // 创建一个新的字符列表，用于存储转换后的Base64编码字符
            List<char> chars = new List<char>();

            // 遍历转换后的字符串，将其转换为Base64编码字符，并添加到字符列表中
            for (int i = 0; i < result.Length; i += 6)
            {
                int value = Convert.ToInt32(result.Substring(i, 6), 2);
                chars.Add((char)('A' + value));
            }

            // 将字符列表中的所有字符连接在一起，形成一个完整的Base64编码字符串
            return new string(chars.ToArray());
        }=
    }
}
