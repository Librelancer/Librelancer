// Auto-generated code - DO NOT Modify.
// This code was generated with the following command-line:
// 	Z:\home\cmcging\src\Librelancer\src\LibreLancer.Tests\Ini\TestAssets\flini-reader-test.exe Nbsp.ini --csharp

using System.Linq;

using Xunit;

namespace LibreLancer.Tests.Ini;

public partial class IniTests
{
    [Fact]
    public void NbspTest()
    {
        var ini = ParseFile(TestAsset.Open<IniTests>("Nbsp.ini"), false, false).ToList();
        Assert.Equal("nbsp\u00A0\u00A0\u00A0\u00A0\u00A0\u00A0\u00A0\u00A0", ini[0].Name);
        Assert.Equal("locked_\u00A0\u00A0\u00A0\u00A0\u00A0\u00A0\u00A0\u00A0 gate\u00A0\u00A0\u00A0\u00A0\u00A0\u00A0\u00A0\u00A0", ini[0][0].Name);
        Assert.Single(ini[0][0]);
        Assert.Equal("2926089285", ini[0][0][0].ToString());
        Assert.False(ini[0][0][0].ToBoolean());
        Assert.Equal(-1368878011, ini[0][0][0].ToInt32());
        Assert.Equal(2926089285.000000f, ini[0][0][0].ToSingle());
        Assert.Single(ini[0]);
        Assert.Equal("\u00A0\u00A0\u00A0\u00A0\u00A0\u00A0\u00A0\u00A0nbsp\u00A0\u00A0\u00A0\u00A0\u00A0\u00A0\u00A0\u00A0", ini[1].Name);
        Assert.Equal("lock_\u00A0\u00A0\u00A0\u00A0\u00A0\u00A0\u00A0\u00A0 gate\u00A0\u00A0\u00A0\u00A0\u00A0\u00A0\u00A0\u00A0", ini[1][0].Name);
        Assert.Empty(ini[1][0]);
        Assert.Single(ini[1]);
        Assert.Equal(2, ini.Count);
    }
}
