using System;

using System.Collections.Generic;
using System.IO;
using LibreLancer;
using LibreLancer.ContentEdit;

namespace LancerEdit;

public static class UtfClipboard
{
    public static LUtfNode FromBytes(byte[] array)
    {
        #if !DEBUG
        try
        {
        #endif
            if (array == null)
                return null;
            using var ms = new MemoryStream(array);
            using var reader = new BinaryReader(ms);
            if (reader.ReadUInt32() != 0xCAFEBABE)
                return null;
            return ReadNode(reader);
            #if !DEBUG
        }
        catch (Exception)
        {
            return null;
        }
        #endif
    }

    static LUtfNode ReadNode(BinaryReader reader)
    {
        var l = new LUtfNode();
        l.Name = reader.ReadStringUTF8();
        l.ResolvedName = reader.ReadStringUTF8();
        var type = reader.ReadByte();
        if (type == 1)
        {
            var count = reader.Read7BitEncodedInt();
            l.Children = new List<LUtfNode>();
            for (int i = 0; i < count; i++)
            {
                var c = ReadNode(reader);
                c.Parent = l;
                l.Children.Add(c);
            }
        }
        else if(type == 2)
        {
            var count = reader.Read7BitEncodedInt();
            l.Data = reader.ReadBytes(count);
        }
        return l;
    }

    public static byte[] ToBytes(LUtfNode node)
    {
        using var ms = new MemoryStream();
        using var writer = new BinaryWriter(ms);
        writer.Write(0xCAFEBABEU);
        WriteNode(node, writer);
        return ms.ToArray();
    }

    static void WriteNode(LUtfNode n, BinaryWriter writer)
    {
        writer.WriteStringUTF8(n.Name);
        writer.WriteStringUTF8(n.ResolvedName);
        if (n.Children != null)
        {
            writer.Write((byte)1);
            writer.Write7BitEncodedInt(n.Children.Count);
            foreach(var child in n.Children)
                WriteNode(child, writer);
        }
        else if (n.Data != null)
        {
            writer.Write((byte)2);
            writer.Write7BitEncodedInt(n.Data.Length);
            writer.Write(n.Data);
        }
        else
        {
            writer.Write((byte)0);
        }
    }
}
