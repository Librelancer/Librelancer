namespace LibreLancer.Net;

public struct MissileFireCmd
{
    public string Hardpoint;
    public bool TargetIsCrc;
    public int Target;

    public static MissileFireCmd Read(PacketReader message)
    {
        var cmd = new MissileFireCmd();
        cmd.Hardpoint = message.GetHpid();
        cmd.TargetIsCrc = message.GetBool();
        cmd.Target = cmd.TargetIsCrc ? message.GetInt() : message.GetVariableInt32();
        return cmd;
    }

    public void Put(PacketWriter message)
    {
        message.PutHpid(Hardpoint);
        message.Put(TargetIsCrc);
        if(TargetIsCrc)
            message.Put(Target);
        else
            message.PutVariableInt32(Target);
    }
}