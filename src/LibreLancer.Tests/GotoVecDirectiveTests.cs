using System.Numerics;
using LibreLancer.Data.Ini;
using LibreLancer.Data.Schema.Missions;
using LibreLancer.Missions;
using LibreLancer.Missions.Directives;
using LibreLancer.Net.Protocol;
using LibreLancer.World;
using LiteNetLib.Utils;
using Xunit;

namespace LibreLancer.Tests;

public class GotoVecDirectiveTests
{
    [Fact]
    public void ParsesReferenceCruiseSpeedRange()
    {
        var entry = new Entry(new Section("ObjList"), "GotoVec");
        ValueBase[] values =
        [
            "goto_cruise", -44513f, 0f, 67197f, 200f, true, -1f,
            "Player", 500f, 1000f, 1f
        ];
        foreach (var value in values)
            entry.Add(value);

        var directive = new GotoVecDirective(entry);

        Assert.Equal(GotoKind.GotoCruise, directive.CruiseKind);
        Assert.Equal(new Vector3(-44513, 0, 67197), directive.Target);
        Assert.Equal("Player", directive.CruiseSpeedReference);
        Assert.Equal(500, directive.CruiseSpeedFullDistance);
        Assert.Equal(1000, directive.CruiseSpeedZeroDistance);
        Assert.Equal(1, directive.CruiseSpeedUnknown);
    }

    [Fact]
    public void ReferenceCruiseSpeedRangeSurvivesPacketRoundTrip()
    {
        var source = new GotoVecDirective
        {
            Target = new Vector3(1, 2, 3),
            CruiseKind = GotoKind.GotoCruise,
            Range = 200,
            Unknown = true,
            MaxThrottle = -1,
            CruiseSpeedReference = "Player",
            CruiseSpeedFullDistance = 500,
            CruiseSpeedZeroDistance = 1000,
            CruiseSpeedUnknown = 1
        };
        var data = new NetDataWriter();
        source.Put(new PacketWriter(data));

        var result = Assert.IsType<GotoVecDirective>(
            MissionDirective.Read(new PacketReader(new NetDataReader(data.CopyData()))));

        Assert.Equal(source.Target, result.Target);
        Assert.Equal(source.CruiseKind, result.CruiseKind);
        Assert.Equal(source.CruiseSpeedReference, result.CruiseSpeedReference);
        Assert.Equal(source.CruiseSpeedFullDistance, result.CruiseSpeedFullDistance);
        Assert.Equal(source.CruiseSpeedZeroDistance, result.CruiseSpeedZeroDistance);
        Assert.Equal(source.CruiseSpeedUnknown, result.CruiseSpeedUnknown);
    }
}
