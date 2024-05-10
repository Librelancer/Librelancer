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
        private static IList<Section> ParseAsAscii(string ini, bool preparse = true, bool allowmaps = false)
        {
            var stream = new MemoryStream(Encoding.ASCII.GetBytes(ini));
            var parser = new LancerTextIniParser();
            return parser.ParseIniFile(null, stream, preparse, allowmaps).ToList();
        }

        private static IList<Section> ParseAsUTF8(string ini, bool preparse = true, bool allowmaps = false)
        {
            var stream = new MemoryStream([.. Encoding.UTF8.GetPreamble(), .. Encoding.UTF8.GetBytes(ini)]);
            var parser = new LancerTextIniParser();
            return parser.ParseIniFile(null, stream, preparse, allowmaps).ToList();
        }

        [Fact]
        public void ParseEmptyIniSucceeds()
        {
            var ini = ParseAsAscii("");
            ini.Should().BeEmpty();
        }

        [Theory]
        [InlineData("[MySection]", "MySection")]
        [InlineData("[=*MySection*=]", "=*MySection*=")]
        public void ParseSectionSucceeds(string iniString, string result)
        {
            var ini = ParseAsAscii(iniString).ToList();
            ini.Should().HaveCount(1);
            var section = ini[0];
            section.Should().HaveCount(0);
            section.Name.Should().Be(result);
        }

        // The UK pound sign is a unicode character and cannot be encoded to ASCII using
        // .NET standard ASCII encoding. By representing it in UTF8 and encoding a BOM in
        // start of the data stream the ini parser should interpret it correctly. 
        [Theory]
        [InlineData("[!£$%^&*()#':/?\\|]", "!£$%^&*()#':/?\\|")]
        public void ParseSectionUtf8Succeeds(string iniString, string result)
        {
            var ini = ParseAsUTF8(iniString).ToList();
            ini.Should().HaveCount(1);
            var section = ini[0];
            section.Should().HaveCount(0);
            section.Name.Should().Be(result);
        }

        [Fact]
        public void ParseEmptyEntryValueSucceeds()
        {
            var ini = ParseAsAscii("[MySection]\nMyKey =").ToList();
            ini.Should().HaveCount(1);
            var section = ini[0];
            section.Should().HaveCount(1);
            section.Name.Should().Be("MySection");
            var entry = section[0];
            entry.Should().HaveCount(0);
            entry.Name.Should().Be("MyKey");
        }

        [Theory]
        [InlineData("[MySection", "MySection")]
        [InlineData("[\"MySection]", "\"MySection")]
        [InlineData("[MySection\"]", "MySection\"")]
        public void ParseIncompleteSectionSucceeds(string iniString, string result)
        {
            var ini = ParseAsAscii(iniString);
            var section = ini[0];
            section.Name.Should().Be(result);
        }

        [Theory]
        [InlineData("[A]\nK=0.0", 0)]
        [InlineData("[A]\nK=1.1", 1.1)]
        [InlineData("[A]\nK=1.11E4", 11100.0)]
        [InlineData("[A]\nK=1.11E+4", 11100.0)]
        [InlineData("[A]\nK=1.11E-4", 0.000111)]
        [InlineData("[A]\nK=-3.4028235E+38", -3.4028235E+38)]
        [InlineData("[A]\nK=3.4028235E+38", 3.4028235E+38)]
        public void ParseSingleValueSucceeds(string iniString, float result)
        {
            var ini = ParseAsAscii(iniString);
            var value = ini[0][0][0];
            value.Should().BeAssignableTo<SingleValue>();
            value.ToSingle().Should().Be(result);
        }

        [Theory]
        [InlineData("[A]\nK=0", 0)]
        [InlineData("[A]\nK=1", 1)]
        [InlineData("[A]\nK=-2147483648", -2147483648)]
        [InlineData("[A]\nK=2147483647", 2147483647)]
        public void ParseIntegerValueSucceeds(string iniString, int result)
        {
            var ini = ParseAsAscii(iniString);
            var value = ini[0][0][0];
            value.Should().BeAssignableTo<Int32Value>();
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
            var ini = ParseAsAscii(iniString);
            var value = ini[0][0][0];
            value.Should().BeAssignableTo<BooleanValue>();
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
            var ini = ParseAsAscii(iniString);
            var value = ini[0][0][0];
            value.Should().BeAssignableTo<StringValue>();
            value.ToString().Should().Be(result);
        }

        [Fact]
        public void ParseIncompleteEntryValueIsIgnored()
        {
            var ini = ParseAsAscii("[MySection]\nMyKey");
            ini.Should().HaveCount(1);
            ini[0].Should().HaveCount(1);
            ini[0][0].Should().HaveCount(0);
        }

        [Fact]
        public void ParseMultipleValueTypesSucceeds()
        {
            var ini = ParseAsAscii("[Commodities]\niron = 1.42, 300, icons\\iron.bmp, \"+1\"");
            ini.Should().HaveCount(1);
            var section = ini[0];
            section.Name.Should().Be("Commodities");
            section.Should().HaveCount(1);
            var entry = section[0];
            entry.Name.Should().Be("iron");
            entry.Should().HaveCount(4);
            entry[0].Should().BeAssignableTo<SingleValue>();
            entry[0].ToSingle().Should().Be((float)1.42);
            entry[1].Should().BeAssignableTo<Int32Value>();
            entry[1].ToInt32().Should().Be(300);
            entry[2].Should().BeAssignableTo<StringValue>();
            entry[2].ToString().Should().Be("icons\\iron.bmp");
            entry[3].Should().BeAssignableTo<StringValue>();
            entry[3].ToString().Should().Be("\"+1\"");
        }

        [Fact]
        public void ParseMapValueNoAllowMapSucceeds()
        {
            var ini = ParseAsAscii("[A]\nname = texture = MaterialNoBendy", preparse: true, allowmaps: false);
            var section = ini[0];
            section[0].Name.Should().Be("name");
            section[0][0].Should().BeAssignableTo<StringValue>();
            section[0][0].ToString().Should().Be("texture = MaterialNoBendy");
        }

        [Fact]
        public void ParseMapValueAllowMapSucceeds()
        {
            var ini = ParseAsAscii("[A]\nname = texture = MaterialNoBendy", preparse: true, allowmaps: true);
            var section = ini[0];
            section[0].Name.Should().Be("name");
            section[0][0].Should().BeAssignableTo<StringKeyValue>();
            section[0][0].ToKeyValue().Key.Should().Be("texture");
            section[0][0].ToKeyValue().Value.Should().Be("MaterialNoBendy");
        }
    }
}
