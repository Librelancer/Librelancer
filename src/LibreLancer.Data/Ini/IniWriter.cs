using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace LibreLancer.Data.Ini;

public static class IniWriter
{
    private const int defaultCodePage = 1252;
    private static readonly Encoding defaultEncoding;
    static IniWriter()
    {
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        defaultEncoding = Encoding.GetEncoding(defaultCodePage);
    }

    public static void WriteIniFile(string outFile, IEnumerable<Section> sections)
    {
        using var stream = File.Create(outFile);
        WriteIni(stream, sections);
    }

    public static void WriteIni(Stream outputStream, IEnumerable<Section> sections)
    {
        using var writer = new StreamWriter(outputStream, defaultEncoding, -1, true);
        writer.NewLine = "\r\n";
        int a = 0;
        foreach (var sec in sections)
        {
            if (a > 0)
                writer.WriteLine();
            a++;
            writer.Write("[");
            writer.Write(sec.Name);
            writer.WriteLine("]");
            foreach (var entry in sec)
            {
                writer.Write(entry.Name);
                if(entry.Count > 0)
                    writer.Write(" = ");
                for (int i = 0; i < entry.Count; i++)
                {
                    if(i > 0)
                        writer.Write(", ");
                    writer.Write(entry[i].ToString());
                }
                writer.WriteLine();
            }
        }

    }

    public static void WriteBini(Stream outputStream, IEnumerable<Section> sections)
    {
        List<string> allStrings = [];
        Dictionary<string, int> stringBlock = new();
        int stringsOffset = 0;

        int StringOffset(string str)
        {
            if (stringBlock.TryGetValue(str, out var offset))
                return offset;
            offset = stringsOffset;
            allStrings.Add(str);
            stringBlock.Add(str, offset);
            stringsOffset += Encoding.UTF8.GetByteCount(str) + 1;
            return offset;
        }

        using var writer = new BinaryWriter(outputStream, defaultEncoding, true);
        writer.Write("BINI"u8);
        writer.Write((uint)1);
        writer.Write((uint)0); //StringBlockOffset
        //Section block here
        //Build string block for section and entry names first, as they're small offsets
        var allSections = sections.ToArray();
        foreach (var s in allSections)
        {
            StringOffset(s.Name);
            foreach (var e in s)
            {
                StringOffset(e.Name);
            }
        }
        //Write sections
        foreach (var s in allSections)
        {
            writer.Write((ushort)StringOffset(s.Name));
            writer.Write((ushort)s.Count);
            foreach (var e in s)
            {
                writer.Write((ushort)StringOffset(e.Name));
                writer.Write((byte)e.Count);
                foreach (var value in e)
                {
                    switch (value)
                    {
                        case BooleanValue boolean:
                            writer.Write((byte)IniValueType.Boolean);
                            writer.Write((bool)boolean);
                            break;
                        case SingleValue single:
                            writer.Write((byte)IniValueType.Single);
                            writer.Write((float)single);
                            break;
                        case Int32Value integer:
                            writer.Write((byte)IniValueType.Int32);
                            writer.Write((int)integer);
                            break;
                        default:
                            writer.Write((byte)IniValueType.String);
                            writer.Write(StringOffset(value.ToString()));
                            break;
                    }
                }
            }
        }
        //Write string block after sections
        var strBlockOffset = (int)writer.BaseStream.Position;
        foreach (var str in allStrings) {
            writer.Write(Encoding.UTF8.GetBytes(str));
            writer.Write((byte)0);
        }
        writer.Seek(8, SeekOrigin.Begin);
        writer.Write(strBlockOffset);
    }
}
