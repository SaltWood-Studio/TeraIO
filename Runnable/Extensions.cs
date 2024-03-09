using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TeraIO.Runnable
{
    public static class Extensions
    {
        public static void ForEach<T>(this IEnumerable<T> it, Action<T, int> action)
        {
            if (it == null) throw new ArgumentNullException(nameof(it));
            if (action == null) throw new ArgumentNullException(nameof(action));
            if (it.Count() == 0) return;

            IEnumerator<T> enumerator = it.GetEnumerator();

            for (int i = 0; i < it.Count(); i++)
            {
                enumerator.MoveNext();
                T item = enumerator.Current;
                action(item, i);
            }
        }

        public static void ForEach<T>(this IEnumerable<T> it, Action<T> action)
        {
            if (it == null) throw new ArgumentNullException(nameof(it));
            if (action == null) throw new ArgumentNullException(nameof(action));
            if (it.Count() == 0) return;

            foreach (var item in it)
            {
                action(item);
            }
        }

    }
}
