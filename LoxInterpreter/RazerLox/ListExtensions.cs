using System.Collections.Generic;

namespace LoxInterpreter.RazerLox
{
    /// <summary>
    /// Some extensions to give a List a Stack-like interface.
    /// </summary>
    internal static class ListExtensions
    {
        public static T First<T>(this List<T> l)
            => l[0];

        public static T Last<T>(this List<T> l)
            => l[l.Count - 1];

        public static T Pop<T>(this List<T> l)
        {
            int index = l.Count - 1;
            T item = l[index];
            l.RemoveAt(index);
            return item;
        }

        public static void Push<T>(this List<T> list, T item)
            => list.Add(item);

        public static bool IsEmpty<T>(this IList<T> l)
            => l.Count == 0;
    }
}
