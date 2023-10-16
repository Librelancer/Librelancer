using LibreLancer.Net.Protocol;
using LiteNetLib.Utils;
using Xunit;

namespace LibreLancer.Tests;


public class ChatMessageTests
{
    [Fact]
    public void ShouldParseColor()
    {
        var msg = BinaryChatMessage.ParseBbCode("[color=red]Hello[/color]");
        Assert.Single(msg.Segments);
        Assert.Equal("Hello", msg.Segments[0].Contents);
        Assert.Equal(Color4.Red, msg.Segments[0].Color.Color);
    }

    [Fact]
    public void ShouldParseSize()
    {
        var msg = BinaryChatMessage.ParseBbCode("[size=xlarge]Hello[/size]");
        Assert.Single(msg.Segments);
        Assert.Equal("Hello", msg.Segments[0].Contents);
        Assert.Equal(ChatMessageSize.XLarge, msg.Segments[0].Size);
    }

    void AssertRoundtrip(BinaryChatMessage msg)
    {
        var writer = new NetDataWriter();
        var pw = new PacketWriter(writer);
        msg.Put(pw);
        var reader = new NetDataReader(writer.CopyData());
        var pr = new PacketReader(reader);
        var msg2 = BinaryChatMessage.Read(pr);
        Assert.Equal(msg.Segments.Count, msg2.Segments.Count);
        for (int i = 0; i < msg.Segments.Count; i++) {
            Assert.Equal(msg.Segments[i].Bold, msg2.Segments[i].Bold);
            Assert.Equal(msg.Segments[i].Italic, msg2.Segments[i].Italic);
            Assert.Equal(msg.Segments[i].Underline, msg2.Segments[i].Underline);
            Assert.Equal(msg.Segments[i].Size, msg2.Segments[i].Size);
            Assert.Equal(msg.Segments[i].Color, msg2.Segments[i].Color);
            Assert.Equal(msg.Segments[i].Contents, msg2.Segments[i].Contents);
        }
    }

    [Fact]
    public void ShouldRoundtrip()
    {
        AssertRoundtrip(BinaryChatMessage.PlainText("Die schönste tomaten"));
        AssertRoundtrip(BinaryChatMessage.ParseBbCode("[color=red]This is[/color] [b]a big[/b] [i]deal.[/i]"));
        AssertRoundtrip(BinaryChatMessage.ParseBbCode("[size=xlarge]Hello World![/size]"));
    }
}
