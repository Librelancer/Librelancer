using LibreLancer.Net.Protocol;
using Xunit;

namespace LibreLancer.Tests;

public class NetRleTests
{
    int AssertRoundtrip(byte[] data)
    {
        var writer = new NetRleWriter();
        for (int i = 0; i < data.Length; i++)
        {
            writer.Write(data[i]);
        }

        var encLength = writer.Length;
        var encoded = writer.GetCopy();
        Assert.Equal(encLength, writer.Length);

        var reader = new NetRleReader(encoded);
        var result = new byte[data.Length];
        for (int i = 0; i < data.Length; i++)
        {
            result[i] = reader.ReadByte();
        }
        Assert.Equal(data, result);
        return encoded.Length;
    }

    [Theory]
    [InlineData(1)]
    [InlineData(3)]
    [InlineData(5)]
    [InlineData(6)]
    [InlineData(7)]
    [InlineData(18)]
    [InlineData(22)]
    [InlineData(23)]
    [InlineData(24)]
    [InlineData(255)]
    [InlineData(279)]
    [InlineData(281)]
    public void ZeroRleRoundtrips(int zeroCount)
    {
        var data = new byte[zeroCount + 1];
        data[zeroCount] = 6;
        AssertRoundtrip(data);
    }

    [Theory]
    [InlineData(new byte[] { 255, 255, 255, 3, 0, 56, 4, 0})]
    [InlineData(new byte[] { 4, 3, 1, 3, 0, 56, 4, 0, 255, 96, 0})]
    [InlineData(new byte[] { 2, 12, 10, 8, 0 ,0 ,0 ,0, 1, 55, 0, 0, 255, 255, 0 ,1 })]
    [InlineData(new byte[] { 0, 176, 8, 152, 16 })]
    public void OtherRoundtrips(byte[] data)
    {
        AssertRoundtrip(data);
    }


    [Fact]
    public void SmallDiffs()
    {
        byte[] data = { 0, 1, 2, 3, 4, 5, 4, 3, 2, 1, 0 };
        int encodeCount = AssertRoundtrip(data);
        Assert.True(encodeCount < data.Length);
    }

    [Fact]
    public void MediumLiterals()
    {
        // each of these should encode to a 1 byte/2 nibble diff +6/-6
        byte[] data = { 6, 12, 18, 24, 18, 12, 6 };
        int encodeCount = AssertRoundtrip(data);
        Assert.Equal(data.Length, encodeCount);
    }

    [Fact]
    public void LargeDiffs()
    {
        // 2x literal arrays (up to 4 non-0 bytes)
        byte[] data = { 255, 1, 254, 2, 253, 3, 252, 4 };
        int encodeCount = AssertRoundtrip(data);
        Assert.Equal(9, encodeCount);
    }


    [Fact]
    public void Rewind()
    {
        var writer = new NetRleWriter();
        writer.Write(0xCA);
        writer.Write(0xFE);
        writer.Write(0xBA);
        int lengthA = writer.Length;
        writer.Checkpoint();
        writer.Write(1);
        writer.Write(2);
        writer.Write(3);
        writer.Rewind();
        int lengthB = writer.Length;
        Assert.Equal(lengthA, lengthB);
        writer.Write(0xBE);
        var enc = writer.GetCopy();
        var reader = new  NetRleReader(enc);
        Assert.Equal(0xCA, reader.ReadByte());
        Assert.Equal(0xFE, reader.ReadByte());
        Assert.Equal(0xBA, reader.ReadByte());
        Assert.Equal(0xBE, reader.ReadByte());
    }
}
