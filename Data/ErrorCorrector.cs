using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TeraIO.Extension;

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
        //private IList<bool> data;
        private BitArray data;
        // 生成的校验码
        public BitArray verificationCode;
        public BitArray VerificationCode
        {
            get => (BitArray)this.verificationCode.Clone();
            private set => this.verificationCode = value;
        }

        /// <summary>
        /// 构造函数，接收一个 IList<bool> 作为输入数据
        /// </summary>
        public ErrorCorrector(IList<bool> data)
        {
            this.data = new BitArray(BoolListToByteArray(data));
            this.verificationCode = GenerateErrorCorrector(this.data);
        }

        /// <summary>
        /// 构造函数，接收一个 IList<byte> 作为输入数据
        /// </summary>
        public ErrorCorrector(IList<byte> data)
        {
            this.data = new BitArray(data.ToArray());
            this.verificationCode = GenerateErrorCorrector(this.data);
        }

        /// <summary>
        /// 构造函数，接收一个 BitArray 作为输入数据
        /// </summary>
        public ErrorCorrector(BitArray data)
        {
            this.data = data;
            this.verificationCode = GenerateErrorCorrector(this.data);
        }

        /// <summary>
        /// 通过一个 <see cref="IList{Byte}"/> 生成错误校验码
        /// </summary>
        /// <param name="bools"></param>
        /// <returns></returns>
        public BitArray GenerateErrorCorrector(IList<byte> bytes)
        {
            BitArray values = new BitArray(bytes.ToArray());
            return GenerateErrorCorrector(values);
        }

        // 通过一个
        public BitArray GenerateErrorCorrector(BitArray values)
        {
            long exponent = (long)Math.Ceiling(Math.Log2(this.data.Count));
            BitArray result = new BitArray(new byte[0]);
            result.Length += 1;
            foreach (var value in values)
            {
                if (value is bool)
                {
                    bool boolValue = (bool)value;
                    result[^1] ^= boolValue;
                }
            }
            for (int i = 1; i < exponent; i++)
            {
                bool bit = false;
                for (int j = 0; j < values.Count; j++)
                {
                    if ((j / (long)Math.Pow(2, i)) % 2 != 0)
                    {
                        //Console.WriteLine($"{i}, {j}");
                        continue;
                    }
                    else
                    {
                        bool b = values[j];
                        //b.Dump();
                        bit ^= b;
                    }
                }
                result.Length += 1;
                result[^1] = bit;
            }
            return result;
        }

        /// <summary>
        /// 检查输入的bytes是否与生成的错误校验码一致
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        public bool Check(IList<byte> data)
        {
            BitArray result = GenerateErrorCorrector(data);
            return this.VerificationCode.Count == result.Count && this.VerificationCode.Xor(result).OfType<bool>().All(e => !e);
        }

        private static byte[] BoolListToByteArray(IList<bool> boolList)
        {
            int boolListLength = boolList.Count;
            byte[] byteArray = new byte[boolListLength / 8 + (boolListLength % 8 > 0 ? 1 : 0)];

            for (int i = 0; i < boolListLength; i++)
            {
                int boolIndex = i % 8;
                int byteIndex = i / 8;

                if (boolList[i])
                {
                    byteArray[byteIndex] |= (byte)(1 << boolIndex);
                }
            }

            return byteArray;
        }

        public byte[] Repair(IEnumerable<byte> rawdata)
        {
            BitArray rawBitArray = new BitArray(rawdata.ToArray());
            byte[] bytes = new byte[rawdata.Count()];
            Repair(rawBitArray).CopyTo(bytes, 0);
            return bytes;
        }

        public BitArray Repair(BitArray rawdata)
        {
            var rawFileVerification = GenerateErrorCorrector(rawdata);
            var result = rawFileVerification.Xor(this.VerificationCode);
            int index = SumFalseIndices(result.OfType<bool>().ToArray());
            BitArray fixedData = (BitArray)rawdata.Clone();
            Console.WriteLine(index);
            fixedData[index] ^= true;
            return fixedData;
        }

        public static int SumFalseIndices(IList<bool> boolArray)
        {
            int sum = 0;
            for (int i = 0; i < boolArray.Count; i++)
            {
                if (!boolArray[i])
                {
                    sum += (int)Math.Pow(2, i);
                }
            }
            return sum;
        }
        /*
        public static List<bool> ByteArrayToBoolList(IList<byte> byteArray)
        {
            List<bool> boolList = new List<bool>();

            foreach (byte b in byteArray)
            {
                boolList.Add((b & 1) != 0);
            }

            return boolList;
        }

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
