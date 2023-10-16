using System;
using System.Numerics;
using System.Text;
using LibreLancer.World;
using LiteNetLib.Utils;

namespace LibreLancer.Net.Protocol;

public struct PacketReader
{
    private NetDataReader reader;
    private NetHpidReader hpids;

    public NetHpidReader HpidReader => hpids;

    public int Size => reader.RawDataSize;

    public PacketReader(NetDataReader reader, NetHpidReader hpids = null)
    {
        this.reader = reader;
        this.hpids = hpids;
    }

    public bool TryGetDisconnectReason(out DisconnectReason reason)
    {
        reason = DisconnectReason.Unknown;
        if (!reader.TryGetUInt(out var sig) || sig != LNetConst.DISCONNECT_MAGIC)
            return false;
        if (!reader.TryGetByte(out var r) || r >= (byte) DisconnectReason.MaxValue)
            return false;
        reason = (DisconnectReason) r;
        return true;
    }

    public ulong GetVariableUInt64()
    {
        long b = reader.GetByte();
        ulong a = (ulong) (b & 0x7f);
        int extraCount = 0;
        //first extra
        if ((b & 0x80) == 0x80)
        {
            b = reader.GetByte();
            a |= (uint) ((b & 0x7f) << 7);
            extraCount++;
        }

        //second extra
        if ((b & 0x80) == 0x80)
        {
            b = reader.GetByte();
            a |= (uint) ((b & 0x7f) << 14);
            extraCount++;
        }

        //third extra
        if ((b & 0x80) == 0x80)
        {
            b = reader.GetByte();
            a |= (uint) ((b & 0x7f) << 21);
            extraCount++;
        }

        //fourth extra
        if ((b & 0x80) == 0x80)
        {
            b = reader.GetByte();
            a |= (ulong) ((b & 0x7f) << 28);
            extraCount++;
        }

        //fifth extra
        if ((b & 0x80) == 0x80)
        {
            b = reader.GetByte();
            a |= (ulong) ((b & 0x7f) << 35);
            extraCount++;
        }

        //sixth extra
        if ((b & 0x80) == 0x80)
        {
            b = reader.GetByte();
            a |= (ulong) ((b & 0x7f) << 42);
            extraCount++;
        }

        //seventh extra
        if ((b & 0x80) == 0x80)
        {
            b = reader.GetByte();
            a |= (ulong) ((b & 0x7f) << 49);
            extraCount++;
        }

        //Full ulong
        if ((b & 0x80) == 0x80)
        {
            b = reader.GetByte();
            a |= (ulong) (((ulong) b) << 57);
            extraCount++;
        }

        switch (extraCount)
        {
            case 1: a += 128; break;
            case 2: a += 16512; break;
            case 3: a += 2113663; break;
            case 4: a += 270549119; break;
            case 5: a += 34630197487; break;
            case 6: a += 4432676708591; break;
            case 7: a += 567382630129903; break;
            case 8: a += 72624976668057839; break;
        }
        return a;
    }

    public ObjectName GetObjectName()
    {
        var c = GetVariableUInt32();
        if (c == 0) return null;
        if (c == 1)
        {
            return new ObjectName(GetString());
        }
        else
        {
            var ids = new int[c - 2];
            for (int i = 0; i < ids.Length; i++)
                ids[i] = GetVariableInt32();
            return new ObjectName(ids);
        }
    }

    public byte[] GetBytes(int count)
    {
        var buf = new byte[count];
        reader.GetBytes(buf, count);
        return buf;
    }

    public Quaternion GetQuaternion()
    {
        var buf = new byte[4];
        reader.GetBytes(buf, 4);
        var pack = new BitReader(buf, 0);
        return pack.GetQuaternion();
    }


    public Vector3 GetNormal()
    {
        var buf = new byte[4];
        reader.GetBytes(buf, 4);
        var pack = new BitReader(buf, 0);
        return pack.GetNormal();
    }

    public Vector3 GetVector3()
    {
        return new Vector3(reader.GetFloat(), reader.GetFloat(), reader.GetFloat());
    }

    public uint GetVariableUInt32()
    {
        return (uint) GetVariableUInt64();
    }

    public long GetVariableInt64()
    {
        return NetPacking.Zag64(GetVariableUInt64());
    }

