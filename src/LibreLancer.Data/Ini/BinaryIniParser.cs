// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System.Collections.Generic;
using System.IO;
using System.Text;

namespace LibreLancer.Ini
{
    public class BinaryIniParser : IIniParser
    {
        private const string BINI = "BINI";
        private const int Version = 1;

        public bool CanParse(Stream stream)
        {
            var buffer = new byte[4];
            stream.Read(buffer, 0, 4);
            stream.Seek(0, SeekOrigin.Begin);
            return Encoding.ASCII.GetString(buffer) == BINI;
        }

        public IEnumerable<Section> ParseIniFile(string path,
            Stream stream,
            bool preparse = true,
            bool allowmaps = false)
        {
            var reader = new BinaryReader(stream);

            // Read Magic
            reader.ReadInt32();

            int formatVersion = reader.ReadInt32();
            if (formatVersion != Version)
                throw new FileVersionException(path, BINI, formatVersion, Version);

            int stringBlockOffset = reader.ReadInt32();
            if (stringBlockOffset > reader.BaseStream.Length)
                throw new FileContentException(path,
                    BINI, "The string block offset was out of range: " + stringBlockOffset);

            long sectionBlockOffset = reader.BaseStream.Position;

            reader.BaseStream.Seek(stringBlockOffset, SeekOrigin.Begin);
            var stringBuffer = new byte[reader.BaseStream.Length - stringBlockOffset];
            reader.Read(stringBuffer, 0, stringBuffer.Length);
            var stringBlock = new BiniStringBlock(Encoding.ASCII.GetString(stringBuffer));

            reader.BaseStream.Seek(sectionBlockOffset, SeekOrigin.Begin);
            while (reader.BaseStream.Position < stringBlockOffset)
            {
                short sectionNameOffset = reader.ReadInt16();
                var sectionName = stringBlock.Get(sectionNameOffset);

                var section = new Section(sectionName) { File = path };

                short entryCount = reader.ReadInt16();
                for (int i = 0; i < entryCount; i++)
                {
                    var entryNameOffset = reader.ReadInt16();
                    var entryName = stringBlock.Get(entryNameOffset);

                    var entry = new Entry(section, entryName);

                    var valueCount = reader.ReadByte();
                    var values = new List<IValue>(valueCount);

                    for (int j = 0; j < valueCount; j++)
                    {
                        var valueType = (IniValueType)reader.ReadByte();
                        switch (valueType)
                        {
                            case IniValueType.Boolean:
                                entry.Add(new BooleanValue(reader) { Entry = entry });
                                break;
                            case IniValueType.Int32:
                                entry.Add(new Int32Value(reader) { Entry = entry });
                                break;
                            case IniValueType.Single:
                                entry.Add(new SingleValue(reader) { Entry = entry });
                                break;
                            case IniValueType.String:
                                entry.Add(new StringValue(reader, stringBlock) { Entry = entry });
                                break;
                            default:
                                throw new FileContentException(path, BINI, "Unknown BINI value type: " + valueType);
                        }
                    }
                    section.Add(entry);
                }

                yield return section;
            }
        }
    }
}
