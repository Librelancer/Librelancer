using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using LibreLancer.Graphics.Text;
using LibreLancer.Interface;

namespace LibreLancer.Net.Protocol;

public enum ChatMessageSize
{
    Regular = 0,
    Small = 1,
    Large = 2,
    XLarge = 3,
}
public class BinaryChatSegment
{
    public bool? Bold;
    public bool? Italic;
    public bool? Underline;
    public OptionalColor Color;
    public ChatMessageSize Size;
    public string Contents;
}

public class BinaryChatMessage
{
    public List<BinaryChatSegment> Segments = new List<BinaryChatSegment>();

    public override string ToString()
    {
        var builder = new StringBuilder();
        foreach (var s in Segments)
            builder.Append(s.Contents);
        return builder.ToString();
    }

    public static BinaryChatMessage Read(PacketReader reader)
    {
        var msg = new BinaryChatMessage();
        byte type = reader.GetByte();
        List<byte> bytes = new List<byte>();
        while (type != 0)
        {
            if (type != 0xFF && type != 0xFE)
                throw new InvalidDataException();
            var seg = new BinaryChatSegment();
            var flags = reader.GetByte();
            seg.Size = (ChatMessageSize) (flags >> 6 & 0x3);
            if ((flags & 0x1) != 0)
                seg.Bold = (flags & 0x2) != 0;
            if ((flags & 0x4) != 0)
                seg.Italic = (flags & 0x8) != 0;
            if ((flags & 0x10) != 0)
                seg.Underline = (flags & 0x10) != 0;
            if (type == 0xFF)
            {
                seg.Color = new OptionalColor(new Color4(reader.GetByte(), reader.GetByte(), reader.GetByte(), 255));
            }
            type = reader.GetByte();
            while (type != 0xFE && type != 0xFF && type != 0)
            {
                bytes.Add(type);
                type = reader.GetByte();
            }
            seg.Contents = Encoding.UTF8.GetString(bytes.ToArray());
            bytes.Clear();
            msg.Segments.Add(seg);
        }
        return msg;
    }

    public void Put(PacketWriter writer)
    {
        foreach (var seg in Segments) {
            writer.Put(seg.Color.Enabled ? (byte)0xFF : (byte)0xFE);
            var flags = (byte)(((int)seg.Size << 6) & 0xC0);
            if (seg.Bold.HasValue) {
                flags |= 0x1;
                if (seg.Bold.Value)
                    flags |= 0x2;
            }
            if (seg.Italic.HasValue)
            {
                flags |= 0x4;
                if (seg.Italic.Value)
                    flags |= 0x8;
            }
            if (seg.Underline.HasValue)
            {
                flags |= 0x10;
                if (seg.Underline.Value)
                    flags |= 0x20;
            }
            writer.Put(flags);
            if (seg.Color.Enabled)
            {
                writer.Put((byte)(seg.Color.Color.R * 255));
                writer.Put((byte)(seg.Color.Color.G * 255));
                writer.Put((byte)(seg.Color.Color.B * 255));
            }
            var bytes = Encoding.UTF8.GetBytes(seg.Contents);
            writer.Put(bytes, 0, bytes.Length);
        }
        writer.Put((byte)0);
    }

    public static BinaryChatMessage PlainText(string text) =>
        new BinaryChatMessage()
        {
            Segments = new List<BinaryChatSegment>()
            {
                new BinaryChatSegment() { Contents = text }
            }
        };

    public static BinaryChatMessage ParseBbCode(string code)
    {
        int boldCount = 0;
        int italicCount = 0;
        int underlineCount = 0;
        Stack<OptionalColor> colors = new Stack<OptionalColor>();
        Stack<ChatMessageSize> sizes = new Stack<ChatMessageSize>();
        List<BinaryChatSegment> segments = new List<BinaryChatSegment>();

        BinaryChatSegment current = null;
        StringBuilder builder = null;

        void Push()
        {
            if(current != null) {
                segments.Add(current);
                current.Contents = builder.ToString();
                current = null;
                builder = null;
            }
        }

        void New()
        {
            if (current == null)
            {
                current = new BinaryChatSegment();
                if (colors.Count > 0)
                    current.Color = colors.Peek();
                if (sizes.Count > 0)
                    current.Size = sizes.Peek();
                if (boldCount > 0)
                    current.Bold = true;
                if (italicCount > 0)
                    current.Italic = true;
                if (underlineCount > 0)
                    current.Underline = true;
                builder = new StringBuilder();
            }
        }

        Span<char> lower = stackalloc char[64];


        for(int i = 0; i < code.Length; i++) {
            if (code[i] == '[')
            {
                var tag = code.AsSpan(i);
                if (tag.StartsWith("[b]"))
                {
                    Push();
                    boldCount++;
                    i += 2;
                    continue;
                }
                if (tag.StartsWith("[/b]"))
                {
                    Push();
                    boldCount--;
                    i += 3;
                    continue;
                }
                if (tag.StartsWith("[i]")) {
                    Push();
                    italicCount++;
                    i += 2;
                    continue;
                }
                if (tag.StartsWith("[/i]"))
                {
                    Push();
                    italicCount--;
                    i += 3;
                    continue;
                }
                if(tag.StartsWith("[u]"))
                {
                    Push();
                    underlineCount++;
                    i += 2;
                    continue;
                }
                if (tag.StartsWith("[/u]"))
                {
                    Push();
                    underlineCount--;
                    i += 3;
                    continue;
                }

                if (tag.StartsWith("[color=") ||
                    tag.StartsWith("[color ="))
                {
                    var end = tag.IndexOf(']');
                    if (end != -1)
                    {
                        var eq = tag.IndexOf('=');
                        var val = tag.Slice(eq + 1, (end - eq - 1));
                        if (Parser.TryParseColor(val.ToString(), out var col))
                        {
                            colors.Push(new OptionalColor(col));
                            i += end;
                            continue;
                        }
                    }
                }

                if (tag.StartsWith("[size=") ||
                    tag.StartsWith("[size ="))
                {
                    var end = tag.IndexOf(']');
                    if (end != -1)
                    {
                        var eq = tag.IndexOf('=');
                        var val = tag.Slice(eq + 1, (end - eq - 1));
                        var trimmed = val.Trim();
                        var x = trimmed.ToLowerInvariant(lower);
                        if(x != -1) {
                            switch (lower.Slice(0, x))
                            {
                                case "small":
                                    Push();
                                    sizes.Push(ChatMessageSize.Small);
                                    i += end;
                                    continue;
                                case "regular":
                                case "medium":
                                    Push();
                                    sizes.Push(ChatMessageSize.Regular);
                                    i += end;
                                    continue;
                                case "large":
                                    Push();
                                    sizes.Push(ChatMessageSize.Large);
                                    i += end;
                                    continue;
                                case "x-large":
                                case "xlarge":
                                    Push();
                                    sizes.Push(ChatMessageSize.XLarge);
                                    i += end;
                                    continue;
                            }

                        }
                    }
                }
                if (tag.StartsWith("[/color]"))
                {
                    Push();
                    colors.Pop();
                    i += 7;
                    continue;
                }
                if (tag.StartsWith("[/size]"))
                {
                    Push();
                    sizes.Pop();
                    i += 6;
                    continue;
                }
            }
            New();
            builder.Append(code[i]);
        }
        Push();
        return new BinaryChatMessage() {Segments = segments};
    }
}
