using System;
using System.Buffers;
using System.Numerics;
using System.Runtime.InteropServices;

namespace LibreLancer.Net.Protocol;

public class SPUpdatePacket : IPacket
{
    public required uint InputSequence;
    public required PlayerAuthState PlayerState;
    public required uint Tick;
    public required ObjectUpdate[] Updates;

    public void WriteContents(PacketWriter outPacket)
    {
        throw new InvalidOperationException("Cannot send SPUpdate by network");
    }
}

public class PackedUpdatePacket : IPacket
{
    public uint InputSequence;
    public uint OldTick;
    public uint Tick;
    public byte[]? AuthState;
    public byte[]? Updates;

    public int DataSize =>
        1 + // Packet Kind
        NetPacking.ByteCountUInt64(Tick) + // Header
        NetPacking.ByteCountInt64((int) ((long) OldTick - Tick)) +
        NetPacking.ByteCountInt64(((int) ((long) InputSequence - Tick))) +
        (AuthState?.Length ?? 0) + // Auth State serialized
        (Updates?.Length ?? 0); // Updates serialized

    public void WriteContents(PacketWriter outPacket)
    {
        outPacket.PutVariableUInt32(Tick);
        outPacket.PutVariableInt32((int) ((long) OldTick - Tick));
        outPacket.PutVariableInt32((int) ((long) InputSequence - Tick));
        outPacket.Put(AuthState!, 0, AuthState!.Length);
        outPacket.Put(Updates!, 0, Updates!.Length);
    }

    public static object Read(PacketReader message)
    {
        var p = new PackedUpdatePacket
        {
            Tick = message.GetVariableUInt32()
        };
        p.OldTick = (uint) (p.Tick + message.GetVariableInt32());
        p.InputSequence = (uint) (p.Tick + message.GetVariableInt32());
        p.Updates = message.GetRemainingBytes();
        return p;
    }

    public (PlayerAuthState AuthState, ObjectUpdate[] Updates) GetUpdates(PlayerAuthState origAuth,
        Func<uint, int, ObjectUpdate> getSource)
    {
        var reader = new BitReader(Updates, 0);
        var pa = PlayerAuthState.Read(ref reader, origAuth);
        reader.Align();
        var count = reader.GetByte();
        int[] ids = new int[count];

        if (count > 0)
        {
            ids[0] = reader.GetVarInt32();
        }

        for (int i = 1; i < count; i++)
        {
            ids[i] = ids[i - 1] + reader.GetVarInt32();
        }

        reader.Align();

        var rle = new NetRleReader(Updates, reader.Position >> 3);

        var updates = new ObjectUpdate[count];
        for (int i = 0; i < count; i++)
        {
            updates[i] = ObjectUpdate.ReadDelta(rle, Tick, ids[i], getSource);
            reader.Align();
        }

        return (pa, updates);
    }

    public void SetAuthState(PlayerAuthState newAuth, PlayerAuthState origAuth, uint tick)
    {
        var writer = new BitWriter();
        newAuth.Write(ref writer, origAuth, tick);
        writer.Align();
        AuthState = writer.GetCopy();
    }
}

public struct GunOrient
{
    public ushort Pitch16;
    public ushort Rot16;

    public float AnglePitch
    {
        get => NetPacking.UnquantizeFloat(Pitch16, NetPacking.ANGLE_MIN, NetPacking.ANGLE_MAX, 16);
        set => Pitch16 = (ushort)NetPacking.QuantizeAngle(value, 16);
    }

    public float AngleRot
    {
        get => NetPacking.UnquantizeFloat(Rot16, NetPacking.ANGLE_MIN, NetPacking.ANGLE_MAX, 16);
        set => Rot16 = (ushort)NetPacking.QuantizeAngle(value, 16);
    }

    public GunOrient(ushort pitch, ushort rot)
    {
        Pitch16 = pitch;
        Rot16 = rot;
    }
}

public enum CruiseThrustState
{
    None = 0,
    Cruising = 1,
    CruiseCharging = 2,
    Thrusting = 3
}

public struct UpdateQuaternion : IEquatable<UpdateQuaternion>
{
    public uint Largest;
    public uint Component1;
    public uint Component2;
    public uint Component3;

