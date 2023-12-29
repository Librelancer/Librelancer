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
    public byte[] Updates;

    public int DataSize { get; private set; }

    public void WriteContents(PacketWriter outPacket)
    {
        outPacket.PutVariableUInt32(Tick);
        outPacket.PutVariableInt32((int)((long)OldTick - Tick));
        outPacket.PutVariableInt32((int)((long)InputSequence - Tick));
        outPacket.Put(Updates, 0, Updates.Length);
    }

    public static object Read(PacketReader message)
    {
        var p = new PackedUpdatePacket();
        p.DataSize = message.Size;
        p.Tick = message.GetVariableUInt32();
        p.OldTick = (uint) (p.Tick + message.GetVariableInt32());
        p.InputSequence = (uint) (p.Tick + message.GetVariableInt32());
        p.Updates = message.GetRemainingBytes();
        return p;
    }

    public (PlayerAuthState, ObjectUpdate[]) GetUpdates(PlayerAuthState origAuth, ObjectUpdate[] origUpdate,
        NetHpidReader hpids)
    {
        var reader = new BitReader(Updates, 0, hpids);
        var pa = PlayerAuthState.Read(ref reader, origAuth);
#if DEBUG_PACKET_ALIGNMENT
        if (reader.GetUInt(16) != 0xBABE) throw new InvalidOperationException();
        var origLen = reader.GetUInt(32);
        if (origLen != (uint) origUpdate.Length) throw new InvalidOperationException($"{origLen} != {origUpdate.Length}");
#endif
        var blankUpdate = new ObjectUpdate {Guns = Array.Empty<GunOrient>()};
        var updates = new List<ObjectUpdate>();
        for (var i = 0; i < origUpdate.Length; i++)
        {
            var hasUpdate = reader.GetBool();
            if (hasUpdate) updates.Add(ObjectUpdate.ReadDelta(ref reader, origUpdate[i]));
        }
#if DEBUG_PACKET_ALIGNMENT
        if (reader.GetUInt(16) != 0xBABE) throw new InvalidOperationException();
#endif
        var hasMore = reader.GetBool();
        if (hasMore)
        {
            var moreLength = reader.GetVarUInt32();
#if DEBUG_PACKET_ALIGNMENT
            FLLog.Debug("Client", $"Received {moreLength} new updates");
#endif
            while (moreLength-- > 0)
            {
                var id =  reader.GetVarInt32();
                var u = ObjectUpdate.ReadDelta(ref reader, blankUpdate);
                u.ID = new ObjNetId(id);
                updates.Add(u);
            }
        }
#if DEBUG_PACKET_ALIGNMENT
        if (reader.GetUInt(16) != 0xBABE) throw new InvalidOperationException();
#endif
        return (pa, updates.ToArray());
    }

    //Returns re-ordered list of updates
    public ObjectUpdate[] SetUpdates(
        PlayerAuthState newAuth,
        PlayerAuthState origAuth,
        ObjectUpdate[] origUpdate,
        IEnumerable<ObjectUpdate> newUpdate,
        NetHpidWriter hpids
    )
    {
        var writer = new BitWriter();
        var blankUpdate = new ObjectUpdate {Guns = Array.Empty<GunOrient>()};
        writer.HpidWriter = hpids;
        newAuth.Write(ref writer, origAuth);
#if DEBUG_PACKET_ALIGNMENT
        writer.PutUInt(0xBABE, 16);
        writer.PutUInt((uint)origUpdate.Length, 32);
#endif
        //Existing updates (delta against old state + no ID write)
        var pendingUpdates = new List<ObjectUpdate>(newUpdate);
        var sentUpdates = new List<ObjectUpdate>();
        for (var i = 0; i < origUpdate.Length; i++)
        {
            var x = pendingUpdates.FindIndex(u => u.ID == origUpdate[i].ID);
            if (x != -1)
            {
                writer.PutBool(true);
                var u = pendingUpdates[x];
                pendingUpdates.RemoveAt(x);
                u.WriteDelta(origUpdate[i], ref writer);
                sentUpdates.Add(u);
            }
            else
            {
                writer.PutBool(false); // update not included for this ID
            }
        }
#if DEBUG_PACKET_ALIGNMENT
        writer.PutUInt(0xBABE, 16);
#endif
        //New updates (delta against blank, write ID)
        if (pendingUpdates.Count > 0)
        {
            writer.PutBool(true);
            writer.PutVarUInt32((uint) pendingUpdates.Count);
            foreach (var u in pendingUpdates)
            {
                writer.PutVarInt32(u.ID.Value);
                u.WriteDelta(blankUpdate, ref writer);
            }

            sentUpdates.AddRange(pendingUpdates);
        }
        else
        {
            writer.PutBool(false);
        }
#if DEBUG_PACKET_ALIGNMENT
        writer.PutUInt(0xBABE, 16);
#endif
        Updates = writer.GetBuffer();
        return sentUpdates.ToArray();
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

    private static bool FlagsEqual(ObjectUpdate a, ObjectUpdate b)
    {
        return a.Tradelane == b.Tradelane &&
               a.EngineKill == b.EngineKill &&
               a.CruiseThrust == b.CruiseThrust &&
               a.RepToPlayer == b.RepToPlayer;
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

    public void WriteDelta(ObjectUpdate src, ref BitWriter msg)
    {
        //ID
        if (src.ID != new ObjNetId(0) && src.ID != ID)
            throw new InvalidOperationException("Cannot delta from different object");
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
        if (FlagsEqual(src, this))
        {
            msg.PutBool(false);
        }
        else
        {
            msg.PutBool(true);
            msg.PutBool(Tradelane);
            msg.PutBool(EngineKill);
            msg.PutUInt((uint) CruiseThrust, 2);
            msg.PutUInt((uint) RepToPlayer, 2);
        }

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
    }

    public static ObjectUpdate ReadDelta(ref BitReader msg, ObjectUpdate source)
    {
        var p = new ObjectUpdate();
        p.ID = source.ID;
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
        var readFlags = msg.GetBool();
        p.Tradelane = readFlags ? msg.GetBool() : source.Tradelane;
        p.EngineKill = readFlags ? msg.GetBool() : source.EngineKill;
        p.CruiseThrust = readFlags ? (CruiseThrustState) msg.GetUInt(2) : source.CruiseThrust;
        p.RepToPlayer = readFlags ? (RepAttitude) msg.GetUInt(2) : source.RepToPlayer;
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

        return p;
    }
}
