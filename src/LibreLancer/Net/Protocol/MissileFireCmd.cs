namespace LibreLancer.Net.Protocol;

public struct MissileFireCmd
{
    public uint Hardpoint;
    public ObjNetId Target;

    public static MissileFireCmd Read(PacketReader message)
    {
        var cmd = new MissileFireCmd
        {
            Hardpoint = message.GetUInt(),
            Target = ObjNetId.Read(message)
        };
        return cmd;
    }

    public void Put(PacketWriter message)
    {
        message.Put(Hardpoint);
        Target.Put(message);
    }
}