    public static implicit operator UpdateQuaternion(Quaternion q)
    {
        var uq = new UpdateQuaternion();
        NetPacking.PackQuaternion(q, 10, out uq.Largest, out uq.Component1, out uq.Component2, out uq.Component3);
        return uq;
    }

    public Quaternion Quaternion => NetPacking.UnpackQuaternion(10, Largest, Component1, Component2, Component3);

    public bool Equals(UpdateQuaternion other)
    {
        return Largest == other.Largest && Component1 == other.Component1 && Component2 == other.Component2 && Component3 == other.Component3;
    }

    public override bool Equals(object? obj)
    {
        return obj is UpdateQuaternion other && Equals(other);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(Largest, Component1, Component2, Component3);
    }

    public static bool operator ==(UpdateQuaternion left, UpdateQuaternion right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(UpdateQuaternion left, UpdateQuaternion right)
    {
        return !left.Equals(right);
    }
}


[StructLayout(LayoutKind.Sequential)]
public struct Fix22d10 : IEquatable<Fix22d10>
{
    public int Value;

    public Fix22d10(float value)
    {
        Value = (int)MathHelper.Clamp((double)value * 1024.0, int.MinValue, int.MaxValue);
    }

    public float ToFloat()
    {
        return (float)((double)Value / 1024.0);
    }

    public bool Equals(Fix22d10 other)
    {
        return Value == other.Value;
    }

    public override bool Equals(object? obj)
    {
        return obj is Fix22d10 other && Equals(other);
    }

    public override int GetHashCode()
    {
        return Value;
    }

    public static bool operator ==(Fix22d10 left, Fix22d10 right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(Fix22d10 left, Fix22d10 right)
    {
        return !left.Equals(right);
    }
}


[StructLayout(LayoutKind.Sequential)]
public struct Vec3Fix22d10 : IEquatable<Vec3Fix22d10>
{
    public Fix22d10 X;
    public Fix22d10 Y;
    public Fix22d10 Z;

    public Vec3Fix22d10(Vector3 v)
    {
        X = new(v.X);
        Y = new(v.Y);
        Z = new(v.Z);
    }

    public Vector3 ToVector3() => new(X.ToFloat(), Y.ToFloat(), Z.ToFloat());

    public static Vec3Fix22d10 operator +(Vec3Fix22d10 v1, Vec3Fix22d10 v2) => new()
    {
        X = new Fix22d10() { Value = v1.X.Value + v2.X.Value },
        Y = new Fix22d10() { Value = v1.Y.Value + v2.Y.Value },
        Z = new Fix22d10() { Value = v1.Z.Value + v2.Z.Value }
    };

    public static Vec3Fix22d10 operator -(Vec3Fix22d10 v1, Vec3Fix22d10 v2) => new()
    {
        X = new Fix22d10() { Value = v1.X.Value - v2.X.Value },
        Y = new Fix22d10() { Value = v1.Y.Value - v2.Y.Value },
        Z = new Fix22d10() { Value = v1.Z.Value - v2.Z.Value }
    };

    public static bool operator ==(Vec3Fix22d10 left, Vec3Fix22d10 right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(Vec3Fix22d10 left, Vec3Fix22d10 right)
    {
        return !left.Equals(right);
    }

    public bool Equals(Vec3Fix22d10 other)
    {
        return X.Equals(other.X) && Y.Equals(other.Y) && Z.Equals(other.Z);
    }

    public override bool Equals(object? obj)
    {
        return obj is Vec3Fix22d10 other && Equals(other);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(X, Y, Z);
    }
}

[StructLayout(LayoutKind.Explicit)]
unsafe struct ZigZagVecDelta
{
    [FieldOffset(0)]
    public fixed byte Data[12];
    [FieldOffset(0)]
    public uint X;
    [FieldOffset(4)]
    public uint Y;
    [FieldOffset(8)]
    public uint Z;

    public ZigZagVecDelta(Vec3Fix22d10 v)
    {
        X = NetPacking.Zig32(v.X.Value);
        Y = NetPacking.Zig32(v.Y.Value);
        Z = NetPacking.Zig32(v.Z.Value);
    }

