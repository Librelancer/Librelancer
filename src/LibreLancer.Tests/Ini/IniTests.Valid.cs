// Auto-generated code - DO NOT Modify.
// This code was generated with the following command-line:
// 	Z:\home\cmcging\src\Librelancer\src\LibreLancer.Tests\Ini\TestAssets\flini-reader-test.exe Valid.ini --csharp

using System.Linq;

using Xunit;

namespace LibreLancer.Tests.Ini;

public partial class IniTests
{
    [Fact]
    public void ValidTest()
    {
        var ini = ParseFile(TestAsset.Open<IniTests>("Valid.ini"), false, false).ToList();
        Assert.Equal("LoggingService", ini[0].Name);
        Assert.Equal("BaseUri", ini[0][0].Name);
        Assert.Single(ini[0][0]);
        Assert.Equal("https://logging-service.local/api", ini[0][0][0].ToString());
        Assert.False(ini[0][0][0].ToBoolean());
        Assert.Equal(0, ini[0][0][0].ToInt32());
        Assert.Equal(0.000000f, ini[0][0][0].ToSingle());
        Assert.Equal("Port", ini[0][1].Name);
        Assert.Single(ini[0][1]);
        Assert.Equal("8080", ini[0][1][0].ToString());
        Assert.False(ini[0][1][0].ToBoolean());
        Assert.Equal(8080, ini[0][1][0].ToInt32());
        Assert.Equal(8080.000000f, ini[0][1][0].ToSingle());
        Assert.Equal(2, ini[0].Count);
        Assert.Equal("Logging", ini[1].Name);
        Assert.Equal("MinimumLevel", ini[1][0].Name);
        Assert.Single(ini[1][0]);
        Assert.Equal("Info", ini[1][0][0].ToString());
        Assert.False(ini[1][0][0].ToBoolean());
        Assert.Equal(0, ini[1][0][0].ToInt32());
        Assert.Equal(0.000000f, ini[1][0][0].ToSingle());
        Assert.Equal("File", ini[1][1].Name);
        Assert.Single(ini[1][1]);
        Assert.Equal("log.txt", ini[1][1][0].ToString());
        Assert.False(ini[1][1][0].ToBoolean());
        Assert.Equal(0, ini[1][1][0].ToInt32());
        Assert.Equal(0.000000f, ini[1][1][0].ToSingle());
        Assert.Equal("MaxSize", ini[1][2].Name);
        Assert.Single(ini[1][2]);
        Assert.Equal("16MB", ini[1][2][0].ToString());
        Assert.False(ini[1][2][0].ToBoolean());
        Assert.Equal(16, ini[1][2][0].ToInt32());
        Assert.Equal(16.000000f, ini[1][2][0].ToSingle());
        Assert.Equal(3, ini[1].Count);
        Assert.Equal(2, ini.Count);
    }
}
