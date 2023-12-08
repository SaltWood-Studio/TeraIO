using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TeraIO.Data
{
    /// <summary>
    /// This class is not recommended to use because it consumes too much memory when validating and generating big data
    /// 这个类不建议使用，因为其在验证、生成大数据时占用内存过大
    /// This class is not recommended to use because it consumes too much memory when validating and generating big data
    /// </summary>
    public class ErrorCorrector
    {
        // 输入的原始数据
        private IList<bool> data;

        // 生成的汉明码
        public IList<bool> verificationCode;

        // 构造函数，接收一个IList<bool>作为输入数据
        public ErrorCorrector(IList<bool> data)
        {
            this.data = data;
            this.verificationCode = GenerateErrorCorrector(this.data);
        }

        // 构造函数，接收一个IList<byte>作为输入数据
        public ErrorCorrector(IList<byte> data)
        {
            this.data = ByteArrayToBoolList(data);
            this.verificationCode = GenerateErrorCorrector(this.data);
        }

        /// <summary>
        /// 通过一个 <see cref="IList{Boolean}"/> 生成错误校验码
        /// </summary>
        /// <param name="bools"></param>
        /// <returns></returns>
        public IList<bool> GenerateErrorCorrector(IList<bool> bools)
        {
            long exponent = (long)Math.Log2(this.data.Count);
            List<bool> result = new List<bool>();
            for (int i = 1; i < exponent; i++)
            {
                bool bit = false;
                for (int j = 0; j < bools.Count; j++)
                {
                    if ((j / (long)Math.Pow(2, i)) % 2 != 0)
                    {
                        Console.WriteLine($"{i}, {j}");
                        continue;
                    }
                    else
                    {
                        bit ^= bools[j];
                    }
                }
                result.Add(bit);
            }
            return result;
        }

        /// <summary>
        /// 通过一个 <see cref="IList{Byte}"/> 生成错误校验码
        /// </summary>
        /// <param name="bytes"></param>
        /// <returns></returns>
        public IList<bool> GenerateErrorCorrector(IList<byte> bytes)
        {
            IList<bool> input = ByteArrayToBoolList(bytes);
            return GenerateErrorCorrector(input);
        }

        // 检查输入的bools是否与生成的错误校验码一致
        public bool Check(IList<bool> b)
        {
            var result = GenerateErrorCorrector(b);
            return this.verificationCode.All(result.Contains) && this.verificationCode.Count == result.Count;
        }

        // 检查输入的bytes是否与生成的错误校验码一致
        public bool Check(IList<byte> b)
        {
            var input = ByteArrayToBoolList(b);
            return Check(input);
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

        /*
        private List<bool> CompleteList()
        {
            List<bool> result = new List<bool>();
            long targetLength = (long)Math.Pow(2, Math.Ceiling(Math.Log2(this.data.Count)));
            result = result.Concat(this.data).ToList();
            for (int i = this.data.Count; i < targetLength; i++)
            {
                result.Add(false);
            }
            return result;
        }

        private List<bool> CompleteList(IList<bool> data)
        {
            List<bool> result = new List<bool>();
            long targetLength = (long)Math.Pow(2, Math.Ceiling(Math.Log2(data.Count)));
            result = result.Concat(data).ToList();
            for (int i = data.Count; i < targetLength; i++)
            {
                result.Add(false);
            }
            return result;
        }*/

        public override string ToString()
        {
            // 创建一个空的字符串列表，用于存储每6个bool值转换成的二进制字符串
            List<string> binaryStrings = new List<string>();

            // 遍历verificationCode列表中的每个bool值
            for (int i = 0; i < verificationCode.Count; i++)
            {/*
                // 如果当前索引是6的倍数，则在字符串列表中添加一个空格
                if (i > 0 && i % 6 == 0)
                {
                    binaryStrings.Add(" ");
                }*/

                // 将当前bool值转换为二进制字符串，并添加到字符串列表中
                binaryStrings.Add(verificationCode[i] ? "1" : "0");
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
        }
    }
}
