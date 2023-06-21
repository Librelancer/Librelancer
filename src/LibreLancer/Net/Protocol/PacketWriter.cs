using System;
using System.Diagnostics;
using System.Numerics;
using System.Text;
using LibreLancer.World;
using LiteNetLib.Utils;

namespace LibreLancer.Net.Protocol;

public class PacketWriter
{
    private NetDataWriter writer;
    private NetHpidWriter hpids;

    public NetHpidWriter HpidWriter => hpids;

    public PacketWriter(NetDataWriter writer, NetHpidWriter hpids = null)
    {
        this.writer = writer;
        this.hpids = hpids;
    }

    public PacketWriter()
    {
        writer = new NetDataWriter();
    }
    
    public static implicit operator NetDataWriter(PacketWriter pw)
    {
        return pw.writer;
    }

    public void Put(ObjectName on)
    {
        if (on == null)
        {
            PutVariableUInt32(0);
        }
        else
        {
            if (on._Ids != null)
            {
                PutVariableUInt32((uint)on._Ids.Length + 2);
                foreach(var i in on._Ids)
                    PutVariableInt32(i);
            }
            else
            {
                PutVariableUInt32(1);
                Put(on._NameString);
            }
        }
    }
    
    public void Put(DisconnectReason reason)
    {
        writer.Put(LNetConst.DISCONNECT_MAGIC);
        writer.Put((byte)reason);
    }

    public void PutVariableUInt64(ulong u)
    {
        if (u <= 127)
        {
            writer.Put((byte) u);
        }
        else if (u <= 16511)
        {
            u -= 128;
            writer.Put((byte) ((u & 0x7f) | 0x80));
            writer.Put((byte) ((u >> 7) & 0x7f));
        }
        else if (u <= 2113662)
        {
            u -= 16512;
            writer.Put((byte) ((u & 0x7f) | 0x80));
            writer.Put((byte) (((u >> 7) & 0x7f) | 0x80));
            writer.Put((byte) ((u >> 14) & 0x7f));
        }
        else if (u <= 270549118)
        {
            u -= 2113663;
            writer.Put((byte) ((u & 0x7f) | 0x80));
            writer.Put((byte) (((u >> 7) & 0x7f) | 0x80));
            writer.Put((byte) (((u >> 14) & 0x7f) | 0x80));
            writer.Put((byte) ((u >> 21) & 0x7f));
        }
        else if (u <= 34630197486)
        {
            u -= 270549119;
            writer.Put((byte) ((u & 0x7f) | 0x80));
            writer.Put((byte) (((u >> 7) & 0x7f) | 0x80));
            writer.Put((byte) (((u >> 14) & 0x7f) | 0x80));
            writer.Put((byte) (((u >> 21) & 0x7f) | 0x80));
            writer.Put((byte) ((u >> 28) & 0x7f));
        }
        else if (u <= 4432676708590)
        {
            u -= 34630197487;
            writer.Put((byte) ((u & 0x7f) | 0x80));
            writer.Put((byte) (((u >> 7) & 0x7f) | 0x80));
            writer.Put((byte) (((u >> 14) & 0x7f) | 0x80));
            writer.Put((byte) (((u >> 21) & 0x7f) | 0x80));
            writer.Put((byte) (((u >> 28) & 0x7f) | 0x80));
            writer.Put((byte) ((u >> 35) & 0x7f));
        }
        else if (u <= 567382630129902)
        {
            u -= 4432676708591;
            writer.Put((byte) ((u & 0x7f) | 0x80));
            writer.Put((byte) (((u >> 7) & 0x7f) | 0x80));
            writer.Put((byte) (((u >> 14) & 0x7f) | 0x80));
            writer.Put((byte) (((u >> 21) & 0x7f) | 0x80));
            writer.Put((byte) (((u >> 28) & 0x7f) | 0x80));
            writer.Put((byte) (((u >> 35) & 0x7f) | 0x80));
            writer.Put((byte) ((u >> 42) & 0x7f));
        }
        else if (u <= 72624976668057838)
        {
            u -= 567382630129903;
            writer.Put((byte) ((u & 0x7f) | 0x80));
            writer.Put((byte) (((u >> 7) & 0x7f) | 0x80));
            writer.Put((byte) (((u >> 14) & 0x7f) | 0x80));
            writer.Put((byte) (((u >> 21) & 0x7f) | 0x80));
            writer.Put((byte) (((u >> 28) & 0x7f) | 0x80));
            writer.Put((byte) (((u >> 35) & 0x7f) | 0x80));
            writer.Put((byte) (((u >> 42) & 0x7f) | 0x80));
            writer.Put((byte) ((u >> 49) & 0x7f));
        }
        else
        {
            u -= 72624976668057839;
            writer.Put((byte) ((u & 0x7f) | 0x80));
            writer.Put((byte) (((u >> 7) & 0x7f) | 0x80));
            writer.Put((byte) (((u >> 14) & 0x7f) | 0x80));
            writer.Put((byte) (((u >> 21) & 0x7f) | 0x80));
            writer.Put((byte) (((u >> 28) & 0x7f) | 0x80));
            writer.Put((byte) (((u >> 35) & 0x7f) | 0x80));
            writer.Put((byte) (((u >> 42) & 0x7f) | 0x80));
            writer.Put((byte) (((u >> 49) & 0x7f) | 0x80));
            writer.Put((byte) (u >> 57));
        }
    }
    
