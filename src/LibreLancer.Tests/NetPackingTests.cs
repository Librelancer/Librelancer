using LibreLancer.Net.Protocol;
using LiteNetLib.Utils;
using Xunit;

namespace LibreLancer.Tests;

public class NetPackingTests
{
    [Fact]
    public void DeltaBits()
    {
        var baseValue = 2000U;
        for (int i = -64; i <= 63; i++)
        {
            var newValue = (uint) (baseValue + i);
            Assert.True(NetPacking.TryDelta(newValue, baseValue, 7, out var d));
            Assert.Equal(newValue, NetPacking.ApplyDelta(d, baseValue, 7));
        }
    }

    [Theory]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(4)]
    [InlineData(5)]
    [InlineData(6)]
    [InlineData(7)]
    [InlineData(8)]
    [InlineData(9)]
    [InlineData(10)]
    public void WriteBits(int bits)
    {
        for(uint i = 0; i < (2 << (bits - 1)); i++)
        {
            var w = new BitWriter();
            w.PutUInt(1,2);
            w.PutUInt(i, bits);
            w.PutUInt(i, bits);
            w.PutUInt(i, bits);
            var r = new BitReader(w.GetCopy(), 0);
            Assert.Equal(1U, r.GetUInt(2));
            Assert.Equal(i, r.GetUInt(bits));
            Assert.Equal(i, r.GetUInt(bits));
            Assert.Equal(i, r.GetUInt(bits));
        }
    }

    [Theory]
    [InlineData(0xFFFFFFFF)]
    [InlineData(0x7FFFFFF)]
    [InlineData(0x7FFF)]
    [InlineData(0x7FF)]
    [InlineData(0x3F)]
    public void WriteUInt(uint v)
    {
        var w = new BitWriter();
        w.PutUInt(v, 32);
        var r = new BitReader(w.GetCopy(), 0);
        Assert.Equal(v, r.GetUInt());
    }

    [Fact]
    public void Alignment()
    {
        var w = new BitWriter();
        w.PutBool(true);
        w.Align();
        w.PutByte(0x33);
        w.Align();
        var result = w.GetCopy();
        Assert.Equal(2, result.Length);
        var r = new BitReader(result, 0);
        Assert.Equal(true, r.GetBool());
        r.Align();
        Assert.Equal(0x33, r.GetByte());
        r.Align();
    }

    [Theory]
    [InlineData("abcdefg")]
    [InlineData("")]
    [InlineData(null)]
    [InlineData("123")]
    [InlineData("li01_01_base")]
    [InlineData("The quick brown fox jumps over the lazy dog.")]
    [InlineData(@"案ずるより産むが易し。 - Giving birth to a baby is easier than worrying about it.
出る杭は打たれる。 - The stake that sticks up gets hammered down.
知らぬが仏。 - Not knowing is Buddha. - Ignorance is bliss.
見ぬが花。 - Not seeing is a flower. - Reality can't compete with imagination.
花は桜木人は武士 - Of flowers, the cherry blossom; of men, the warrior.")]
    public void Strings(string s)
    {
        var pw = new PacketWriter();
        pw.Put(s);
        var pr = new PacketReader(new NetDataReader(pw.GetCopy()));
        Assert.Equal(s, pr.GetString());
    }

    [Theory]
    [InlineData("abcdefg")]
    [InlineData(null)]
    [InlineData("123")]
    [InlineData("")]
    [InlineData("The quick brown fox jumps over the lazy dog.")]
    public void TryGetStrings(string s)
    {
        var pw = new PacketWriter();
        pw.Put(s);
        var pr = new PacketReader(new NetDataReader(pw.GetCopy()));
        Assert.True(pr.TryGetString(out string s2));
        Assert.Equal(s, s2);
    }

    [Fact]
    public void TryGetStringEmpty()
    {
        var pw = new PacketWriter();
        var pr = new PacketReader(new NetDataReader(pw.GetCopy()));
        Assert.False(pr.TryGetString(out _));
    }

    [Fact]
    public void TryGetStringInvalid()
    {
        var pw = new PacketWriter();
        pw.Put((byte)0x45);
        var pr = new PacketReader(new NetDataReader(pw.GetCopy()));
        Assert.False(pr.TryGetString(out _));
    }
}
