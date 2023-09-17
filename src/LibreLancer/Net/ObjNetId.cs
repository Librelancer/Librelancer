using LibreLancer.Net.Protocol;
using LibreLancer.World;

namespace LibreLancer.Net;

public struct ObjNetId
{
    public bool IsCRC;
    public int Value;


    public static ObjNetId Read(PacketReader message)
    {
        var id = new ObjNetId() {IsCRC = message.GetBool()};
        if (id.IsCRC)
            id.Value = (int) message.GetUInt();
        else
            id.Value = message.GetVariableInt32();
        return id;
    }

    public void Put(PacketWriter message)
    {
        message.Put(IsCRC);
        if(IsCRC)
            message.Put((uint)Value);
        else
            message.PutVariableInt32(Value);
    }

    public static implicit operator ObjNetId(GameObject obj)
    {
        if (obj == null) return default;
        if (obj.SystemObject != null)
            return new ObjNetId() {IsCRC = true, Value = (int) obj.NicknameCRC};
        return new ObjNetId() {IsCRC = false, Value = obj.NetID};
    }

    public override string ToString() => IsCRC ? $"[CRC: 0x{((uint) Value):X}]" : $"[NetObject: {Value}]";
}
