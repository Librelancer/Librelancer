// Auto-generated code - DO NOT Modify.
// This code was generated with the following command-line:
// 	Z:\home\cmcging\src\Librelancer\src\LibreLancer.Tests\Ini\TestAssets\flini-reader-test.exe Complex.ini --csharp

using System.Linq;

using Xunit;

namespace LibreLancer.Tests.Ini;

public partial class IniTests
{
    [Fact]
    public void ComplexTest()
    {
        var ini = ParseFile(TestAsset.Open<IniTests>("Complex.ini"), false, false).ToList();
        Assert.Equal("Section1 	!\"$%^&*()-={};'#:@~|\\,./<>?", ini[0].Name);
        Assert.Equal("Key2 	!\"$%^&*()-{}'#:@~|\\,./<>?", ini[0][0].Name);
        Assert.Single(ini[0][0]);
        Assert.Equal("Value1 	!\"$%^&*()-={}'#:@~|\\./<>?", ini[0][0][0].ToString());
        Assert.False(ini[0][0][0].ToBoolean());
        Assert.Equal(0, ini[0][0][0].ToInt32());
        Assert.Equal(0.000000f, ini[0][0][0].ToSingle());
        Assert.Single(ini[0]);
        Assert.Single(ini);
    }
}
