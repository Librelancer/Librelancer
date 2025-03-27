using System;
using System.IO;
using System.Collections.Generic;
using System.IO.Compression;
using System.Text;
using System.Text.Json;

namespace LibreLancer.Interface
{
    //Output of InterfaceEdit, works OK with version control
    public class InterfaceTextBundle
    {
        public SortedDictionary<string, byte[]> db { get; set; } = new();

        public string ToJSON()
        {
            return JsonSerializer.Serialize(this, new JsonSerializerOptions() {WriteIndented = true});
        }

        public bool Exists(string key)
        {
            return db.ContainsKey(key);
        }

        public static InterfaceTextBundle FromJSON(string text)
        {
            //Quicker to use JsonDocument than to spin up a serializer
            var doc = JsonDocument.Parse(text);
            var tb = new InterfaceTextBundle();
            var db = doc.RootElement.GetProperty("db");
            foreach (var kv in db.EnumerateObject())
            {
                tb.db[kv.Name] = kv.Value.GetBytesFromBase64();
            }
            return tb;
        }

        public void AddStringCompressed(string key, string value)
        {
            var bytes = GetBytes(value);
            db.Add(key, bytes);
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
            using (var strmIn = new MemoryStream(db[key]))
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
