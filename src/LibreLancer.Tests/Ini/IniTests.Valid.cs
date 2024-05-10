// Auto-generated code - DO NOT Modify.
// This code was generated with the following command-line:
// 	flini-reader-test Valid.ini --csharp

using System.Linq;

using FluentAssertions;
using Xunit;

namespace LibreLancer.Tests.Ini;

public partial class IniTests
{
    [Fact]
    public void ValidTest()
    {
        var ini = ParseFile(TestAsset.Open<IniTests>("Valid.ini"), false, false).ToList();
        ini[0].Name.Should().Be("LoggingService");
        ini[0][0].Name.Should().Be("BaseUri");
        ini[0][0].Should().HaveCount(1);
        ini[0][0][0].ToString().Should().Be("https://logging-service.local/api");
        ini[0][0][0].ToBoolean().Should().Be(false);
        ini[0][0][0].ToInt32().Should().Be(0);
        ini[0][0][0].ToSingle().Should().Be((float)0.000000);
        ini[0][1].Name.Should().Be("Port");
        ini[0][1].Should().HaveCount(1);
        ini[0][1][0].ToString().Should().Be("8080");
        ini[0][1][0].ToBoolean().Should().Be(false);
        ini[0][1][0].ToInt32().Should().Be(8080);
        ini[0][1][0].ToSingle().Should().Be((float)8080.000000);
        ini[0].Count.Should().Be(2);
        ini[1].Name.Should().Be("Logging");
        ini[1][0].Name.Should().Be("MinimumLevel");
        ini[1][0].Should().HaveCount(1);
        ini[1][0][0].ToString().Should().Be("Info");
        ini[1][0][0].ToBoolean().Should().Be(false);
        ini[1][0][0].ToInt32().Should().Be(0);
        ini[1][0][0].ToSingle().Should().Be((float)0.000000);
        ini[1][1].Name.Should().Be("File");
        ini[1][1].Should().HaveCount(1);
        ini[1][1][0].ToString().Should().Be("log.txt");
        ini[1][1][0].ToBoolean().Should().Be(false);
        ini[1][1][0].ToInt32().Should().Be(0);
        ini[1][1][0].ToSingle().Should().Be((float)0.000000);
        ini[1][2].Name.Should().Be("MaxSize");
        ini[1][2].Should().HaveCount(1);
        ini[1][2][0].ToString().Should().Be("16MB");
        ini[1][2][0].ToBoolean().Should().Be(false);
        ini[1][2][0].ToInt32().Should().Be(16);
        ini[1][2][0].ToSingle().Should().Be((float)16.000000);
        ini[1].Count.Should().Be(3);
        ini.Count.Should().Be(2);
    }
}
