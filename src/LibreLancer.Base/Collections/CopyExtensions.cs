using System;
using System.Collections.Generic;
using System.Linq;

namespace LibreLancer;

public static class CopyExtensions
{
    public static T[]? ShallowCopy<T>(this T[]? source) => source?.Length == 0 ? [] : (T[]?)source?.Clone();

    public static T[]? CloneCopy<T>(this T[]? source) where T : ICloneable
    {
        if (source == null)
        {
            return null;
        }

        if (source.Length == 0)
        {
            return [];
        }

        var newArray = new T[source.Length];
        for (var i = 0; i < source.Length; i++)
            newArray[i] = (T)source[i].Clone();

        return newArray;
    }

    public static List<T>? ShallowCopy<T>(this List<T>? source) => source?.Count == 0 ? [] : source?.ToList();
    public static List<T>? CloneCopy<T>(this List<T>? source) where T : ICloneable => source?.Count == 0 ? [] : source?.Select(x => (T) x.Clone()).ToList();
}
