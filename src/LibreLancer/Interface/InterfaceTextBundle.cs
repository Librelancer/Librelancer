using System;
using System.IO;
using System.Collections.Generic;
using System.IO.Compression;
using System.Text;
using Newtonsoft.Json;

namespace LibreLancer.Interface
{
    //Output of InterfaceEdit, works OK with version control
    public class InterfaceTextBundle
    {
        public SortedDictionary<string, string> db = new SortedDictionary<string, string>();

        private static JsonSerializer _json = JsonSerializer.Create(new JsonSerializerSettings()
        {
            Formatting = Formatting.Indented
        });
        public string ToJSON()
        {
            var writer = new StringWriter();
            _json.Serialize(writer, this);
            return writer.ToString();
        }

        public static InterfaceTextBundle FromJSON(string text)
        {
            var reader = new StringReader(text);
            return (InterfaceTextBundle)_json.Deserialize(reader, typeof(InterfaceTextBundle));
        }
        
        public void AddStringCompressed(string key, string value)
        {
            var bytes = GetBytes(value);
            db.Add(key, Convert.ToBase64String(bytes));
        }
        
        public static byte[] GetBytes(string s)
        {
            using (var strm = new MemoryStream())
            {
                using (var comp = new BrotliStream(strm, CompressionLevel.Optimal, true))
                {
                    var txt = Encoding.UTF8.GetBytes(s);
                    comp.Write(txt, 0, txt.Length);
                }
                return strm.ToArray();
            }
        }

        public string GetStringCompressed(string key)
        {
            using (var strmIn = new MemoryStream(Convert.FromBase64String(db[key])))
            {
                using (var strmOut = new MemoryStream())
                {
                    using (var comp = new BrotliStream(strmIn, CompressionMode.Decompress))
                    {
                        comp.CopyTo(strmOut);
                    }
                    var bytes = strmOut.ToArray();
                    return Encoding.UTF8.GetString(bytes);
                }
            }
        }
    }
}