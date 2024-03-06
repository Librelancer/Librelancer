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
        private const string FileType = "BINI";
        private const int FileVersion = 1;

        public IEnumerable<Section> ParseIniFile(string path, Stream stream, bool preparse = true, bool allowmaps = false)
        {
            var reader = new BinaryReader(stream);

            // Read Magic
            reader.ReadInt32();

            int formatVersion = reader.ReadInt32();
            if (formatVersion != FileVersion) throw new FileVersionException(path, FileType, formatVersion, FileVersion);

            int stringBlockOffset = reader.ReadInt32();
            if (stringBlockOffset > reader.BaseStream.Length) throw new FileContentException(path, FileType, "The string block offset was out of range: " + stringBlockOffset);

            long sectionBlockOffset = reader.BaseStream.Position;

            reader.BaseStream.Seek(stringBlockOffset, SeekOrigin.Begin);
            byte[] stringBuffer = new byte[reader.BaseStream.Length - stringBlockOffset];
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

                    var valueCount = reader.ReadByte();
                    var values = new List<IValue>(valueCount);

                    for (int j = 0; j < valueCount; j++)
                    {
                        var valueType = (IniValueType)reader.ReadByte();
                        switch (valueType)
                        {
                            case IniValueType.Boolean:
                                values.Add(new BooleanValue(reader));
                                break;
                            case IniValueType.Int32:
                                values.Add(new Int32Value(reader));
                                break;
                            case IniValueType.Single:
                                values.Add(new SingleValue(reader));
                                break;
                            case IniValueType.String:
                                values.Add(new StringValue(reader, stringBlock, sectionName, path, -1));
                                break;
                            default:
                                throw new FileContentException(FileType, "Unknown BINI value type: " + valueType);
                        }
                    }
                    var entry = new Entry(entryName, values) { File = path };
                    section.Add(entry);
                }

                yield return section;
            }
        }
    }
}
