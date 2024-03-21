using System;
using System.IO;
using System.Linq;
using System.Text;
using FluentAssertions;
using LibreLancer.Ini;
using Xunit;

namespace LibreLancer.Tests.Ini;

public class IniWriterTests
{
    private static Section[] Parse(string ini)
    {
        var stream = new MemoryStream(Encoding.UTF8.GetBytes(ini));
        var parser = new TextIniParser();
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
        var parser = new TextIniParser();
        return parser.ParseIniFile("TEST", stream).ToArray();
    }

    [Fact]
    public void EmptyWriteSucceeds()
    {
        RoundTripBini(Array.Empty<Section>()).Should().HaveCount(0);
        RoundTripText(Array.Empty<Section>()).Should().HaveCount(0);
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
        bini.Length.Should().Be(sections.Length);
        for (int i = 0; i < bini.Length; i++)
        {
            bini[i].Name.Should().Be(sections[i].Name);
            bini[i].Count.Should().Be(sections[i].Count);
            for (int j = 0; j < bini[i].Count; j++)
            {
                bini[i][j].Name.Should().Be(sections[i][j].Name);
                bini[i][j].Count.Should().Be(sections[i][j].Count);
                for (int k = 0; k < bini[i][j].Count; k++)
                {
                    bini[i][j][k].ToString().Should().Be(sections[i][j][k].ToString());
                }
            }
        }

        // Make sure all the same
        var tini = RoundTripText(sections);
        tini.Length.Should().Be(sections.Length);
        for (int i = 0; i < tini.Length; i++)
        {
            tini[i].Name.Should().Be(sections[i].Name);
            tini[i].Count.Should().Be(sections[i].Count);
            for (int j = 0; j < tini[i].Count; j++)
            {
                tini[i][j].Name.Should().Be(sections[i][j].Name);
                tini[i][j].Count.Should().Be(sections[i][j].Count);
                for (int k = 0; k < tini[i][j].Count; k++)
                {
                    tini[i][j][k].ToString().Should().Be(sections[i][j][k].ToString());
                }
            }
        }
    }

}
