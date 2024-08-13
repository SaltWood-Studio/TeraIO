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

        public static void Merge<T>(this ICollection<T> left, IEnumerable<T> right)
        {
            foreach (T item in right)
            {
                left.Add(item);
            }
        }

        public static void ForEach<T>(this IEnumerable<T> values, Action<T> action)
        {
            foreach (T item in values)
            {
                action(item);
            }
        }
    }
}
