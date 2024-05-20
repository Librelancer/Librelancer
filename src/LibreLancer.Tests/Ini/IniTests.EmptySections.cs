// Auto-generated code - DO NOT Modify.
// This code was generated with the following command-line:
// 	flini-reader-test EmptySections.ini --csharp

using System.Linq;

using FluentAssertions;
using Xunit;

namespace LibreLancer.Tests.Ini;

public partial class IniTests
{
    [Fact]
    public void EmptySectionsTest()
    {
        var ini = ParseFile(TestAsset.Open<IniTests>("EmptySections.ini"), false, false).ToList();
        ini[0].Name.Should().Be("Section1");
        ini[0].Count.Should().Be(0);
        ini[1].Name.Should().Be("Section2");
        ini[1].Count.Should().Be(0);
        ini[2].Name.Should().Be("Section3");
        ini[2].Count.Should().Be(0);
        ini[3].Name.Should().Be("Section4");
        ini[3].Count.Should().Be(0);
        ini.Count.Should().Be(4);
    }
}
