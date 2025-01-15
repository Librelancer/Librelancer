// Auto-generated code - DO NOT Modify.
// This code was generated with the following command-line:
// 	Z:\home\cmcging\src\Librelancer\src\LibreLancer.Tests\Ini\TestAssets\flini-reader-test.exe EmptySections.ini --csharp

using System.Linq;

using Xunit;

namespace LibreLancer.Tests.Ini;

public partial class IniTests
{
    [Fact]
    public void EmptySectionsTest()
    {
        var ini = ParseFile(TestAsset.Open<IniTests>("EmptySections.ini"), false, false).ToList();
        Assert.Equal("Section1", ini[0].Name);
        Assert.Empty(ini[0]);
        Assert.Equal("Section2", ini[1].Name);
        Assert.Empty(ini[1]);
        Assert.Equal("Section3", ini[2].Name);
        Assert.Empty(ini[2]);
        Assert.Equal("Section4", ini[3].Name);
        Assert.Empty(ini[3]);
        Assert.Equal(4, ini.Count);
    }
}
