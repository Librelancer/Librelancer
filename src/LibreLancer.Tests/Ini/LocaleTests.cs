using System;
using System.Globalization;
using System.IO;
using System.Linq;
using LibreLancer.Data.Ini;
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
            Assert.Single(p);
            Assert.Equal("Section", p[0].Name);
            Assert.Single(p[0]);
            Assert.Equal("Value", p[0][0].Name);
            Assert.Single(p[0][0]);
            Assert.IsAssignableFrom<SingleValue>(p[0][0][0]);
            Assert.Equal(1.1f, p[0][0][0].ToSingle());
        });
    }
}