    public Vec3Fix22d10 Zag() => new()
    {
        X = new() { Value = NetPacking.Zag32(X) },
        Y = new() { Value = NetPacking.Zag32(Y) },
        Z = new() { Value = NetPacking.Zag32(Z) }
    };
}

public class ObjectUpdate
{
    public static readonly ObjectUpdate Blank = new();

    public ObjNetId ID;
    public Vec3Fix22d10 Position;
    public Vec3Fix22d10 LinearVelocity;
    public Vec3Fix22d10 AngularVelocity;
    public UpdateQuaternion Orientation = Quaternion.Identity;
    public int Hull;
    public int Shield;
    public byte Throttle;

    public float ThrottleFloat
    {
        get => ((sbyte)Throttle) * 127f;
        set => Throttle = (byte)(value / 127.0f);
    }

    public byte Flags;
    public GunOrient[] Guns = [];

    public CruiseThrustState CruiseThrust
    {
        get => (CruiseThrustState)(Flags & 0x3);
        set => Flags = (byte)((Flags & 0xC) | (byte)value);
    }

    public bool EngineKill
    {
        get => MathHelper.GetFlag(Flags, 2);
        set => MathHelper.SetFlag(ref Flags, 2, value);
    }

    public bool Tradelane
    {
        get => MathHelper.GetFlag(Flags, 3);
        set => MathHelper.SetFlag(ref Flags, 3, value);
    }

    // Read+write transposed bytes to arrange 0s next to each-other
    // In order for our compression to work, the compressor
    // needs to see the 0s grouped together as close as possible.

    // We zigzag encode all the ints so that the sign bit goes
    // into the low byte. This gives us longer runs of 0 for the
    // high bytes.


