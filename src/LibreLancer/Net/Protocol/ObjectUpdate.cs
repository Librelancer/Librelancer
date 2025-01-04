using System;
using System.Collections.Generic;
using System.Numerics;

namespace LibreLancer.Net.Protocol;

public class SPUpdatePacket : IPacket
{
    public uint InputSequence;
    public PlayerAuthState PlayerState;
    public uint Tick;
    public ObjectUpdate[] Updates;

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
    public byte[] AuthState;
    public byte[] Updates;

    public int DataSize =>
        1 + //Packet Kind
        NetPacking.ByteCountUInt64(Tick) + //Header
        NetPacking.ByteCountInt64((int)((long)OldTick - Tick)) +
        NetPacking.ByteCountInt64(((int)((long)InputSequence - Tick))) +
        (AuthState?.Length ?? 0) + //Auth State serialized
        (Updates?.Length ?? 0); //Updates serialized


    public void WriteContents(PacketWriter outPacket)
    {
        outPacket.PutVariableUInt32(Tick);
        outPacket.PutVariableInt32((int)((long)OldTick - Tick));
        outPacket.PutVariableInt32((int)((long)InputSequence - Tick));
        outPacket.Put(AuthState, 0, AuthState.Length);
        outPacket.Put(Updates, 0, Updates.Length);
    }

    public static object Read(PacketReader message)
    {
        var p = new PackedUpdatePacket();
        p.Tick = message.GetVariableUInt32();
        p.OldTick = (uint) (p.Tick + message.GetVariableInt32());
        p.InputSequence = (uint) (p.Tick + message.GetVariableInt32());
        p.Updates = message.GetRemainingBytes();
        return p;
    }

    public (PlayerAuthState, ObjectUpdate[]) GetUpdates(PlayerAuthState origAuth, Func<uint, int, ObjectUpdate> getSource)
    {
        var reader = new BitReader(Updates, 0);
        var pa = PlayerAuthState.Read(ref reader, origAuth);
        reader.Align();
        var count = reader.GetVarUInt32();
        int[] ids = new int[count];
        if (count > 0)
        {
            ids[0] = reader.GetVarInt32();
        }
        for (int i = 1; i < count; i++)
        {
            ids[i] = ids[i - 1] + reader.GetVarInt32();
        }
        var updates = new ObjectUpdate[count];
        for (int i = 0; i < count; i++)
        {
            updates[i] = ObjectUpdate.ReadDelta(ref reader, Tick, ids[i], getSource);
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
    private uint pitch;
    private uint rot;
    public float AnglePitch
    {
        get => NetPacking.UnquantizeFloat(pitch, NetPacking.ANGLE_MIN, NetPacking.ANGLE_MAX, 16);
        set => pitch = NetPacking.QuantizeAngle(value, 16);
    }

    public float AngleRot
    {
        get => NetPacking.UnquantizeFloat(rot, NetPacking.ANGLE_MIN, NetPacking.ANGLE_MAX, 16);
        set => rot = NetPacking.QuantizeAngle(value, 16);
    }

    public void ReadDelta(ref BitReader message, GunOrient src)
    {
        if(message.GetBool())
        {
            pitch = message.GetBool()
                ? NetPacking.ApplyDelta(message.GetUInt(8), src.pitch, 8)
                : message.GetUInt(16);
        }
        else
        {
            pitch = src.pitch;
        }
        if (message.GetBool())
        {
            rot = message.GetBool()
                ? NetPacking.ApplyDelta(message.GetUInt(8), src.rot, 8)
                : message.GetUInt(16);
        }
        else
        {
            rot = src.rot;
        }
    }

    public void WriteDelta(GunOrient src, ref BitWriter message)
    {
        if (pitch == src.pitch)
        {
            message.PutBool(false);
        }
        else
        {
            message.PutBool(true);
            if (NetPacking.TryDelta(pitch, src.pitch, 8, out var d)) {
                message.PutBool(true);
                message.PutUInt(d, 8);
            }
            else {
                message.PutBool(false);
                message.PutUInt(pitch, 16);
            }
        }
        if (rot == src.rot)
        {
            message.PutBool(false);
        }
        else
        {
            message.PutBool(true);
            if (NetPacking.TryDelta(rot, src.rot, 8, out var d)) {
                message.PutBool(true);
                message.PutUInt(d, 8);
            }
            else {
                message.PutBool(false);
                message.PutUInt(rot, 16);
            }
        }
    }
}

public enum CruiseThrustState
{
    None = 0,
    Cruising = 1,
    CruiseCharging = 2,
    Thrusting = 3
}

public struct UpdateQuaternion
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

    private const int DELTA_BITS = 7;

    public void WriteDelta(UpdateQuaternion src, ref BitWriter writer)
    {
        if (Largest == src.Largest &&
            NetPacking.TryDelta(Component1, src.Component1, DELTA_BITS, out var deltaA) &&
            NetPacking.TryDelta(Component2, src.Component2, DELTA_BITS, out var deltaB) &&
            NetPacking.TryDelta(Component3, src.Component3, DELTA_BITS, out var deltaC)
           )
        {
            writer.PutBool(true);
            writer.PutUInt(deltaA, DELTA_BITS);
            writer.PutUInt(deltaB, DELTA_BITS);
            writer.PutUInt(deltaC, DELTA_BITS);
        }
        else
        {
            writer.PutBool(false);
            writer.PutUInt(Largest, 2);
            writer.PutUInt(Component1, 10);
            writer.PutUInt(Component2, 10);
            writer.PutUInt(Component3, 10);
        }
    }

    public static UpdateQuaternion ReadDelta(UpdateQuaternion src, ref BitReader reader)
    {
        if (reader.GetBool())
        {
            return new UpdateQuaternion
            {
                Largest = src.Largest,
                Component1 = NetPacking.ApplyDelta(reader.GetUInt(DELTA_BITS), src.Component1, DELTA_BITS),
                Component2 = NetPacking.ApplyDelta(reader.GetUInt(DELTA_BITS), src.Component2, DELTA_BITS),
                Component3 = NetPacking.ApplyDelta(reader.GetUInt(DELTA_BITS), src.Component3, DELTA_BITS),
            };
        }
        else
        {
            return new UpdateQuaternion
            {
                Largest = reader.GetUInt(2),
                Component1 = reader.GetUInt(10),
                Component2 = reader.GetUInt(10),
                Component3 = reader.GetUInt(10)
            };
        }
    }

    public Quaternion Quaternion =>
        NetPacking.UnpackQuaternion(10, Largest, Component1, Component2, Component3);
}

public struct UpdateVector
{
    private readonly int precision;
    private readonly float min;
    private readonly float max;
    private uint x;
    private uint y;
    private uint z;

