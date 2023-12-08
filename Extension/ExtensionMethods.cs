using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TeraIO.Extension
{
    public static class ExtensionMethods
    {
        public static void Dump(this object a)
        {
            Console.WriteLine(a.ToString());
        }

        public static void Dump(this string a)
        {
            Console.WriteLine(a);
        }

        public static void Dump<T>(this IEnumerable<T> a)
        {
            Console.WriteLine($"[{string.Join(", ", a)}");
        }
    }
}
