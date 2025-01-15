using System;
using System.IO;
using System.Linq;
using System.Text;
using LibreLancer.Ini;
using Xunit;

namespace LibreLancer.Tests.Ini;

public class IniWriterTests
{
    private static Section[] Parse(string ini)
    {
        var stream = new MemoryStream(Encoding.UTF8.GetBytes(ini));
        var parser = new LancerTextIniParser();
        return parser.ParseIniFile(null, stream).ToArray();
    }

    static Section[] RoundTripBini(Section[] input)
    {
        var stream = new MemoryStream();
        IniWriter.WriteBini(stream, input);
        stream.Position = 0;
        var parser = new BinaryIniParser();
        return parser.ParseIniFile("TEST", stream).ToArray();
    }

    static Section[] RoundTripText(Section[] input)
    {
        var stream = new MemoryStream();
        IniWriter.WriteIni(stream, input);
        stream.Position = 0;
        var parser = new LancerTextIniParser();
        return parser.ParseIniFile("TEST", stream).ToArray();
    }

    [Fact]
    public void EmptyWriteSucceeds()
    {
        Assert.Empty(RoundTripBini(Array.Empty<Section>()));
        Assert.Empty(RoundTripText(Array.Empty<Section>()));
    }

    [Theory]
    [InlineData("[Section]\nName = Hello")]
    [InlineData("[Commodities]\niron = 1.42, 300, icons\\iron.bmp, \"+1\"")]
    [InlineData("[Value]emptyvalue\na = true\nb = false\nc = 3.145")]
    public void ShouldRoundTrip(string source)
    {
        var sections = Parse(source);

        // Compare all for bini
        var bini = RoundTripBini(sections);
        Assert.Equal(sections.Length, bini.Length);
        for (int i = 0; i < bini.Length; i++)
        {
            Assert.Equal(sections[i].Name, bini[i].Name);
            Assert.Equal(sections[i].Count, bini[i].Count);
            for (int j = 0; j < bini[i].Count; j++)
            {
                Assert.Equal(sections[i][j].Name, bini[i][j].Name);
                Assert.Equal(sections[i][j].Count, bini[i][j].Count);
                for (int k = 0; k < bini[i][j].Count; k++)
                {
                    Assert.Equal(sections[i][j][k].ToString(), bini[i][j][k].ToString());
                }
            }
        }

        // Make sure all the same
        var tini = RoundTripText(sections);
        Assert.Equal(sections.Length, tini.Length);
        for (int i = 0; i < tini.Length; i++)
        {
            Assert.Equal(sections[i].Name, tini[i].Name);
            Assert.Equal(sections[i].Count, tini[i].Count);
            for (int j = 0; j < tini[i].Count; j++)
            {
                Assert.Equal(sections[i][j].Name, tini[i][j].Name);
                Assert.Equal(sections[i][j].Count, tini[i][j].Count);
                for (int k = 0; k < tini[i][j].Count; k++)
                {
                    Assert.Equal(sections[i][j][k].ToString(), tini[i][j][k].ToString());
                }
            }
        }
    }
}
