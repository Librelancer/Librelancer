using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace LibreLancer.ContentEdit
{
    static class Extensions
    {
        
        public static StringBuilder AppendFmtLine(this StringBuilder builder, object o)
            => builder.AppendLine(o.ToString());

        public static bool AddIfUnique<T>(this List<T> list, T item)
            => AddIfUnique(list, item, out _);
        
        public static bool AddIfUnique<T>(this List<T> list, T item, out int index)
        {
            index = list.IndexOf(item);
            if (index == -1) {
                list.Add(item);
                index = list.Count - 1;
                return true;
            }
            return false;
        }

        public static int IndexOfMin<T>(this IEnumerable<T> self)
            => self.Select((item, index) => (item, index)).Min().index;
        
        public static int IndexOfMax<T>(this IEnumerable<T> self)
            => self.Select((item, index) => (item, index)).Max().index;
        
    }
}