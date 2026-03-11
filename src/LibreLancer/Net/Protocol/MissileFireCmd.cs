namespace LibreLancer.Net.Protocol;

public struct MissileFireCmd
{
    public string Hardpoint;
    public ObjNetId Target;

    public static MissileFireCmd Read(PacketReader message)
    {
        var cmd = new MissileFireCmd
        {
            Hardpoint = message.GetHpid(),
            Target = ObjNetId.Read(message)
        };
        return cmd;
    }

    public void Put(PacketWriter message)
    {
        message.PutHpid(Hardpoint);
        Target.Put(message);
    }
}
