using System.Runtime.InteropServices;

namespace LibreLancer.Net;

[StructLayout(LayoutKind.Explicit)]
public struct UpdateAck
{
    [FieldOffset(0)]
    public uint Tick;
    [FieldOffset(4)]
    public ulong History;

    [FieldOffset(4)] public uint History0;
    [FieldOffset(8)] public uint History1;

    public UpdateAck(uint tick, ulong history)
    {
        Tick = tick;
        History = history;
    }

    public UpdateAck(uint tick, uint history0, uint history1)
    {
        Tick = tick;
        History0 = history0;
        History1 = history1;
    }

    public bool this[uint tick]
    {
        get
        {
            if (tick > Tick)
                return false;
            if (tick == Tick)
                return true;
            var idx = Tick - (tick + 1);
            return idx < 63 && (History & (1UL << (int)idx)) != 0;
        }
        set
        {
            var idx = Tick - (tick + 1);
            if (idx > 63) return;
            if(value)
                History |= (1UL << (int)idx);
            else
                History &= ~(1UL << (int)idx);
        }
    }
}
