using System.Numerics;
using LibreLancer.Net;
using LibreLancer.Net.Protocol;
using LibreLancer.World.Components;
using LiteNetLib.Utils;
using Xunit;

namespace LibreLancer.Tests;

public class InputUpdatePacketTests
{
    [Fact]
    public void ShouldRoundtrip()
    {
        var pkt = new InputUpdatePacket();
        pkt.AckTick = 97;
        pkt.SelectedObject = new ObjNetId(1673);
        pkt.Current = new NetInputControls()
        {
            Cruise = true,
            Steering = Vector3.UnitY,
            Strafe = StrafeControls.Left,
            Throttle = 1,
            Thrust = false,
            Tick = 98
        };

        var dw = new NetDataWriter();
        var writer = new PacketWriter(dw);
        pkt.WriteContents(writer);

        var dr = new NetDataReader(dw.CopyData());
        var reader = new PacketReader(dr);
        var pkt2 = (InputUpdatePacket)InputUpdatePacket.Read(reader);

        Assert.Equal(pkt.AckTick, pkt2.AckTick);
        Assert.Equal(pkt.SelectedObject, pkt2.SelectedObject);
        Assert.Equal(pkt.Current.Tick, pkt2.Current.Tick);

    }
}
