// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using LibreLancer.Data.Ini;
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
            Assert.Empty(ini);
        }

        [Theory]
        [InlineData("[MySection]", "MySection")]
        [InlineData("[=*MySection*=]", "=*MySection*=")]
        public void ParseSectionSucceeds(string iniString, string result)
        {
            var ini = ParseAsAscii(iniString).ToList();
            Assert.Single(ini);
            var section = ini[0];
            Assert.Empty(section);
            Assert.Equal(result, section.Name);
        }

        // The UK pound sign is a unicode character and cannot be encoded to ASCII using
        // .NET standard ASCII encoding. By representing it in UTF8 and encoding a BOM in
        // start of the data stream the ini parser should interpret it correctly.
        [Theory]
        [InlineData("[!£$%^&*()#':/?\\|]", "!£$%^&*()#':/?\\|")]
        public void ParseSectionUtf8Succeeds(string iniString, string result)
        {
            var ini = ParseAsUTF8(iniString).ToList();
            Assert.Single(ini);
            var section = ini[0];
            Assert.Empty(section);
            Assert.Equal(result, section.Name);
        }

        [Fact]
        public void ParseEmptyEntryValueSucceeds()
        {
            var ini = ParseAsAscii("[MySection]\nMyKey =").ToList();
            Assert.Single(ini);
            var section = ini[0];
            Assert.Single(section);
            Assert.Equal("MySection", section.Name);
            var entry = section[0];
            Assert.Empty(entry);
            Assert.Equal("MyKey", entry.Name);
        }

        [Theory]
        [InlineData("[MySection", "MySection")]
        [InlineData("[\"MySection]", "\"MySection")]
        [InlineData("[MySection\"]", "MySection\"")]
        public void ParseIncompleteSectionSucceeds(string iniString, string result)
        {
            var ini = ParseAsAscii(iniString);
            var section = ini[0];
            Assert.Equal(result, section.Name);
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
            Assert.IsAssignableFrom<SingleValue>(value);
            Assert.Equal(result, value.ToSingle());
        }

        // This is required for mods loading ALEs
        [Fact]
        public void ParseLongValueWrapSucceeds()
        {
            var ini = ParseAsAscii("[A]\nK=3949920388");
            var value = ini[0][0][0];
            Assert.Equal(-345046908, value.ToInt32());
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
            Assert.IsAssignableFrom<Int32Value>(value);
            Assert.Equal(result, value.ToInt32());
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
            Assert.IsAssignableFrom<BooleanValue>(value);
            Assert.Equal(result, value.ToBoolean());
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
            Assert.IsAssignableFrom<StringValue>(value);
            Assert.Equal(result, value.ToString());
        }

        [Fact]
        public void ParseIncompleteEntryValueIsIgnored()
        {
            var ini = ParseAsAscii("[MySection]\nMyKey");
            Assert.Single(ini);
            Assert.Single(ini[0]);
            Assert.Empty(ini[0][0]);
        }

        [Fact]
        public void ParseMultipleValueTypesSucceeds()
        {
            var ini = ParseAsAscii("[Commodities]\niron = 1.42, 300, icons\\iron.bmp, \"+1\"");
            Assert.Single(ini);
            var section = ini[0];
            Assert.Equal("Commodities", section.Name);
            Assert.Single(section);
            var entry = section[0];
            Assert.Equal("iron", entry.Name);
            Assert.Equal(4, entry.Count);
            Assert.IsAssignableFrom<SingleValue>(entry[0]);
            Assert.Equal((float)1.42, entry[0].ToSingle());
            Assert.IsAssignableFrom<Int32Value>(entry[1]);
            Assert.Equal(300, entry[1].ToInt32());
            Assert.IsAssignableFrom<StringValue>(entry[2]);
            Assert.Equal("icons\\iron.bmp", entry[2].ToString());
            Assert.IsAssignableFrom<StringValue>(entry[3]);
            Assert.Equal("\"+1\"", entry[3].ToString());
        }

        [Fact]
        public void ParseMapValueNoAllowMapSucceeds()
        {
            var ini = ParseAsAscii("[A]\nname = texture = MaterialNoBendy", preparse: true, allowmaps: false);
            var section = ini[0];
            Assert.Equal("name", section[0].Name);
            Assert.IsAssignableFrom<StringValue>(section[0][0]);
            Assert.Equal("texture = MaterialNoBendy", section[0][0].ToString());
        }

        [Fact]
        public void ParseMapValueAllowMapSucceeds()
        {
            var ini = ParseAsAscii("[A]\nname = texture = MaterialNoBendy", preparse: true, allowmaps: true);
            var section = ini[0];
            Assert.Equal("name", section[0].Name);
            Assert.IsAssignableFrom<StringKeyValue>(section[0][0]);
            Assert.Equal("texture", section[0][0].ToKeyValue().Key);
            Assert.Equal("MaterialNoBendy", section[0][0].ToKeyValue().Value);
        }
    }
}
