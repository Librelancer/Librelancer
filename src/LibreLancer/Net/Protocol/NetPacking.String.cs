using System.Collections.Generic;
using System.Text;

namespace LibreLancer
{
    public static partial class NetPacking
    {
        static readonly string[] codebook =
        {
            "equipment", "explosion", "freighter", "hardpoint", "starboard", "transport", "ambien",
            "generic", "measure", "battle", "cruise", "danger", "engine", "medium", "script", "shield", 
            "trader", "turret", "close", "enter", "extra", "equip", "large", "mount", "music", "small", 
            "space", "trail", ".ini", ".thn", "able", "ance", "base", "body", "intro", "deck", "dock", "dust",
            "fire", "gate", "head", "hole", "hull", "idle", "ight", "info", "item", "jump", "leav", "male",
            "ment", "min", "open", "port", "ring", "ship", "tion", "bar", "ent", "gcs", "gf_", "gun", "hud",
            "ine", "ing", "ist", "mix", "his", "nth", "one", "ous", "pad", "rtc", "sch", "scr", "sfx",
            "shr", "spr", "are", "str", "thr", "tlr", "ye", "an",
            "00", "01", "02", "03", "04", "05", "06", "07", "08", "09",
            "ai", "au", "aw", "ay", "bl", "bw", "ch", "ck", "cl", "cm", "cr", "dr", "dx", "ea", "ed", "ee", "ei", 
            "eq", "er", "eu", "ew", "ey", "fl", "fm", "fr", "gh", "gl", "gr", "hi", "hp", "ie", "li", "ll", "ml",
            "ng", "no", "nt", "oi", "oo", "ou", "ow", "oy", "ph", "pl", "pr", "qu", "rh", "rr", "sc", "sh", "is",
            "sk", "sl", "sm", "sn", "sp", "ss", "st", "sw", "th", "tr", "tt", "tw", "ui", "wh", "wr"
        };
        
        static bool EncodeString(string input, out byte[] output)
        {
            //only compress printable ASCII
            for (int i = 0; i < input.Length; i++)
            {
                if (input[i] < 32 ||
                    input[i] > 126)
                {
                    output = null;
                    return false;
                }
            }
            List<byte> bytes = new List<byte>();
            //pack
            for (int i = 0; i < input.Length; i++)
            {
                bool useCodebook = false;
                for (int j = 0; j < codebook.Length; j++)
                {
                    if (i + codebook[j].Length <= input.Length &&
                        string.Equals(codebook[j], input.Substring(i, codebook[j].Length)))
                    {
                        bytes.Add((byte) (95 + j));
                        i += (codebook[j].Length - 1);
                        useCodebook = true;
                        break;
                    }
                }

                if (useCodebook) continue;
                bytes.Add((byte) (input[i] - 32));
            }

            output = bytes.ToArray();
            return true;
        }
        
        static string DecodeString(byte[] input)
        {
            var reader = new BitReader(input, 0);
            var builder = new StringBuilder();
            for (int i = 0; i < input.Length; i++)
            {
                if (input[i] <= 94)
                {
                    builder.Append((char) (32 + input[i]));
                }
                else
                {
                    builder.Append(codebook[input[i] - 95]);
                }
            }

            return builder.ToString();
        }

        public static void PutStringPacked(this LiteNetLib.Utils.NetDataWriter om, string s)
        {
            if (s == null) {
                om.Put((byte)0);
            } else if (s == "") {
                om.Put((byte)1);
            } else {
                if (EncodeString(s, out byte[] encoded)) {
                    if (encoded.Length < 63) {
                        om.Put((byte)(encoded.Length + 1));
                    } else {
                       om.Put((byte)(1 << 6));  
                       om.PutVariableUInt32((uint)(encoded.Length - 63));
                    }
                    om.Put(encoded);
                }
                else
                {
                    var bytes = Encoding.UTF8.GetBytes(s);
                    if (bytes.Length < 63) {
                        om.Put((byte)(2 << 6 | bytes.Length + 1));
                    } else {
                        om.Put((byte)(3 << 6));
                        om.PutVariableUInt32((uint)(bytes.Length - 63));
                    }
                    om.Put(bytes);
                }
            }
        }
        public static string GetStringPacked(this LiteNetLib.Utils.NetDataReader im)
        {
            var firstByte = im.GetByte();
            if (firstByte == 0) return null;
            if (firstByte == 1) return "";
            var type = (firstByte >> 6);
            int len;
            if (type == 0 || type == 2)
                len = (firstByte & 0x3f) - 1;
            else
                len = (int)im.GetVariableUInt32() + 63;
            var bytes = im.GetBytes(len);
            if (type == 0 || type == 1)
                return DecodeString(bytes);
            else
                return Encoding.UTF8.GetString(bytes);
        }
    }
}