    public int GetVariableInt32() => (int) GetVariableInt64();

    public unsafe Guid GetGuid()
    {
        Guid g = new Guid();
        var longs = (ulong*) &g;
        longs[0] = reader.GetULong();
        longs[1] = reader.GetULong();
        return g;
    }


    bool TryPeekByte(ref int o, out byte v)
    {
        v = 0;
        if (reader.AvailableBytes < o) return false;
        v = reader.RawData[reader.Position + o++];
        return true;
    }

    public bool TryPeekVariableUInt32(ref int offset, out uint len)
    {
        len = 0;
        uint a = 0;
        if (!TryPeekByte(ref offset, out byte b)) return false;
        a = (uint) (b & 0x7f);
        int extraCount = 0;
        //first extra
        if ((b & 0x80) == 0x80)
        {
            if (!TryPeekByte(ref offset, out b)) return false;
            a |= (uint) ((b & 0x7f) << 7);
            extraCount++;
        }
        //second extra
        if ((b & 0x80) == 0x80)
        {
            if (!TryPeekByte(ref offset, out b)) return false;
            a |= (uint) ((b & 0x7f) << 7);
            extraCount++;
        }
        //third extra
        if ((b & 0x80) == 0x80)
        {
            if (!TryPeekByte(ref offset, out b)) return false;
            a |= (uint) ((b & 0x7f) << 7);
            extraCount++;
        }
        //fourth extra
        if ((b & 0x80) == 0x80)
        {
            if (!TryPeekByte(ref offset, out b)) return false;
            a |= (uint) ((b & 0x7f) << 7);
            extraCount++;
        }
        switch (extraCount) {
            case 1: a += 128; break;
            case 2: a += 16512; break;
            case 3: a += 2113663; break;
        }
        len = a;
        return true;
    }

    public bool TryGetString(out string str, uint maxLength = 2048)
    {
        str = null;
        if (reader.AvailableBytes < 1) return false;
        var firstByte = reader.PeekByte();
        if (firstByte == 0) { reader.SkipBytes(1); return true; }
        if (firstByte == 1) { reader.SkipBytes(1); str = ""; return true; }
        var type = (firstByte >> 6);
        uint len;
        if (type == 0 || type == 2)
        {
            len = (uint) ((firstByte & 0x3f) - 1);
            if (len > maxLength) return false;
            if (reader.AvailableBytes < len + 1) return false;
            reader.SkipBytes(1);
        }
        else
        {
            if (reader.AvailableBytes < 64) return false; //63 + reader.GetByte()
            int off = 1;
            if (!TryPeekVariableUInt32(ref off, out len)) return false;
            len += 63;
            if (len > maxLength) return false;
            if (reader.AvailableBytes < off + len) return false;
            reader.SkipBytes(off);
        }
        var bytes = GetBytes((int)len);
        if (type == 0 || type == 1)
            str = NetPacking.DecodeString(bytes);
        else
            str = Encoding.UTF8.GetString(bytes);
        return true;
    }

    public string GetString()
    {
        var firstByte = reader.GetByte();
        if (firstByte == 0) return null;
        if (firstByte == 1) return "";
        var type = (firstByte >> 6);
        int len;
        if (type == 0 || type == 2)
            len = (firstByte & 0x3f) - 1;
        else
            len = (int)GetVariableUInt32() + 63;
        var bytes = GetBytes(len);
        if (type == 0 || type == 1)
            return NetPacking.DecodeString(bytes);
        else
            return Encoding.UTF8.GetString(bytes);
    }

    public string GetHpid()
    {
        if (hpids == null) throw new InvalidOperationException();
        var idx = GetVariableUInt32();
        if (idx == 0) return null;
        else if (idx == 1) return "";
        else return hpids.GetString(idx - 2);
    }

    public bool GetBool() => reader.GetBool();
    public float GetFloat() => reader.GetFloat();
    public int GetInt() => reader.GetInt();
    public uint GetUInt() => reader.GetUInt();
    public byte GetByte() => reader.GetByte();
    public short GetShort() => reader.GetShort();

    public bool TryGetULong(out ulong result) => reader.TryGetULong(out result);

    public bool TryGetUInt(out uint result) => reader.TryGetUInt(out result);

    public byte[] GetRemainingBytes() => reader.GetRemainingBytes();
}
