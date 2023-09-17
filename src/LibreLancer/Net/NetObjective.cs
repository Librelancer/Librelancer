using System;
using System.Numerics;
using BepuUtilities;
using LibreLancer.Net.Protocol;

namespace LibreLancer.Net;

public enum ObjectiveKind
{
    NoObjective,
    Basic,
    NavMarker,
    Object,
}

public struct NetObjective
{
    public bool Equals(NetObjective other)
    {
        return Kind == other.Kind &&
               Ids == other.Ids &&
               Explanation == other.Explanation &&
               string.Equals(System, other.System, StringComparison.OrdinalIgnoreCase) &&
               string.Equals(Object, other.Object, StringComparison.OrdinalIgnoreCase) &&
               Position.Equals(other.Position);
    }

    public override bool Equals(object obj)
    {
        return obj is NetObjective other && Equals(other);
    }

    public override int GetHashCode()
    {
        var hashCode = new HashCode();
        hashCode.Add((int) Kind);
        hashCode.Add(Ids);
        hashCode.Add(Explanation);
        hashCode.Add(System, StringComparer.OrdinalIgnoreCase);
        hashCode.Add(Object, StringComparer.OrdinalIgnoreCase);
        hashCode.Add(Position);
        return hashCode.ToHashCode();
    }

    public static bool operator ==(NetObjective left, NetObjective right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(NetObjective left, NetObjective right)
    {
        return !left.Equals(right);
    }

    public ObjectiveKind Kind;
    public int Ids;
    public int Explanation;
    public string System;
    public string Object;
    public Vector3 Position;

    public NetObjective(int ids)
    {
        Kind = ObjectiveKind.Basic;
        Ids = ids;
        Explanation = 0;
        System = null;
        Position = Vector3.Zero;
    }

    public NetObjective(int ids, int explanation, string system, Vector3 position)
    {
        Kind = ObjectiveKind.NavMarker;
        Ids = ids;
        Explanation = explanation;
        System = system;
        Position = position;
    }

    public NetObjective(int ids, int explanation, string system, string obj)
    {
        Kind = ObjectiveKind.Object;
        Ids = ids;
        Explanation = explanation;
        System = system;
        Object = obj;
    }

    public static NetObjective Read(PacketReader reader)
    {
        var k = (ObjectiveKind) reader.GetByte();
        if (k == ObjectiveKind.Basic)
            return new NetObjective(reader.GetVariableInt32());
        if (k == ObjectiveKind.NavMarker)
            return new NetObjective(
                reader.GetVariableInt32(),
                reader.GetVariableInt32(),
                reader.GetString(),
                reader.GetVector3()
            );
        if (k == ObjectiveKind.Object)
            return new NetObjective(
                reader.GetVariableInt32(),
                reader.GetVariableInt32(),
                reader.GetString(),
                reader.GetString()
            );
        return new NetObjective();
    }

    public void Put(PacketWriter writer)
    {
        writer.Put((byte) Kind);
        if (Kind == ObjectiveKind.Basic)
            writer.PutVariableInt32(Ids);
        if (Kind == ObjectiveKind.NavMarker)
        {
            writer.PutVariableInt32(Ids);
            writer.PutVariableInt32(Explanation);
            writer.Put(System);
            writer.Put(Position);
        }

        if (Kind == ObjectiveKind.Object)
        {
            writer.PutVariableInt32(Ids);
            writer.PutVariableInt32(Explanation);
            writer.Put(System);
            writer.Put(Object);
        }
    }
}