    public unsafe void WriteDelta(ObjectUpdate src, uint oldTick, uint newTick, NetRleWriter msg)
    {
        var ol = msg.Length;
        if (oldTick == 0)
        {
            msg.Write(255);
        }
        else if (oldTick == newTick)
        {
            throw new ArgumentException("old tick == new tick");
        }
        else if ((newTick - oldTick) > 254 || oldTick > newTick)
        {
            throw new ArgumentException("old tick must be < newTick and up to 254 ticks away");
        }
        else
        {
            msg.Write((byte) (newTick - oldTick));
        }

        msg.Write((byte)(Guns.Length - src.Guns.Length));
        ZigZagVecDelta posDelta = new(Position - src.Position);
        ZigZagVecDelta avDelta = new(AngularVelocity - src.AngularVelocity);
        ZigZagVecDelta lvDelta = new(LinearVelocity - src.LinearVelocity);


        msg.Write(posDelta.Data[0]);  //X0
        msg.Write(avDelta.Data[0]);
        msg.Write(lvDelta.Data[0]);

        msg.Write(posDelta.Data[4]); //Y0
        msg.Write(avDelta.Data[4]);
        msg.Write(lvDelta.Data[4]);

        msg.Write(posDelta.Data[8]); //Z0
        msg.Write(avDelta.Data[8]);
        msg.Write(lvDelta.Data[8]);

        msg.Write(posDelta.Data[1]); //X1
        msg.Write(avDelta.Data[1]);
        msg.Write(lvDelta.Data[1]);

        msg.Write(posDelta.Data[5]); //Y1
        msg.Write(avDelta.Data[5]);
        msg.Write(lvDelta.Data[5]);

        msg.Write(posDelta.Data[9]); //Z1
        msg.Write(avDelta.Data[9]);
        msg.Write(lvDelta.Data[9]);

        msg.Write(posDelta.Data[2]); //X2
        msg.Write(avDelta.Data[2]);
        msg.Write(lvDelta.Data[2]);

        msg.Write(posDelta.Data[6]); //Y2
        msg.Write(avDelta.Data[6]);
        msg.Write(lvDelta.Data[6]);

        msg.Write(posDelta.Data[10]); //Z2
        msg.Write(avDelta.Data[10]);
        msg.Write(lvDelta.Data[10]);

        msg.Write(posDelta.Data[3]); //X3
        msg.Write(avDelta.Data[3]);
        msg.Write(lvDelta.Data[3]);

        msg.Write(posDelta.Data[7]); //Y3
        msg.Write(avDelta.Data[7]);
        msg.Write(lvDelta.Data[7]);

        msg.Write(posDelta.Data[11]); //Z3
        msg.Write(avDelta.Data[11]);
        msg.Write(lvDelta.Data[11]);

        msg.Write((byte)(Orientation.Largest - src.Orientation.Largest));

        long dXs = (long)Orientation.Component1 - src.Orientation.Component1;
        long dYs = (long)Orientation.Component2 - src.Orientation.Component2;
        long dZs = (long)Orientation.Component3 - src.Orientation.Component3;
        var dX = NetPacking.Zig64(dXs);
        var dY = NetPacking.Zig64(dYs);
        var dZ = NetPacking.Zig64(dZs);

        msg.Write((byte)((dX >> 8) & 0xFF));
        msg.Write((byte)((dY >> 8) & 0xFF));
        msg.Write((byte)((dZ >> 8) & 0xFF));
        msg.Write((byte)(dX & 0xFF));
        msg.Write((byte)(dY & 0xFF));
        msg.Write((byte)(dZ & 0xFF));

        msg.Write((byte)(Flags - src.Flags));
        msg.Write((byte)(Throttle - src.Throttle));

        var dHull = NetPacking.Zig32(Hull - src.Hull);
        var dShield = NetPacking.Zig32(Shield - src.Shield);

        msg.Write0(dHull);
        msg.Write0(dShield);
        msg.Write1(dHull);
        msg.Write1(dShield);
        msg.Write2(dHull);
        msg.Write2(dShield);
        msg.Write3(dHull);
        msg.Write3(dShield);

        if (Guns is { Length: > 0 })
        {
            Span<ushort> diffP = stackalloc ushort[Guns.Length];
            Span<ushort> diffR = stackalloc ushort[Guns.Length];
            for (int i = 0; i < Guns.Length; i++)
            {
                var o = src.Guns != null && src.Guns.Length > i
                    ? src.Guns[i]
                    : new() { AnglePitch = 0, AngleRot = 0 };
                diffP[i] = (ushort)NetPacking.Zig32(Guns[i].Pitch16 - o.Pitch16);
                diffR[i] = (ushort)NetPacking.Zig32(Guns[i].Rot16 - o.Rot16);
            }
            for (int i = 0; i < diffP.Length; i++)
            {
                msg.Write((byte)((diffP[i] >> 8) & 0xFF));
                msg.Write((byte)((diffR[i] >> 8) & 0xFF));
            }
            for (int i = 0; i < diffP.Length; i++)
            {
                msg.Write((byte)(diffP[i] & 0xFF));
                msg.Write((byte)(diffR[i] & 0xFF));
            }
        }
    }

