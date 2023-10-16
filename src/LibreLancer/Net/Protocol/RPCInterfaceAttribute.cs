using System;

namespace LibreLancer.Net.Protocol;

class RPCInterfaceAttribute : Attribute { }

class ChannelAttribute : Attribute
{
    public int Channel;
    public ChannelAttribute(int channel)
    {
        Channel = channel;
    }
}
