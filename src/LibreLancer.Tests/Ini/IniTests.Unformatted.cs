// Auto-generated code - DO NOT Modify.
// This code was generated with the following command-line:
// 	Z:\home\cmcging\src\Librelancer\src\LibreLancer.Tests\Ini\TestAssets\flini-reader-test.exe Unformatted.ini --csharp

using System.Linq;

using Xunit;

namespace LibreLancer.Tests.Ini;

public partial class IniTests
{
    [Fact]
    public void UnformattedTest()
    {
        var ini = ParseFile(TestAsset.Open<IniTests>("Unformatted.ini"), false, false).ToList();
        Assert.Equal("  Section One", ini[0].Name);
        Assert.Equal("Key1", ini[0][0].Name);
        Assert.Single(ini[0][0]);
        Assert.Equal("Value1", ini[0][0][0].ToString());
        Assert.False(ini[0][0][0].ToBoolean());
        Assert.Equal(0, ini[0][0][0].ToInt32());
        Assert.Equal(0.000000f, ini[0][0][0].ToSingle());
        Assert.Equal("Key2", ini[0][1].Name);
        Assert.Single(ini[0][1]);
        Assert.Equal("Value Two", ini[0][1][0].ToString());
        Assert.False(ini[0][1][0].ToBoolean());
        Assert.Equal(0, ini[0][1][0].ToInt32());
        Assert.Equal(0.000000f, ini[0][1][0].ToSingle());
        Assert.Equal("Key3", ini[0][2].Name);
        Assert.Single(ini[0][2]);
        Assert.Equal("Value Three", ini[0][2][0].ToString());
        Assert.False(ini[0][2][0].ToBoolean());
        Assert.Equal(0, ini[0][2][0].ToInt32());
        Assert.Equal(0.000000f, ini[0][2][0].ToSingle());
        Assert.Equal(3, ini[0].Count);
        Assert.Equal(" Section Two", ini[1].Name);
        Assert.Equal("Key4", ini[1][0].Name);
        Assert.Single(ini[1][0]);
        Assert.Equal("Value Four", ini[1][0][0].ToString());
        Assert.False(ini[1][0][0].ToBoolean());
        Assert.Equal(0, ini[1][0][0].ToInt32());
        Assert.Equal(0.000000f, ini[1][0][0].ToSingle());
        Assert.Single(ini[1]);
        Assert.Equal(2, ini.Count);
    }
}
