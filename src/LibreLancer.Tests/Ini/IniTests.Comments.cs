// Auto-generated code - DO NOT Modify.
// This code was generated with the following command-line:
// 	Z:\home\cmcging\src\Librelancer\src\LibreLancer.Tests\Ini\TestAssets\flini-reader-test.exe Comments.ini --csharp

using System.Linq;

using Xunit;

namespace LibreLancer.Tests.Ini;

public partial class IniTests
{
    [Fact]
    public void CommentsTest()
    {
        var ini = ParseFile(TestAsset.Open<IniTests>("Comments.ini"), false, false).ToList();
        Assert.Equal("Section  ;This is a Section", ini[0].Name);
        Assert.Empty(ini[0]);
        Assert.Equal("Section2", ini[1].Name);
        Assert.Empty(ini[1]);
        Assert.Equal("Section3", ini[2].Name);
        Assert.Equal("Key", ini[2][0].Name);
        Assert.Empty(ini[2][0]);
        Assert.Equal("Key", ini[2][1].Name);
        Assert.Empty(ini[2][1]);
        Assert.Equal("Key", ini[2][2].Name);
        Assert.Empty(ini[2][2]);
        Assert.Equal("Key", ini[2][3].Name);
        Assert.Empty(ini[2][3]);
        Assert.Equal("", ini[2][4].Name);
        Assert.Empty(ini[2][4]);
        Assert.Equal("", ini[2][5].Name);
        Assert.Single(ini[2][5]);
        Assert.Equal("Value", ini[2][5][0].ToString());
        Assert.False(ini[2][5][0].ToBoolean());
        Assert.Equal(0, ini[2][5][0].ToInt32());
        Assert.Equal(0.000000f, ini[2][5][0].ToSingle());
        Assert.Equal("", ini[2][6].Name);
        Assert.Single(ini[2][6]);
        Assert.Equal("Value", ini[2][6][0].ToString());
        Assert.False(ini[2][6][0].ToBoolean());
        Assert.Equal(0, ini[2][6][0].ToInt32());
        Assert.Equal(0.000000f, ini[2][6][0].ToSingle());
        Assert.Equal(7, ini[2].Count);
        Assert.Equal(3, ini.Count);
    }
}
