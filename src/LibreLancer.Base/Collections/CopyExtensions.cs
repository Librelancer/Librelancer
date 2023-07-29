using System;
using System.Collections.Generic;
using System.Linq;

namespace LibreLancer;

public static class CopyExtensions
{
    public static T[] ShallowCopy<T>(this T[] source)
    {
        if (source == null) return null;
        if (source.Length == 0) return Array.Empty<T>();
        return (T[])source.Clone();
    }

    public static T[] CloneCopy<T>(this T[] source) where T : ICloneable
    {
        if (source == null) return null;
        if (source.Length == 0) return Array.Empty<T>();
        var newArray = new T[source.Length];
        for (int i = 0; i < source.Length; i++)
            newArray[i] = (T)source[i].Clone();
        return newArray;
    }
    
    public static List<T> ShallowCopy<T>(this List<T> source)
    {
        if (source == null) return null;
        if (source.Count == 0) return new List<T>();
        return source.ToList();
    }
    
    public static List<T> CloneCopy<T>(this List<T> source) where T : ICloneable
    {
        if (source == null) return null;
        if (source.Count == 0) return new List<T>();
        return source.Select(x => (T) x.Clone()).ToList();
    }
}