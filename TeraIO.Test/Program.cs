using System.Text;
using TeraIO;
using TeraIO.Data;

namespace TeraIO.Test
{
    internal class Program
    {
        static void Main(string[] args)
        {
            byte[] data = File.ReadAllBytes("P:/NikGapps-tiny-arm64-11-20220324-signed.zip");
            List<bool> bools = TeraHash.ByteArrayToBoolList(data);
            HammingCode hammingCode = new HammingCode(bools);
            string i = ToString(hammingCode.hammingCode);
            Console.WriteLine(i);
            Console.WriteLine(hammingCode);
        }

        public static string ToString(List<bool[,]> args)
        {
            string result = "";
            foreach (bool[,] i in args)
            {
                foreach (bool j in i)
                {
                    result += j ? "1" : "0";
                }
                result += " ";
            }
            return result;
        }

        public static string ToString(List<bool> args)
        {
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < args.Count; i++)
            {
                sb.Append(args[i] ? 1 : 0);
            }
            return sb.ToString();
        }
    }
}