    public static unsafe ObjectUpdate ReadDelta(NetRleReader msg, uint mainTick, int id,
        Func<uint, int, ObjectUpdate> getSource)
    {
        var od = new ObjectUpdate() { ID = new(id) };
        var b = msg.ReadByte();

        ObjectUpdate src = b == 255 ? Blank : getSource(mainTick - b, id);

        var gunCount = (byte)(src.Guns.Length + msg.ReadByte());

        ZigZagVecDelta posDelta = new();
        ZigZagVecDelta avDelta = new();
        ZigZagVecDelta lvDelta = new();

        // Transposed bytes to arrange 0s next to each-other
        posDelta.Data[0] = msg.ReadByte();
        avDelta.Data[0] = msg.ReadByte();
        lvDelta.Data[0] = msg.ReadByte();

        posDelta.Data[4] = msg.ReadByte();
        avDelta.Data[4] = msg.ReadByte();
        lvDelta.Data[4] = msg.ReadByte();

        posDelta.Data[8] = msg.ReadByte();
        avDelta.Data[8] = msg.ReadByte();
        lvDelta.Data[8] = msg.ReadByte();

        posDelta.Data[1] = msg.ReadByte();
        avDelta.Data[1] = msg.ReadByte();
        lvDelta.Data[1] = msg.ReadByte();

        posDelta.Data[5] = msg.ReadByte();
        avDelta.Data[5] = msg.ReadByte();
        lvDelta.Data[5] = msg.ReadByte();

        posDelta.Data[9] = msg.ReadByte();
        avDelta.Data[9] = msg.ReadByte();
        lvDelta.Data[9] = msg.ReadByte();

        posDelta.Data[2] = msg.ReadByte();
        avDelta.Data[2] = msg.ReadByte();
        lvDelta.Data[2] = msg.ReadByte();

        posDelta.Data[6] = msg.ReadByte();
        avDelta.Data[6] = msg.ReadByte();
        lvDelta.Data[6] = msg.ReadByte();

        posDelta.Data[10] = msg.ReadByte();
        avDelta.Data[10] = msg.ReadByte();
        lvDelta.Data[10] = msg.ReadByte();

        posDelta.Data[3] = msg.ReadByte();
        avDelta.Data[3] = msg.ReadByte();
        lvDelta.Data[3] = msg.ReadByte();

        posDelta.Data[7] = msg.ReadByte();
        avDelta.Data[7] = msg.ReadByte();
        lvDelta.Data[7] = msg.ReadByte();

        posDelta.Data[11] = msg.ReadByte();
        avDelta.Data[11] = msg.ReadByte();
        lvDelta.Data[11] = msg.ReadByte();

        var lg = (byte)(src.Orientation.Largest + msg.ReadByte());

        var dXh = msg.ReadByte();
        var dYh = msg.ReadByte();
        var dZh = msg.ReadByte();
        var dXl = msg.ReadByte();
        var dYl = msg.ReadByte();
        var dZl = msg.ReadByte();

        var dX = NetPacking.Zag64((ulong)((dXh << 8) | dXl));
        var dY = NetPacking.Zag64((ulong)((dYh << 8) | dYl));
        var dZ = NetPacking.Zag64((ulong)((dZh << 8) | dZl));

        od.Position = src.Position + posDelta.Zag();
        od.AngularVelocity = src.AngularVelocity + avDelta.Zag();
        od.LinearVelocity = src.LinearVelocity + lvDelta.Zag();

        od.Orientation = new()
        {
            Largest = lg,
            Component1 = (uint)(src.Orientation.Component1 + dX),
            Component2 = (uint)(src.Orientation.Component2 + dY),
            Component3 = (uint)(src.Orientation.Component3 + dZ)
        };

        od.Flags = (byte)(src.Flags + msg.ReadByte());
        od.Throttle = (byte)(src.Throttle + msg.ReadByte());

        uint dHull = 0;
        uint dShield = 0;
        msg.Read0(ref dHull);
        msg.Read0(ref dShield);
        msg.Read1(ref dHull);
        msg.Read1(ref dShield);
        msg.Read2(ref dHull);
        msg.Read2(ref dShield);
        msg.Read3(ref dHull);
        msg.Read3(ref dShield);

        od.Hull = src.Hull + NetPacking.Zag32(dHull);
        od.Shield = src.Shield + NetPacking.Zag32(dShield);

        Span<ushort> dPitch = stackalloc ushort[gunCount];
        Span<ushort> dRoll = stackalloc ushort[gunCount];
        od.Guns = new GunOrient[gunCount];

        for (int i = 0; i < gunCount; i++)
        {
            dPitch[i] = (ushort)(msg.ReadByte() << 8);
            dRoll[i] = (ushort)(msg.ReadByte() << 8);
        }

        for (int i = 0; i < gunCount; i++)
        {
            dPitch[i] |= msg.ReadByte();
            dRoll[i] |= msg.ReadByte();
        }


        for (int i = 0; i < od.Guns.Length; i++)
        {
            var s = i < src.Guns.Length ? src.Guns[i] : new() { AnglePitch = 0, AngleRot = 0};
            var p = (ushort)(s.Pitch16 + NetPacking.Zag64(dPitch[i]));
            var r = (ushort)(s.Rot16 + NetPacking.Zag64(dRoll[i]));
            od.Guns[i] = new(p, r);
        }

        return od;
    }
}
