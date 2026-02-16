using System;
using LibreLancer.Data.Ini;

namespace LibreLancer.Data;

public struct HashValue : IEquatable<HashValue>
{
    public uint Hash => h;
    public string? String => s;

    private uint h;
    private string? s;

    public HashValue(ValueBase v)
    {
        if (v.TryToInt32(out var hash))
        {
            h = (uint)hash;
            s = null;
        }
        else
        {
            s = v.ToString();
            h = FLHash.CreateID(s);
        }
    }


    public HashValue(string s)
    {
        this.s = s;
        this.h = FLHash.CreateID(s);
    }

    public HashValue(uint h)
    {
        this.s = null;
        this.h = h;
    }

    public HashValue(int h)
    {
        this.s = null;
        this.h = (uint)h;
    }

    public override string ToString() => s != null ? $"{s} ({h})" : h.ToString();
    public static implicit operator HashValue(uint u) => new(u);
    public static implicit operator HashValue(int i) => new(i);
    public static implicit operator HashValue(string s) => new(s);
    public static implicit operator uint(HashValue sh) => sh.h;
    public static explicit operator int(HashValue sh) => unchecked((int)sh.h);
    public override bool Equals(object? obj) => obj is HashValue hash && h == hash.h;
    public bool Equals(HashValue other) => h == other.h;
    public static bool operator ==(HashValue left, HashValue right) => left.Equals(right);
    public static bool operator !=(HashValue left, HashValue right) => !left.Equals(right);
    public override int GetHashCode() => unchecked((int) h);
}
