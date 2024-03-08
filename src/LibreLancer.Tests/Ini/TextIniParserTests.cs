// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

using FluentAssertions;
using LibreLancer.Ini;
using Xunit;

namespace LibreLancer.Tests.Ini
{
    public class TextIniParserTests
    {
        private static IList<Section> Parse(string ini)
        {
            var stream = new MemoryStream(Encoding.UTF8.GetBytes(ini));
            var parser = new TextIniParser();
            return parser.ParseIniFile(null, stream).ToList();
        }

        [Fact]
        public void ParseEmptyIniSucceeds()
        {
            var ini = Parse("");
            ini.Should().BeEmpty();
        }

        [Theory]
        [InlineData("[MySection]", "MySection")]
        [InlineData("[=*MySection*=]", "=*MySection*=")]
        [InlineData("[!£$%^&*()#':/?\\|]", "!£$%^&*()#':/?\\|")]
        public void ParseSectionSucceeds(string iniString, string result)
        {
            var ini = Parse(iniString).ToList();
            ini.Should().HaveCount(1);
            var section = ini[0];
            section.Should().HaveCount(0);
            section.Name.Should().Be(result);
        }

        [Fact]
        public void ParseEmptyEntryValueSucceeds()
        {
            var ini = Parse("[MySection]\nMyKey =").ToList();
            ini.Should().HaveCount(1);
            var section = ini[0];
            section.Should().HaveCount(1);
            section.Name.Should().Be("MySection");
            var entry = section[0];
            entry.Should().HaveCount(0);
            entry.Name.Should().Be("MyKey");
        }

        [Theory]
        [InlineData("[MySection")]
        //[InlineData("MySection]")]
        //[InlineData("[\"MySection]")]
        //[InlineData("[MySection\"]")]
        public void ParseIncompleteSectionThrows(string ini)
        {
            Action act = () => { Parse(ini); };
            act.Should().Throw<FileContentException>().WithMessage("*Invalid section header*");
        }

        [Theory]
        [InlineData("[A]\nK=0.0", 0)]
        [InlineData("[A]\nK=1.1", 1.1)]
        [InlineData("[A]\nK=1.11E4", 11100.0)]
        [InlineData("[A]\nK=1.11E-4", 0.000111)]
        [InlineData("[A]\nK=-2147483649", -2147483649)]
        [InlineData("[A]\nK=2147483648", 2147483648)]
        public void ParseSingleValueSucceeds(string iniString, float result)
        {
            var ini = Parse(iniString);
            var value = ini[0][0][0];
            value.Should().BeOfType<SingleValue>();
            value.ToSingle().Should().Be(result);
        }

        [Theory]
        [InlineData("[A]\nK=0", 0)]
        [InlineData("[A]\nK=1", 1)]
        [InlineData("[A]\nK=-2147483648", -2147483648)]
        [InlineData("[A]\nK=2147483647", 2147483647)]
        public void ParseIntegerValueSucceeds(string iniString, int result)
        {
            var ini = Parse(iniString);
            var value = ini[0][0][0];
            value.Should().BeOfType<Int32Value>();
            value.ToInt32().Should().Be(result);
        }

        [Theory]
        [InlineData("[A]\nK=true", true)]
        [InlineData("[A]\nK=True", true)]
        [InlineData("[A]\nK=TRUE", true)]
        [InlineData("[A]\nK=false", false)]
        [InlineData("[A]\nK=False", false)]
        [InlineData("[A]\nK=FALSE", false)]
        public void ParseBooleanValueSucceeds(string iniString, bool result)
        {
            var ini = Parse(iniString);
            var value = ini[0][0][0];
            value.Should().BeOfType<BooleanValue>();
            value.ToBoolean().Should().Be(result);
        }

        [Theory]
        [InlineData("[A]\nK=ImaString", "ImaString")]
        [InlineData("[A]\nK=I'maString", "I'maString")]
        [InlineData("[A]\nK=\"ImaString\"", "\"ImaString\"")]
        [InlineData("[A]\nK=\"I'maString\"", "\"I'maString\"")]
        [InlineData("[A]\nK=1.1.1", "1.1.1")]
        [InlineData("[A]\nK=T", "T")]
        [InlineData("[A]\nK=f", "f")]
        public void ParseStringValueSucceeds(string iniString, string result)
        {
            var ini = Parse(iniString);
            var value = ini[0][0][0];
            value.Should().BeOfType<StringValue>();
            value.ToString().Should().Be(result);
        }

        [Fact]
        public void ParseIncompleteEntryValueIsIgnored()
        {
            var ini = Parse("[MySection]\nMyKey");
            ini.Should().HaveCount(1);
            ini[0].Should().HaveCount(1);
            ini[0][0].Should().HaveCount(0);
        }

        [Fact]
        public void ParseMultipleValueTypesSucceeds()
        {
            var ini = Parse("[Commodities]\niron = 1.42, 300, icons\\iron.bmp, \"+1\"");
            ini.Should().HaveCount(1);
            var section = ini[0];
            section.Name.Should().Be("Commodities");
            section.Should().HaveCount(1);
            var entry = section[0];
            entry.Name.Should().Be("iron");
            entry.Should().HaveCount(4);
            entry[0].Should().BeOfType<SingleValue>();
            entry[0].ToSingle().Should().Be((float)1.42);
            entry[1].Should().BeOfType<Int32Value>();
            entry[1].ToInt32().Should().Be(300);
            entry[2].Should().BeOfType<StringValue>();
            entry[2].ToString().Should().Be("icons\\iron.bmp");
            entry[3].Should().BeOfType<StringValue>();
            entry[3].ToString().Should().Be("\"+1\"");
        }
    }
}