    public Vector3 Vector => precision == 0
        ? throw new InvalidOperationException()
        : new Vector3(NetPacking.UnquantizeFloat(x, min, max, precision),
            NetPacking.UnquantizeFloat(y, min, max, precision),
            NetPacking.UnquantizeFloat(z, min, max, precision));

    public UpdateVector(Vector3 vector, int precision, float min, float max)
    {
        this.precision = precision;
        this.min = min;
        this.max = max;
        x = NetPacking.QuantizeFloat(vector.X, min, max, precision);
        y = NetPacking.QuantizeFloat(vector.Y, min, max, precision);
        z = NetPacking.QuantizeFloat(vector.Z, min, max, precision);
    }

    public void WriteDelta(UpdateVector src, int deltaBits, ref BitWriter writer)
    {
        // ReSharper disable CompareOfFloatsByEqualityOperator
        if (precision != src.precision ||
            min != src.min ||
            max != src.max)
            throw new InvalidOperationException();
        // ReSharper restore CompareOfFloatsByEqualityOperator
        if (src.x == x && src.y == y && src.z == z)
        {
            writer.PutBool(false);
            return;
        }
        writer.PutBool(true);
        if (NetPacking.TryDelta(x, src.x, deltaBits, out var deltaA) &&
            NetPacking.TryDelta(y, src.y, deltaBits, out var deltaB) &&
            NetPacking.TryDelta(z, src.z, deltaBits, out var deltaC))
        {
            writer.PutBool(true);
            writer.PutUInt(deltaA, deltaBits);
            writer.PutUInt(deltaB, deltaBits);
            writer.PutUInt(deltaC, deltaBits);
        }
        else
        {
            writer.PutBool(false);
            writer.PutUInt(x, precision);
            writer.PutUInt(y, precision);
            writer.PutUInt(z, precision);
        }
    }

