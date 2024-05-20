// Auto-generated code - DO NOT Modify.
// This code was generated with the following command-line:
// 	flini-reader-test EmptyEntities.ini --csharp

using System.Linq;

using FluentAssertions;
using Xunit;

namespace LibreLancer.Tests.Ini;

public partial class IniTests
{
    [Fact]
    public void EmptyEntitiesTest()
    {
        var ini = ParseFile(TestAsset.Open<IniTests>("EmptyEntities.ini"), false, false).ToList();
        ini[0].Name.Should().Be("Section");
        ini[0][0].Name.Should().Be("Key1");
        ini[0][0].Should().HaveCount(0);
        ini[0][1].Name.Should().Be("Key2");
        ini[0][1].Should().HaveCount(0);
        ini[0][2].Name.Should().Be("Key3");
        ini[0][2].Should().HaveCount(0);
        ini[0][3].Name.Should().Be("Ke         y4");
        ini[0][3].Should().HaveCount(0);
        ini[0][4].Name.Should().Be("NoKey");
        ini[0][4].Should().HaveCount(0);
        ini[0][5].Name.Should().Be("");
        ini[0][5].Should().HaveCount(0);
        ini[0].Count.Should().Be(6);
        ini.Count.Should().Be(1);
    }
}
