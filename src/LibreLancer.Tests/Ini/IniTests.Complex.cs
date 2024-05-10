// Auto-generated code - DO NOT Modify.
// This code was generated with the following command-line:
// 	flini-reader-test Complex.ini --csharp

using System.Linq;

using FluentAssertions;
using Xunit;

namespace LibreLancer.Tests.Ini;

public partial class IniTests
{
    [Fact]
    public void ComplexTest()
    {
        var ini = ParseFile(TestAsset.Open<IniTests>("Complex.ini"), false, false).ToList();
        ini[0].Name.Should().Be("Section1 	!\"$%^&*()-={};'#:@~|\\,./<>?");
        ini[0][0].Name.Should().Be("Key2 	!\"$%^&*()-{}'#:@~|\\,./<>?");
        ini[0][0].Should().HaveCount(1);
        ini[0][0][0].ToString().Should().Be("Value1 	!\"$%^&*()-={}'#:@~|\\./<>?");
        ini[0][0][0].ToBoolean().Should().Be(false);
        ini[0][0][0].ToInt32().Should().Be(0);
        ini[0][0][0].ToSingle().Should().Be((float)0.000000);
        ini[0].Count.Should().Be(1);
        ini.Count.Should().Be(1);
    }
}