    public static UpdateVector ReadDelta(UpdateVector src, int deltaBits, ref BitReader reader)
    {
        if (!reader.GetBool())
            return src;
        uint x, y, z;
        if (reader.GetBool())
        {
            x = NetPacking.ApplyDelta(reader.GetUInt(deltaBits), src.x, deltaBits);
            y = NetPacking.ApplyDelta(reader.GetUInt(deltaBits), src.y, deltaBits);
            z = NetPacking.ApplyDelta(reader.GetUInt(deltaBits), src.z, deltaBits);
        }
        else
        {
            x = reader.GetUInt(src.precision);
            y = reader.GetUInt(src.precision);
            z = reader.GetUInt(src.precision);
        }
        return src with {x = x, y = y, z = z};
    }
}

public class ObjectUpdate
{
    public static readonly ObjectUpdate Blank = new (){Guns = []};

    private const int VELOCITY_DELTA_BITS = 14;
    public UpdateVector AngularVelocity = new(Vector3.Zero, 24, -16384, 16384);

    //Info
    public CruiseThrustState CruiseThrust;
    public bool EngineKill;
    public GunOrient[] Guns;

    public long HullValue;

    public ObjNetId ID;

    //Identifier
    public UpdateVector LinearVelocity = new(Vector3.Zero, 24, -32768, 32767);
    public UpdateQuaternion Orientation = Quaternion.Identity;
    public Vector3 Position;
    public RepAttitude RepToPlayer;
    public long ShieldValue;
    public float Throttle;
    public bool Tradelane;

    public void SetVelocity(Vector3 linear, Vector3 angular)
    {
        LinearVelocity = new UpdateVector(linear, 24, -32768, 32767);
        AngularVelocity = new UpdateVector(angular, 24, -16384, 16384);
    }

    private static bool CanQuantize(Vector3 a, Vector3 b, float min, float max)
    {
        return a.X - b.X >= min && a.X - b.X <= max &&
               a.Y - b.Y >= min && a.Y - b.Y <= max &&
               a.Z - b.Z >= min && a.Z - b.Z <= max;
    }

    private static Vector3 PostQuantize(Vector3 v, float min, float max, int bits)
    {
        var aX = NetPacking.QuantizeFloat(v.X, min, max, bits);
        var aY = NetPacking.QuantizeFloat(v.Y, min, max, bits);
        var aZ = NetPacking.QuantizeFloat(v.Z, min, max, bits);

        return new Vector3(
            NetPacking.UnquantizeFloat(aX, min, max, bits),
            NetPacking.UnquantizeFloat(aY, min, max, bits),
            NetPacking.UnquantizeFloat(aZ, min, max, bits)
        );
    }

