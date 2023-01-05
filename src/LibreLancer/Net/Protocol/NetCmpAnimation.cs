namespace LibreLancer.Net.Protocol;

public struct NetCmpAnimation
{
    public string Name;
    public float StartTime;
    public bool Reverse;
    public bool Finished;
    public bool Loop;

    public static NetCmpAnimation Read(PacketReader message)
    {
        var os = new NetCmpAnimation();
        os.Name = message.GetString();
        os.StartTime = message.GetFloat();
        var flags = message.GetByte();
        os.Finished = (flags & (1 << 0)) != 0;
        os.Loop = (flags & (1 << 1)) != 0;
        os.Reverse = (flags & (1 << 2)) != 0;
        return os;
    }

    public void Put(PacketWriter message)
    {
        message.Put(Name);
        message.Put(StartTime);
        byte flags = 0;
        if (Finished) flags |= (1 << 0);
        if (Loop) flags |= (1 << 1);
        if (Reverse) flags |= (1 << 2);
        message.Put(flags);
    }
}