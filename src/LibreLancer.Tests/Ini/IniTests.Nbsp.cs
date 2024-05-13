// Auto-generated code - DO NOT Modify.
// This code was generated with the following command-line:
// 	flini-reader-test Nbsp.ini --csharp

using System.Linq;

using FluentAssertions;
using Xunit;

namespace LibreLancer.Tests.Ini;

public partial class IniTests
{
    [Fact]
    public void NbspTest()
    {
        var ini = ParseFile(TestAsset.Open<IniTests>("Nbsp.ini"), false, false).ToList();
        ini[0].Name.Should().Be("nbsp\u00A0\u00A0\u00A0\u00A0\u00A0\u00A0\u00A0\u00A0");
        ini[0][0].Name.Should().Be("locked_\u00A0\u00A0\u00A0\u00A0\u00A0\u00A0\u00A0\u00A0 gate\u00A0\u00A0\u00A0\u00A0\u00A0\u00A0\u00A0\u00A0");
        ini[0][0].Should().HaveCount(1);
        ini[0][0][0].ToString().Should().Be("2926089285");
        ini[0][0][0].ToBoolean().Should().Be(false);
        ini[0][0][0].ToInt32().Should().Be(-1368878011);
        ini[0][0][0].ToSingle().Should().Be((float)2926089285.000000);
        ini[0].Count.Should().Be(1);
        ini[1].Name.Should().Be("\u00A0\u00A0\u00A0\u00A0\u00A0\u00A0\u00A0\u00A0nbsp\u00A0\u00A0\u00A0\u00A0\u00A0\u00A0\u00A0\u00A0");
        ini[1][0].Name.Should().Be("lock_\u00A0\u00A0\u00A0\u00A0\u00A0\u00A0\u00A0\u00A0 gate\u00A0\u00A0\u00A0\u00A0\u00A0\u00A0\u00A0\u00A0");
        ini[1][0].Should().HaveCount(0);
        ini[1].Count.Should().Be(1);
        ini.Count.Should().Be(2);
    }
}