    public void WriteDelta(ObjectUpdate src, uint oldTick, uint newTick, ref BitWriter msg)
    {
        #if DEBUG
        msg.PutByte(0xA1);
        #endif
        //ID
        if (src.ID != new ObjNetId(0) && src.ID != ID)
            throw new InvalidOperationException("Cannot delta from different object");
        if (oldTick == 0) {
            msg.PutByte(255);
        }
        else if (oldTick == newTick) {
            throw new ArgumentException("old tick == new tick");
        }
        else if ((newTick - oldTick) > 254 || oldTick > newTick)
        {
            throw new ArgumentException("old tick must be < newTick and up to 254 ticks away");
        }
        else {
            msg.PutByte((byte)(newTick - oldTick));
        }
        //Position
        if (NetPacking.ApproxEqual(src.Position, Position))
        {
            msg.PutUInt(0, 2);
        }
        else
        {
            if (CanQuantize(Position, src.Position, -512, 511))
            {
                msg.PutUInt(1, 2);
                msg.PutRangedVector3(Position - src.Position, -512, 511, 20);
                Position = src.Position + PostQuantize(Position - src.Position, -512, 511, 20);
            }
            else
            {
                msg.PutUInt(3, 2);
                msg.PutVector3(Position);
            }
        }

        //Orientation
        if (Orientation.Largest == src.Orientation.Largest &&
            Orientation.Component1 == src.Orientation.Component1 &&
            Orientation.Component2 == src.Orientation.Component2 &&
            Orientation.Component3 == src.Orientation.Component3)
        {
            msg.PutBool(false);
        }
        else
        {
            msg.PutBool(true);
            Orientation.WriteDelta(src.Orientation, ref msg);
        }

        //Linear Velocity
        LinearVelocity.WriteDelta(src.LinearVelocity, VELOCITY_DELTA_BITS, ref msg);
        //Angular Velocity
        AngularVelocity.WriteDelta(src.AngularVelocity, VELOCITY_DELTA_BITS, ref msg);

        //Flags
        msg.PutBool(Tradelane);
        msg.PutBool(EngineKill);
        msg.PutUInt((uint) CruiseThrust, 2);

        //Throttle
        if (NetPacking.QuantizedEqual(src.Throttle, Throttle, -1, 1, 7))
        {
            msg.PutBool(false);
        }
        else
        {
            msg.PutBool(true);
            msg.PutRangedFloat(Throttle, -1, 1, 7);
        }

        //Hull
        if (HullValue == src.HullValue)
        {
            msg.PutBool(false);
        }
        else
        {
            msg.PutBool(true);
            msg.PutVarInt64(HullValue - src.HullValue);
        }

        //Shield
        if (ShieldValue == src.ShieldValue)
        {
            msg.PutBool(false);
        }
        else
        {
            msg.PutBool(true);
            msg.PutVarInt64(ShieldValue - src.ShieldValue);
        }

        //Guns
        if (Guns == null || Guns.Length == 0)
        {
            msg.PutBool(false);
        }
        else
        {
            msg.PutBool(true);
            if (Guns.Length != (src.Guns?.Length ?? 0))
            {
                msg.PutBool(true);
                msg.PutVarUInt32((uint) Guns.Length);
            }
            else
            {
                msg.PutBool(false);
            }

            for (var i = 0; i < Guns.Length; i++)
            {
                var sg = (src.Guns?.Length ?? 0) > i ? src.Guns[i] : default;
                Guns[i].WriteDelta(sg, ref msg);
            }
        }
        #if DEBUG
        msg.PutByte(0xA1);
        #endif
    }

    public static ObjectUpdate ReadDelta(ref BitReader msg, uint mainTick, int id, Func<uint, int, ObjectUpdate> getSource)
    {
        #if DEBUG
        if(msg.GetByte() != 0xA1) throw new InvalidOperationException("Invalid delta data");
        #endif
        var p = new ObjectUpdate() { ID = new(id) };
        var b = msg.GetByte();
        ObjectUpdate source = b == 255 ? ObjectUpdate.Blank : getSource(mainTick - b, id);
        var posKind = msg.GetUInt(2);
        if (posKind == 0)
            p.Position = source.Position;
        else if (posKind == 1)
            p.Position = source.Position + msg.GetRangedVector3(-512, 511, 20);
        else if (posKind == 3)
            p.Position = msg.GetVector3();
        p.Orientation = msg.GetBool() ? UpdateQuaternion.ReadDelta(source.Orientation, ref msg) : source.Orientation;
        p.LinearVelocity = UpdateVector.ReadDelta(source.LinearVelocity, VELOCITY_DELTA_BITS, ref msg);
        p.AngularVelocity = UpdateVector.ReadDelta(source.AngularVelocity, VELOCITY_DELTA_BITS, ref msg);
        p.Tradelane = msg.GetBool();
        p.EngineKill = msg.GetBool();
        p.CruiseThrust = (CruiseThrustState)msg.GetUInt(2);
        p.Throttle = msg.GetBool() ? msg.GetRangedFloat(-1, 1, 7) : source.Throttle;
        p.HullValue = msg.GetBool() ? (source.HullValue + msg.GetVarInt64()) : source.HullValue;
        p.ShieldValue = msg.GetBool() ? (source.ShieldValue + msg.GetVarInt64()) : source.ShieldValue;
        if (msg.GetBool()) //Has guns
        {
            var len = msg.GetBool() ? (int) msg.GetVarUInt32() : source.Guns.Length;
            p.Guns = new GunOrient[len];
            for (var i = 0; i < len; i++)
            {
                var sg = (source.Guns?.Length ?? 0) > i ? source.Guns[i] : default;
                p.Guns[i].ReadDelta(ref msg, sg);
            }
        }
        else
        {
            p.Guns = Array.Empty<GunOrient>();
        }
        #if DEBUG
        if(msg.GetByte() != 0xA1) throw new InvalidOperationException("Invalid delta data");
        #endif
        return p;
    }
}
