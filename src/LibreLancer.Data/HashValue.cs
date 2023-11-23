using System;
using LibreLancer.Ini;

namespace LibreLancer.Data;

public struct HashValue : IEquatable<HashValue>
{
    public uint Hash => h;
    public string String => s;

    private uint h;
    private string s;

    public HashValue(IValue v)
    {
        if (v.TryToInt32(out int hash))
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

    public override string ToString()
    {
        if (s != null) return $"{s} ({h})";
        return h.ToString();
    }

    public static implicit operator HashValue(uint u)
    {
        return new HashValue(u);
    }

    public static implicit operator HashValue(int i)
    {
        return new HashValue(i);
    }

    public static implicit operator HashValue(string s) => new HashValue(s);

    public static implicit operator uint(HashValue sh)
    {
        return sh.h;
    }

    public static explicit operator int(HashValue sh)
    {
        return unchecked((int)sh.h);
    }

    public override bool Equals(object obj)
    {
        if (!(obj is HashValue hash)) return false;
        return h == hash.h;
    }

    public bool Equals(HashValue other)
    {
        return h == other.h;
    }

    public static bool operator ==(HashValue left, HashValue right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(HashValue left, HashValue right)
    {
        return !left.Equals(right);
    }

    public override int GetHashCode()
    {
        return unchecked((int) h);
    }
}
