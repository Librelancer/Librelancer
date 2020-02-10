using System;
using System.Collections;
using System.Linq;

namespace LancerEdit
{
    static class EnumerableExtensions
    {
        public static T Get<T>(this IEnumerable src, Func<T,bool> comparison)
        {
            return src.OfType<T>().FirstOrDefault(comparison);
        }

        public static T FirstOfType<T>(this IEnumerable src)
        {
            return src.OfType<T>().FirstOrDefault();
        }
    }
}