    public void PutVariableInt64(long value)
    {
        PutVariableUInt64(NetPacking.Zig64(value));
    }

    public void PutVariableInt32(int value)
    {
        PutVariableInt64(value);
    }

    public void PutVariableUInt32(uint u)
    {
        PutVariableUInt64(u);
    }
    
    public unsafe void Put(Guid g)
    {
        var longs = (ulong*)&g;
        writer.Put(longs[0]);
        writer.Put(longs[1]);
    }
    
    public void Put(Quaternion q)
    {
        var pack = new BitWriter(32);
        pack.PutQuaternion(q);
        Debug.Assert(pack.ByteLength == 4);
        pack.WriteToPacket(this);
    }
    
    public void PutNormal(Vector3 n)
    {
        var pack = new BitWriter(32);
        pack.PutNormal(n);
        Debug.Assert(pack.ByteLength == 4);
        pack.WriteToPacket(this);
    }
    
    public void Put(Vector3 vec)
    {
        writer.Put(vec.X);
        writer.Put(vec.Y);
        writer.Put(vec.Z);
    }

    public void PutHpid(string hpid)
    {
        if (hpids == null) throw new InvalidOperationException();
        if(hpid == null) PutVariableUInt32(0);
        else if(hpid == "") PutVariableUInt32(1);
        else PutVariableUInt32(hpids.GetIndex(hpid) + 2);
    }
    
    public void Put(string s)
    {
        if (s == null) {
            writer.Put((byte)0);
        } else if (s == "") {
            writer.Put((byte)1);
        } else {
            if (NetPacking.EncodeString(s, out byte[] encoded)) {
                if (encoded.Length < 63) {
                    writer.Put((byte)(encoded.Length + 1));
                } else {
                    writer.Put((byte)(1 << 6));  
                    PutVariableUInt32((uint)(encoded.Length - 63));
                }
                writer.Put(encoded);
            }
            else
            {
                var bytes = Encoding.UTF8.GetBytes(s);
                if (bytes.Length < 63) {
                    writer.Put((byte)(2 << 6 | bytes.Length + 1));
                } else {
                    writer.Put((byte)(3 << 6));
                    PutVariableUInt32((uint)(bytes.Length - 63));
                }
                writer.Put(bytes);
            }
        }
    }

    public void Put(byte b) => writer.Put(b);
    public void Put(bool b) => writer.Put(b);
    public void Put(float f) => writer.Put(f);

    public void Put(ulong u) => writer.Put(u);
    public void Put(uint u) => writer.Put(u);
    public void Put(int i) => writer.Put(i);
    public void Put(short s) => writer.Put(s);

    public void Put(byte[] data, int offset, int length) => writer.Put(data, offset, length);

    public byte[] GetCopy() => writer.CopyData();
}