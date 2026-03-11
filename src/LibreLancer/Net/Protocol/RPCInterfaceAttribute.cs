using System;

namespace LibreLancer.Net.Protocol;

internal class RPCInterfaceAttribute : Attribute { }

internal class ChannelAttribute : Attribute
{
    public int Channel;
    public ChannelAttribute(int channel)
    {
        Channel = channel;
    }
}
