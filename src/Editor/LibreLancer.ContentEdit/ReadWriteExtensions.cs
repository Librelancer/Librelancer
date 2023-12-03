using System.IO;
using System.Text;

namespace LibreLancer.ContentEdit;

public static class ReadWriteExtensions
{
    public static void WriteStringUTF8(this BinaryWriter writer, string s)
    {
        if(s == null) {
            writer.Write7BitEncodedInt(0);
        }
        else {
            writer.Write7BitEncodedInt(s.Length + 1);
            var bytes = Encoding.UTF8.GetBytes(s);
            writer.Write(bytes);
        }
    }

    public static string ReadStringUTF8(this BinaryReader reader)
    {
        var len = reader.Read7BitEncodedInt();
        if (len == 0) return null;
        else
        {
            var bytes = reader.ReadBytes(len - 1);
            return Encoding.UTF8.GetString(bytes);
        }
    }
}
