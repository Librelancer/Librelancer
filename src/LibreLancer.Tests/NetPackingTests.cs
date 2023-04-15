using LibreLancer.Net.Protocol;
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
            var r = new BitReader(w.GetBuffer(), 0);
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
        var r = new BitReader(w.GetBuffer(), 0);
        Assert.Equal(v, r.GetUInt());
    }
}