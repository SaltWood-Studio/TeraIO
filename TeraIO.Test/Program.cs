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
            List<bool> bools = TeraHash.ByteArrayToBoolList(data);
            ErrorCorrector verificator = new ErrorCorrector(bools);
            string i = ToString(verificator.verificationCode);
            Console.WriteLine(i);
            Console.WriteLine(verificator);
            Console.WriteLine(verificator.Check(bools));
        }

        public static string ToString(IList<bool> args)
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