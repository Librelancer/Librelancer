using System.Linq;
using Xunit;

namespace LibreLancer.Tests.Ini;

public partial class IniTests
{
    [Fact]
    public void EmptyValueTest()
    {
        var ini = ParseFile(TestAsset.Open<IniTests>("EmptyValue.ini"), false, false).ToList();
        Assert.Equal("Cargo", ini[0].Name);
        Assert.Equal(3, ini[0][0].Count);
        Assert.Equal("power_core", ini[0][0][0].ToString());
        Assert.Equal("", ini[0][0][1].ToString());
        Assert.Equal(1, ini[0][0][2].ToInt32());
        Assert.Single(ini[0]);
        Assert.Single(ini);
    }
}
