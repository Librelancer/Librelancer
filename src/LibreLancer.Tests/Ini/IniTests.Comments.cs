// Auto-generated code - DO NOT Modify.
// This code was generated with the following command-line:
// 	flini-reader-test Comments.ini --csharp

using System.Linq;

using FluentAssertions;
using Xunit;

namespace LibreLancer.Tests.Ini;

public partial class IniTests
{
    [Fact]
    public void CommentsTest()
    {
        var ini = ParseFile(TestAsset.Open<IniTests>("Comments.ini"), false, false).ToList();
        ini[0].Name.Should().Be("Section  ;This is a Section");
        ini[0].Count.Should().Be(0);
        ini[1].Name.Should().Be("Section2");
        ini[1].Count.Should().Be(0);
        ini[2].Name.Should().Be("Section3");
        ini[2][0].Name.Should().Be("Key");
        ini[2][0].Should().HaveCount(0);
        ini[2][1].Name.Should().Be("Key");
        ini[2][1].Should().HaveCount(0);
        ini[2][2].Name.Should().Be("Key");
        ini[2][2].Should().HaveCount(0);
        ini[2][3].Name.Should().Be("Key");
        ini[2][3].Should().HaveCount(0);
        ini[2][4].Name.Should().Be("");
        ini[2][4].Should().HaveCount(0);
        ini[2][5].Name.Should().Be("");
        ini[2][5].Should().HaveCount(1);
        ini[2][5][0].ToString().Should().Be("Value");
        ini[2][5][0].ToBoolean().Should().Be(false);
        ini[2][5][0].ToInt32().Should().Be(0);
        ini[2][5][0].ToSingle().Should().Be((float)0.000000);
        ini[2][6].Name.Should().Be("");
        ini[2][6].Should().HaveCount(1);
        ini[2][6][0].ToString().Should().Be("Value");
        ini[2][6][0].ToBoolean().Should().Be(false);
        ini[2][6][0].ToInt32().Should().Be(0);
        ini[2][6][0].ToSingle().Should().Be((float)0.000000);
        ini[2].Count.Should().Be(7);
        ini.Count.Should().Be(3);
    }
}
