using System.Collections;
using System.Text;
using TeraIO;
using TeraIO.Data;
using TeraIO.Extension;

namespace TeraIO.Test
{
    internal class Program
    {
        static void Main(string[] args)
        {
            byte[] data = File.ReadAllBytes("P:/WePE64_V2.2.iso.txt");
            ErrorCorrector verificator = new ErrorCorrector(data);
            string i = ToString(verificator.verificationCode);
            Console.WriteLine(i);
            Console.WriteLine(verificator);
            Console.WriteLine(verificator.Check(data));
        }

        public static string ToString(BitArray args)
        {
            string result = "";
            foreach (bool j in args)
            {
                result += j ? "1" : "0";
            }
            result += " ";
            return result;
        }
    }
}