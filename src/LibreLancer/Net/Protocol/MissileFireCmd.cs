namespace LibreLancer.Net.Protocol;

public struct MissileFireCmd
{
    public string Hardpoint;
    public ObjNetId Target;

    public static MissileFireCmd Read(PacketReader message)
    {
        var cmd = new MissileFireCmd();
        cmd.Hardpoint = message.GetHpid();
        cmd.Target = ObjNetId.Read(message);
        return cmd;
    }

    public void Put(PacketWriter message)
    {
        message.PutHpid(Hardpoint);
        Target.Put(message);
    }
}
