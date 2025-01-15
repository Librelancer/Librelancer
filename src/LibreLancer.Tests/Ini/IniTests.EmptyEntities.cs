// Auto-generated code - DO NOT Modify.
// This code was generated with the following command-line:
// 	Z:\home\cmcging\src\Librelancer\src\LibreLancer.Tests\Ini\TestAssets\flini-reader-test.exe EmptyEntities.ini --csharp

using System.Linq;

using Xunit;

namespace LibreLancer.Tests.Ini;

public partial class IniTests
{
    [Fact]
    public void EmptyEntitiesTest()
    {
        var ini = ParseFile(TestAsset.Open<IniTests>("EmptyEntities.ini"), false, false).ToList();
        Assert.Equal("Section", ini[0].Name);
        Assert.Equal("Key1", ini[0][0].Name);
        Assert.Empty(ini[0][0]);
        Assert.Equal("Key2", ini[0][1].Name);
        Assert.Empty(ini[0][1]);
        Assert.Equal("Key3", ini[0][2].Name);
        Assert.Empty(ini[0][2]);
        Assert.Equal("Ke         y4", ini[0][3].Name);
        Assert.Empty(ini[0][3]);
        Assert.Equal("NoKey", ini[0][4].Name);
        Assert.Empty(ini[0][4]);
        Assert.Equal("", ini[0][5].Name);
        Assert.Empty(ini[0][5]);
        Assert.Equal(6, ini[0].Count);
        Assert.Single(ini);
    }
}
