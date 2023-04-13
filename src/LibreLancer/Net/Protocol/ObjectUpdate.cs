//#define DEBUG_PACKET_ALIGNMENT

using System;
using System.Collections.Generic;
using System.Numerics;

namespace LibreLancer.Net.Protocol;

public class SPUpdatePacket : IPacket
{
    public int InputSequence;
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
    public int InputSequence;
    public uint OldTick;
    public uint Tick;
    public byte[] Updates;

    public void WriteContents(PacketWriter outPacket)
    {
        outPacket.Put(Tick);
        outPacket.Put(OldTick);
        outPacket.Put(InputSequence);
        outPacket.Put(Updates, 0, Updates.Length);
    }

    public static object Read(PacketReader message)
    {
        var p = new PackedUpdatePacket();
        p.Tick = message.GetUInt();
        p.OldTick = message.GetUInt();
        p.InputSequence = message.GetInt();
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
                var isCrc = reader.GetBool();
                var id = isCrc ? reader.GetInt() : reader.GetVarInt32();
                var u = ObjectUpdate.ReadDelta(ref reader, blankUpdate);
                u.IsCRC = isCrc;
                u.ID = id;
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
            var x = pendingUpdates.FindIndex(u => u.IsCRC == origUpdate[i].IsCRC && u.ID == origUpdate[i].ID);
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
                writer.PutBool(u.IsCRC);
                if (u.IsCRC)
                    writer.PutInt(u.ID);
                else
                    writer.PutVarInt32(u.ID);
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
    public string Hardpoint;
    public float AnglePitch;
    public float AngleRot;

    public void ReadDelta(ref BitReader message, GunOrient src)
    {
        Hardpoint = message.GetBool() ? message.GetHpid() : src.Hardpoint;
        AnglePitch = message.GetBool() ? message.GetRadiansQuantized() : src.AnglePitch;
        AngleRot = message.GetBool() ? message.GetRadiansQuantized() : src.AngleRot;
    }

    public void WriteDelta(GunOrient src, ref BitWriter message)
    {
        if (Hardpoint == src.Hardpoint)
        {
            message.PutBool(false);
        }
        else
        {
            message.PutBool(true);
            message.PutHpid(Hardpoint);
        }

        if (NetPacking.QuantizeAngle(AnglePitch, 16) ==
            NetPacking.QuantizeAngle(src.AnglePitch, 16))
        {
            message.PutBool(false);
        }
        else
        {
            message.PutBool(true);
            message.PutRadiansQuantized(AnglePitch);
        }

        if (NetPacking.QuantizeAngle(AngleRot, 16) ==
            NetPacking.QuantizeAngle(src.AngleRot, 16))
        {
            message.PutBool(false);
        }
        else
        {
            message.PutBool(true);
            message.PutRadiansQuantized(AngleRot);
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

    private const int DELTA_OFFSET = 2 << (DELTA_BITS - 2);
    private const int DELTA_MIN = -DELTA_OFFSET;
    private const int DELTA_MAX = DELTA_OFFSET - 1;

    public void WriteDelta(UpdateQuaternion src, ref BitWriter writer)
    {
        var diffA = (int) Component1 - (int) src.Component1;
        var diffB = (int) Component2 - (int) src.Component2;
        var diffC = (int) Component3 - (int) src.Component3;
        if (Largest == src.Largest &&
            diffA >= DELTA_MIN && diffA <= DELTA_MAX &&
            diffB >= DELTA_MIN && diffB <= DELTA_MAX &&
            diffC >= DELTA_MIN && diffC <= DELTA_MAX)
        {
            writer.PutBool(true);
            writer.PutUInt((uint) (diffA + DELTA_OFFSET), DELTA_BITS);
            writer.PutUInt((uint) (diffB + DELTA_OFFSET), DELTA_BITS);
            writer.PutUInt((uint) (diffC + DELTA_OFFSET), DELTA_BITS);
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
            var diffA = (int) (reader.GetUInt(DELTA_BITS) - DELTA_OFFSET);
            var diffB = (int) (reader.GetUInt(DELTA_BITS) - DELTA_OFFSET);
            var diffC = (int) (reader.GetUInt(DELTA_BITS) - DELTA_OFFSET);
            return new UpdateQuaternion
            {
                Largest = src.Largest,
                Component1 = (uint) ((int) src.Component1 + diffA),
                Component2 = (uint) ((int) src.Component2 + diffB),
                Component3 = (uint) ((int) src.Component3 + diffC)
            };
        }

        var uq = new UpdateQuaternion
        {
            Largest = reader.GetUInt(2),
            Component1 = reader.GetUInt(10),
            Component2 = reader.GetUInt(10),
            Component3 = reader.GetUInt(10)
        };
        return uq;
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
        var deltaOffset = 2 << (deltaBits - 2);
        var deltaMin = -deltaOffset;
        var deltaMax = deltaOffset - 1;
        var diffA = (int) x - (int) src.x;
        var diffB = (int) y - (int) src.y;
        var diffC = (int) z - (int) src.z;
        if (diffA >= deltaMin && diffA <= deltaMax &&
            diffB >= deltaMin && diffB <= deltaMax &&
            diffC >= deltaMin && diffC <= deltaMax)
        {
            writer.PutBool(true);
            writer.PutUInt((uint) (diffA + deltaOffset), deltaBits);
            writer.PutUInt((uint) (diffB + deltaOffset), deltaBits);
            writer.PutUInt((uint) (diffC + deltaOffset), deltaBits);
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
        var deltaOffset = 2 << (deltaBits - 2);
        uint x;
        uint y;
        uint z;
        if (reader.GetBool())
        {
            var diffX = (int) (reader.GetUInt(deltaBits) - deltaOffset);
            var diffY = (int) (reader.GetUInt(deltaBits) - deltaOffset);
            var diffZ = (int) (reader.GetUInt(deltaBits) - deltaOffset);
            x = (uint) ((int) src.x + diffX);
            y = (uint) ((int) src.y + diffY);
            z = (uint) ((int) src.z + diffZ);
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

    public float HullValue;

    public int ID;

    //Identifier
    public bool IsCRC;
    public UpdateVector LinearVelocity = new(Vector3.Zero, 24, -32768, 32767);
    public UpdateQuaternion Orientation = Quaternion.Identity;
    public Vector3 Position;
    public RepAttitude RepToPlayer;
    public float ShieldValue;
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
        if (src.ID != 0 && (IsCRC != src.IsCRC || src.ID != ID))
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
        if (NetPacking.ApproxEqual(Math.Abs(Quaternion.Dot(Orientation.Quaternion, src.Orientation.Quaternion)), 1.0f))
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
        if (NetPacking.ApproxEqual(HullValue, src.HullValue))
        {
            msg.PutBool(false);
        }
        else
        {
            msg.PutBool(true);
            msg.PutFloat(HullValue);
        }

        //Shield
        if (NetPacking.ApproxEqual(ShieldValue, src.ShieldValue))
        {
            msg.PutBool(false);
        }
        else
        {
            msg.PutBool(true);
            msg.PutFloat(ShieldValue);
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
        p.IsCRC = source.IsCRC;
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
        p.HullValue = msg.GetBool() ? msg.GetFloat() : source.HullValue;
        p.ShieldValue = msg.GetBool() ? msg.GetFloat() : source.ShieldValue;
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