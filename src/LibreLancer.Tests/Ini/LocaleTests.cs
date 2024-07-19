using System;
using System.Globalization;
using System.IO;
using System.Linq;
using LibreLancer.Ini;
using System.Text;
using FluentAssertions;
using Xunit;

namespace LibreLancer.Tests.Ini;

public class LocaleTests
{
    void RunWithLocale(string locale, Action action)
    {
        var currentLocale = CultureInfo.CurrentCulture;
        var locale2 = CultureInfo.GetCultureInfo(locale);
        CultureInfo.CurrentCulture = locale2;
        action();
        CultureInfo.CurrentCulture = currentLocale;
    }

    [Theory]
    [InlineData("en-US")]
    [InlineData("de-DE")]
    [InlineData("es-ES")]
    public void TestDecimalSeparator(string locale)
    {
        RunWithLocale(locale, () =>
        {
            var parser = new LancerTextIniParser();
            var s = new MemoryStream("[Section]\nValue = 1.1"u8.ToArray());

            var p = parser.ParseIniFile("[data]", s, true, false).ToArray();
            p.Should().HaveCount(1);
            p[0].Name.Should().Be("Section");
            p[0].Should().HaveCount(1);
            p[0][0].Name.Should().Be("Value");
            p[0][0].Should().HaveCount(1);
            p[0][0][0].Should().BeOfType<SingleValue>();
            p[0][0][0].ToSingle().Should().Be(1.1f);
        });
    }
}
