using System.Collections.Generic;
using LibreLancer.Net.Protocol;

namespace LibreLancer.Net;

public class AllowedDocking
{
    public bool CanDock = true;
    public bool CanTl = true;
    public HashSet<uint> DockExceptions;
    public HashSet<uint> TlExceptions;

    public void Put(PacketWriter message)
    {
        var flags = CanDock ? (1 << 0) : 0;
        if (CanTl) flags |= (1 << 1);
        message.Put((byte)flags);
        if (DockExceptions is { Count: > 0 })
        {
            message.PutVariableUInt32((uint)DockExceptions.Count);
            foreach(var e in DockExceptions)
                message.Put(e);
        }
        else
        {
            message.PutVariableUInt32(0);
        }
        if (TlExceptions is { Count: > 0 })
        {
            message.PutVariableUInt32((uint)TlExceptions.Count);
            foreach (var e in TlExceptions)
                message.Put(e);
        }
        else
        {
            message.PutVariableUInt32(0);
        }
    }

    public static AllowedDocking Read(PacketReader message)
    {
        var flags = message.GetByte();
        var de = new HashSet<uint>();
        var countDe = message.GetVariableUInt32();
        for (int i = 0; i < countDe; i++)
        {
            de.Add(message.GetUInt());
        }
        var tl = new HashSet<uint>();
        var countTl = message.GetVariableUInt32();
        for(int i =0 ; i < countTl; i++)
        {
            tl.Add(message.GetUInt());
        }
        return new()
        {
            CanDock = (flags & (1 << 0)) != 0,
            CanTl = (flags & (1 << 1)) != 0,
            DockExceptions = de,
            TlExceptions = tl
        };
    }

}
