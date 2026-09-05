using System;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using LibreLancer.Net.DataTypes;
using LibreLancer.World.Components;

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
        1 + (AuthState?.Length ?? 0) + // Auth State serialized
        (Updates?.Length ?? 0); // Updates serialized

    public void WriteContents(PacketWriter outPacket)
    {
        outPacket.PutVariableUInt32(Tick);
        outPacket.PutVariableInt32((int) ((long) OldTick - Tick));
        outPacket.PutVariableInt32((int) ((long) InputSequence - Tick));
        outPacket.Put((byte)(AuthState?.Length ?? 0));
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
        p.AuthState = message.GetBytes(message.GetByte());
        p.Updates = message.GetRemainingBytes();
        return p;
    }

    public (PlayerAuthState AuthState, ObjectUpdate[] Updates) GetUpdates(PlayerAuthState origAuth,
        Func<uint, int, ObjectUpdate> getSource)
    {
        var reader = new BitReader(Updates!, 0);
        var pa = PlayerAuthState.Decode(AuthState!, origAuth);
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

        var rle = new NetRleReader(Updates!, reader.Position >> 3);

        var updates = new ObjectUpdate[count];
        for (int i = 0; i < count; i++)
        {
            updates[i] = ObjectUpdate.ReadDelta(rle, Tick, ids[i], getSource);
            reader.Align();
        }

        return (pa, updates);
    }

    public void SetAuthState(PlayerAuthState newAuth, PlayerAuthState origAuth)
    {
        AuthState = newAuth.Encode(origAuth);
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

[StructLayout(LayoutKind.Sequential)]
public record struct PartHealth(uint Hardpoint, byte Health);

public class ObjectUpdate
{
    public static readonly ObjectUpdate Blank = new();

    public ObjNetId ID;
    public Vec3Fix22d10 Position;
    public Vec3Fix22d10 LinearVelocity;
    public Vec3Fix22d10 AngularVelocity;
    public Quaternion32 Orientation = Quaternion.Identity;
    public int Hull;
    public int Shield;
    private byte throttle;

    public float ThrottleFloat
    {
        get => Unsafe.BitCast<byte,sbyte>(throttle) / 127f;
        set => throttle = Unsafe.BitCast<sbyte, byte>((sbyte)(value * 127.0f));
    }

    public byte Flags;
    public GunOrient[] Guns = [];
    public PartHealth[] DamagedParts = [];

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
        if (oldTick == 0)
        {
            msg.Write(0);
        }
        else if (oldTick == newTick)
        {
            throw new ArgumentException("old tick == new tick");
        }
        else if ((newTick - oldTick) > 255 || oldTick > newTick)
        {
            throw new ArgumentException("old tick must be < newTick and up to 255 ticks away");
        }
        else
        {
            // Will always be >= 1
            msg.Write((byte) (newTick - oldTick));
        }

        msg.Write((byte)(Guns.Length - src.Guns.Length));
        msg.Write((byte)(DamagedParts.Length - src.DamagedParts.Length));
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

        msg.Write((byte)NetPacking.ZigZagDelta(Orientation.Largest, src.Orientation.Largest));

        var dX = NetPacking.Delta16(Orientation.Component1, src.Orientation.Component1);
        var dY = NetPacking.Delta16(Orientation.Component2, src.Orientation.Component2);
        var dZ = NetPacking.Delta16(Orientation.Component3, src.Orientation.Component3);

        msg.Write((byte)((dX >> 8) & 0xFF));
        msg.Write((byte)((dY >> 8) & 0xFF));
        msg.Write((byte)((dZ >> 8) & 0xFF));
        msg.Write((byte)(dX & 0xFF));
        msg.Write((byte)(dY & 0xFF));
        msg.Write((byte)(dZ & 0xFF));

        msg.Write((byte)(Flags - src.Flags));
        msg.Write((byte)(throttle - src.throttle));

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
                diffP[i] = NetPacking.Delta16(Guns[i].Pitch16, o.Pitch16);
                diffR[i] = NetPacking.Delta16(Guns[i].Rot16, o.Rot16);
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
        if (DamagedParts is { Length: > 0 })
        {
            Span<byte> diffH = stackalloc byte[DamagedParts.Length];
            for (int i = 0; i < diffH.Length; i++)
            {
                var o = src.DamagedParts.Length > i ? src.DamagedParts[i] : default;
                var diffP = (uint)(DamagedParts[i].Hardpoint - o.Hardpoint);
                msg.Write0(diffP);
                msg.Write1(diffP);
                msg.Write2(diffP);
                msg.Write3(diffP);
                diffH[i] = (byte)(DamagedParts[i].Health - o.Health);
            }
            for (int i = 0; i < diffH.Length; i++)
            {
                msg.Write(diffH[i]);
            }
        }
    }

    public static unsafe ObjectUpdate ReadDelta(NetRleReader msg, uint mainTick, int id,
        Func<uint, int, ObjectUpdate> getSource)
    {
        var od = new ObjectUpdate() { ID = new(id) };
        var b = msg.ReadByte();

        ObjectUpdate src = b == 0 ? Blank : getSource(mainTick - b, id);

        var gunCount = (byte)(src.Guns.Length + msg.ReadByte());
        var dmgCount = (byte)(src.DamagedParts.Length + msg.ReadByte());
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

        var lg = NetPacking.ApplyZigZagDelta(src.Orientation.Largest,  msg.ReadByte());

        var dXh = msg.ReadByte();
        var dYh = msg.ReadByte();
        var dZh = msg.ReadByte();
        var dXl = msg.ReadByte();
        var dYl = msg.ReadByte();
        var dZl = msg.ReadByte();

        var dX = ((ushort)((dXh << 8) | dXl));
        var dY = ((ushort)((dYh << 8) | dYl));
        var dZ = ((ushort)((dZh << 8) | dZl));

        od.Position = src.Position + posDelta.Zag();
        od.AngularVelocity = src.AngularVelocity + avDelta.Zag();
        od.LinearVelocity = src.LinearVelocity + lvDelta.Zag();

        od.Orientation = new()
        {
            Largest = lg,
            Component1 = NetPacking.ApplyDelta16(src.Orientation.Component1, dX),
            Component2 = NetPacking.ApplyDelta16(src.Orientation.Component2, dY),
            Component3 = NetPacking.ApplyDelta16(src.Orientation.Component3, dZ)
        };

        od.Flags = (byte)(src.Flags + msg.ReadByte());
        od.throttle = (byte)(src.throttle + msg.ReadByte());

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
            var p = (ushort)NetPacking.ApplyDelta16(s.Pitch16, dPitch[i]);
            var r = (ushort)NetPacking.ApplyDelta16(s.Rot16, dRoll[i]);
            od.Guns[i] = new(p, r);
        }

        Span<uint> dHp = stackalloc uint[dmgCount];
        for (int i = 0; i < dmgCount; i++)
        {
            dHp[i] = 0;
            msg.Read0(ref dHp[i]);
            msg.Read1(ref dHp[i]);
            msg.Read2(ref dHp[i]);
            msg.Read3(ref dHp[i]);
        }

        od.DamagedParts = new PartHealth[dmgCount];
        for (int i = 0; i < od.DamagedParts.Length; i++)
        {
            var o = i < src.DamagedParts.Length ? src.DamagedParts[i] : default;
            od.DamagedParts[i] = new(
                (dHp[i] + o.Hardpoint),
                (byte)(msg.ReadByte() + o.Health)
            );
        }
        return od;
    }
}
