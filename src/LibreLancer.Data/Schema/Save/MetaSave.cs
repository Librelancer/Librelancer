using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using LibreLancer.Data.Ini;

namespace LibreLancer.Data.Schema.Save;


public class MetaSave
{
    public string? Filename { get; private set;  }
    public string? Description { get; private set;  }
    public int DescriptionStrid { get; private set;  }
    public DateTime? Timestamp { get; private set;  }

    private MetaSave()
    {
    }

    public static MetaSave FromFile(string filename)
    {
        var c = FlCodec.ReadFile(filename);
        using var stream = new MemoryStream(c);
        var sg = new MetaSave() {Filename = filename};
        foreach (var sec in IniFile.ParseFile(filename, stream, false))
        {
            bool tsSet = false;
            bool descSet = false;

            if (!sec.Name.Equals("player", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            foreach (var e in sec)
            {
                if (e.Name.Equals("descrip_strid", StringComparison.OrdinalIgnoreCase))
                {
                    sg.DescriptionStrid = e[0].ToInt32();
                    descSet = true;
                }
                else if (e.Name.Equals("description", StringComparison.OrdinalIgnoreCase))
                {
                    var bytes = e[0].ToString().SplitInGroups(2).Select(x => byte.Parse(x, NumberStyles.HexNumber)).ToArray();
                    sg.Description = Encoding.BigEndianUnicode.GetString(bytes);
                    descSet = true;

                }
                else if (e.Name.Equals("tstamp", StringComparison.OrdinalIgnoreCase))
                {
                    sg.Timestamp = DateTime.FromFileTime(e[0].ToInt64() << 32 | e[1].ToInt64());
                    tsSet = true;
                }
                if (descSet && tsSet)
                    break;
            }

            break;
        }
        return sg;
    }
}
