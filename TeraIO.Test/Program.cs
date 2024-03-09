using System.Collections;
using System.Text;
using TeraIO;
using TeraIO.Data;
using TeraIO.Extension;
using TeraIO.Network.Http;
using TeraIO.Runnable;

namespace TeraIO.Test
{
    internal class Program
    {
        static void Main(string[] args)
        {
            string path = "C:/appverifUI.dll";
            FileInfo file = new FileInfo(path);
            StreamReader sr = new StreamReader(path);
            Stream stream = sr.BaseStream;
            byte[] buffer = new byte[file.Length];
            stream.Read(buffer, 0, (int)file.Length);

            byte[] result = MD5.ComputeMD5(buffer);
            Console.WriteLine(Convert.ToHexString(result));
        }
    }
}