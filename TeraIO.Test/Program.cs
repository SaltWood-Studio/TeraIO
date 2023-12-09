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
            byte[] data = File.ReadAllBytes("");
            ErrorCorrector verificator = new ErrorCorrector(data);
            Console.WriteLine(verificator);
            Console.WriteLine(verificator.Check(data));
            BitArray bitArray = new BitArray(data);
            int position = new Random().Next(0, bitArray.Count);
            position.Dump();
            bitArray[position] ^= true;
            bool[] rawdata = bitArray.OfType<bool>().ToArray();
            var fixedData = verificator.Repair(TeraHash.BoolArrayToByteArray(rawdata));
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