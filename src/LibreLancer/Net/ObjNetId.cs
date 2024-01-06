using System;
using LibreLancer.Net.Protocol;
using LibreLancer.World;

namespace LibreLancer.Net;

public struct ObjNetId
{
    public int Value;

    public ObjNetId(int value) => Value = value;

    public bool Equals(ObjNetId other) => Value == other.Value;

    public static ObjNetId Read(PacketReader message) => new(message.GetVariableInt32());


    public static ObjNetId Read(ref BitReader message) => new (message.GetVarInt32());

    public void Put(PacketWriter message) => message.PutVariableInt32(Value);

    public void Put(BitWriter message) => message.PutVarInt32(Value);

    public override bool Equals(object obj) => obj is ObjNetId other && Equals(other);

    public override int GetHashCode() => Value.GetHashCode();
    public static bool operator ==(ObjNetId left, ObjNetId right) => left.Equals(right);

    public static bool operator !=(ObjNetId left, ObjNetId right) => !left.Equals(right);

    public static implicit operator ObjNetId(GameObject obj)
    {
        return new ObjNetId() {Value = obj?.NetID ?? 0};
    }

    public override string ToString() => Value == 0 ? "NULL" : $"[ObjId: {Value}]";
}
