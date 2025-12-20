using System;
using System.Collections.Generic;

namespace LibreLancer.Data;

public static class DataEquality
{
    public static bool IdEquals(string a, string b)
    {
        if (a == null && b == null) return true;
        if (a == null) return false;
        if (b == null) return false;
        return a.Equals(b, StringComparison.OrdinalIgnoreCase);
    }

    public static bool ObjectEquals<T>(T a, T b) where T : IDataEquatable<T>
    {
        if(a == null && b == null) return true;
        if(a == null) return false;
        if(b == null) return false;
        return a.DataEquals(b);
    }

    public static bool ListEquals<T>(IList<T> a, IList<T> b) where T : IDataEquatable<T>
    {
        if(a == null && b == null) return true;
        if(a == null) return false;
        if(b == null) return false;
        if (a.Count != b.Count) return false;
        for (int i = 0; i < a.Count; i++)
        {
            if (!a[i].DataEquals(b[i])) return false;
        }
        return true;
    }

}
