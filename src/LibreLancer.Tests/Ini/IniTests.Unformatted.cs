// Auto-generated code - DO NOT Modify.
// This code was generated with the following command-line:
// 	flini-reader-test Unformatted.ini --csharp

using System.Linq;

using FluentAssertions;
using Xunit;

namespace LibreLancer.Tests.Ini;

public partial class IniTests
{
    [Fact]
    public void UnformattedTest()
    {
        var ini = ParseFile(TestAsset.Open<IniTests>("Unformatted.ini"), false, false).ToList();
        ini[0].Name.Should().Be("  Section One");
        ini[0][0].Name.Should().Be("Key1");
        ini[0][0].Should().HaveCount(1);
        ini[0][0][0].ToString().Should().Be("Value1");
        ini[0][0][0].ToBoolean().Should().Be(false);
        ini[0][0][0].ToInt32().Should().Be(0);
        ini[0][0][0].ToSingle().Should().Be((float)0.000000);
        ini[0][1].Name.Should().Be("Key2");
        ini[0][1].Should().HaveCount(1);
        ini[0][1][0].ToString().Should().Be("Value Two");
        ini[0][1][0].ToBoolean().Should().Be(false);
        ini[0][1][0].ToInt32().Should().Be(0);
        ini[0][1][0].ToSingle().Should().Be((float)0.000000);
        ini[0][2].Name.Should().Be("Key3");
        ini[0][2].Should().HaveCount(1);
        ini[0][2][0].ToString().Should().Be("Value Three");
        ini[0][2][0].ToBoolean().Should().Be(false);
        ini[0][2][0].ToInt32().Should().Be(0);
        ini[0][2][0].ToSingle().Should().Be((float)0.000000);
        ini[0].Count.Should().Be(3);
        ini[1].Name.Should().Be(" Section Two");
        ini[1][0].Name.Should().Be("Key4");
        ini[1][0].Should().HaveCount(1);
        ini[1][0][0].ToString().Should().Be("Value Four");
        ini[1][0][0].ToBoolean().Should().Be(false);
        ini[1][0][0].ToInt32().Should().Be(0);
        ini[1][0][0].ToSingle().Should().Be((float)0.000000);
        ini[1].Count.Should().Be(1);
        ini.Count.Should().Be(2);
    }
}
