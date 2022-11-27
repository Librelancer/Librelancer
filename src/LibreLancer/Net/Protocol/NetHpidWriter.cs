using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;

namespace LibreLancer.Net.Protocol;

public class NetHpidWriter
{
    private Dictionary<string, uint> indices;
    private List<string> strings = new List<string>();
    private int compressedRevision = -1;
    private int revision = 0;

    public int Revision
    {
        get
        {
            lock (sync) {
                return revision;
            }
        }
    }

    private byte[] compressed;

    public byte[] GetData()
    {
        lock (sync)
        {
            if (revision == compressedRevision) return compressed;
            using var mem = new MemoryStream();
            using (var brotli = new BrotliStream(mem, CompressionLevel.Optimal, true))
            {
                var pw = new PacketWriter();
                pw.PutVariableUInt32((uint)strings.Count);
                for (int i = 0; i < strings.Count; i++) pw.Put(strings[i]);
                var bytes = pw.GetCopy();
                brotli.Write(BitConverter.GetBytes(bytes.Length));
                brotli.Write(bytes);
                brotli.Flush();
            }
            compressed = mem.ToArray();
            compressedRevision = revision;
            return compressed;
        }
    }

    private object sync = new object();
        
    public NetHpidWriter()
    {
        strings = new List<string>(NetPacking.DefaultHpidData);
        indices = new Dictionary<string, uint>(StringComparer.OrdinalIgnoreCase);
        for(int i = 0; i < NetPacking.DefaultHpidData.Length; i++)
            indices.Add(NetPacking.DefaultHpidData[i], (uint)i);
    }

    public event Action<string> OnAddString;

    public uint GetIndex(string str)
    {
        lock (sync)
        {
            if (indices.TryGetValue(str, out uint idx))
                return idx;
            revision++;
            strings.Add(str);
            idx = (uint) indices.Count;
            indices.Add(str, idx);
            OnAddString(str);
            return idx;
        }
    }
}