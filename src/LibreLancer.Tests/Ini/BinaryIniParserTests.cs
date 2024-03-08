// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

using FluentAssertions;
using LibreLancer.Ini;
using Xunit;

namespace LibreLancer.Tests.Ini
{
    public class BinaryIniParserTests
    {
        private static IList<Section> Parse(byte[] bini)
        {
            var stream = new MemoryStream(bini);
            var parser = new BinaryIniParser();
            return parser.ParseIniFile(null, stream).ToList();
        }

        [Fact]
        public void ParseEmptyBiniSucceeds()
        {
            var bini = new byte[] { 0x42, 0x49, 0x4e, 0x49, // magic("BINI")
                                    0x1, 0x0, 0x0, 0x0,     // version(1)
                                    0xc, 0x0, 0x0, 0x0 };   // textoff(0xc)

            var ini = Parse(bini);
            ini.Should().BeEmpty();
        }

        [Fact]
        public void ParseBiniBadVersionThrows()
        {
            var bini = new byte[] { 0x42, 0x49, 0x4e, 0x49, // magic("BINI")
                                    0x0, 0x0, 0x0, 0x0,     // version(0) - invalid version
                                    0xc, 0x0, 0x0, 0x0 };   // textoff(0xc)

            Action act = () => { Parse(bini); };
            act.Should().Throw<FileVersionException>().WithMessage("*version 1 was expected but a vesion 0*");
        }

        [Fact]
        public void ParseBiniTextOffsetGreaterThanLengthThrows()
        {
            var bini = new byte[] { 0x42, 0x49, 0x4e, 0x49, // magic("BINI")
                                    0x1, 0x0, 0x0, 0x0,     // version(1)
                                    0xd, 0x0, 0x0, 0x0 };   // textoff(0xd) - invalid offset

            Action act = () => { Parse(bini); };
            act.Should().Throw<FileContentException>().WithMessage("*out of range*");
        }

        [Fact]
        public void ParseBiniBadValueTypeThrows()
        {
            var bini = new byte[] { 0x42, 0x49, 0x4e, 0x49, // magic("BINI")
                                    0x1, 0x0, 0x0, 0x0,     // version(1)
                                    0x18, 0x0, 0x0, 0x0,    // textoff(0x18)
                                                            // Data Section
                                    0x0, 0x0,               // sectionName(A)
                                    0x1, 0x0,               // numEntries(1)
                                    0x0, 0x0,               // entryName(A)
                                    0x1,                    // numValues(1)
                                    0x4,                    // type - bad type
                                    0x0, 0x0, 0x0, 0x0,     // value(0)
                                                            // Text Section
                                    0x48, 0x0 };            // "A"

            Action act = () => { Parse(bini); };
            act.Should().Throw<FileContentException>().WithMessage("*value type*");
        }

        [Fact]
        public void ParseBasicBiniSucceeds()
        {
            var bini = new byte[] { 0x42, 0x49, 0x4e, 0x49, // magic("BINI")
                                    0x1, 0x0, 0x0, 0x0,     // version(1)
                                    0x18, 0x0, 0x0, 0x0,    // textoff(0x18)
                                                            // Data Section
                                    0x0, 0x0,               // sectionName("MySection")
                                    0x1, 0x0,               // numEntries(1)
                                    0xa, 0x0,               // entryName("MyKey")
                                    0x1,                    // numValues(1)
                                    0x3,                    // type(string)
                                    0x10, 0x0, 0x0, 0x0,    // value("MyValue")
                                                            // Text Section
                                    0x4d, 0x79, 0x53, 0x65, 0x63, 0x74, 0x69, 0x6f, 0x6e, 0x0,  // "MySection"
                                    0x4d, 0x79, 0x4b, 0x65, 0x79, 0x0,                          // "MyKey"
                                    0x4d, 0x79, 0x56, 0x61, 0x6c, 0x75, 0x65, 0x0 };            // "MyValue"

            var ini = Parse(bini);
            ini.Should().HaveCount(1);
            var section = ini[0];
            section.Should().HaveCount(1);
            section.Name.Should().Be("MySection");
            var entry = section[0];
            entry.Should().HaveCount(1);
            entry.Name.Should().Be("MyKey");
            var value = entry[0];
            value.ToString().Should().Be("MyValue");
        }
    }
}
