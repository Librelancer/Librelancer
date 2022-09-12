using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;

namespace LibreLancer
{
    public static partial class NetPacking
    {
        //Tables
        const string DIRECT_CHARS = @"ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
        const int COMMON_PUNC = 24; //first 23 chars can have a space encoded after them
        const int PUNC_SPACE_IDX = 34;
        const string PUNCTUATION = "@$_!\"#%&'()*+,-./:;<=>?{|}`^~\\[]"; 
        const string LATIN_EXTRA =
            "¡¢£¤¥¦§¨©ª«¬®¯°·¸¹º±²³´µ¶»¼½¾¿ÀÁÂÃÄÅÆÇÈÉÊËÌÍÎÏÐÑÒÓÔÕÖ×ØÙÚÛÜÝÞßàáâãäåæçèéêëìíîïðñòóôõö÷øùúûüýþÿ" +
            "ĀāĂăĄąĆćĈĉĊċČčĎďĐđĒēĔĕĖėĘęĚěĜĝĞğĠġĢģĤĥĦħĨĩĪīĬĭĮįİıĲĳĴĵĶķĸĹĺĻļĽľĿŀŁłŃńŅņŇňŉŊŋŌōŎŏŐőŒœŔŕŖŗŘřŚśŜŝŞşŠšŢţŤťŦŧŨũŪūŬŭŮůŰűŲųŴŵŶŷŸŹźŻżŽžſ";
        //Symbols
        const uint SPACE =  62;
        const uint ESCAPE = 63U;
        //Escape symbols
        const uint ESC_REPEAT = 33;
        const uint ESC_NEWLINE = 58;
        const uint ESC_DICT = 59;
        //98 entries max. This can be more optimal
        //Benefit from codebook if pre-dict encoding adds up to 4 chars
        //As the codebook ref takes up 3 chars
        //LATIN_EXTRA = 3 chars
        //Punctuation = 2 chars
        static readonly string[] codebook = { 
            ".thn", ".ini", ":)", "Hello", "hello", 
            "i'm", "I'M", "I'm", "what", "WHAT", 
            "What", "when", "WHEN", "When", "here", 
            "HERE", "Where", "There", "their", "Their", 
            "THEIR", "you're", "You're", "YOU'RE", "n't", 
            "N'T", "Than", "THAN", "than", "Then", 
            "THEN", "then", "These", "these", "Which", 
            "which", "WHICH", "them", "THEM", "Them",
            "Will", "will", "WILL", "tion", "yeah", 
            "Yeah", "YEAH", "mission", "This", "this",
            "With", "with", "more", "your", "Your",
            "ness", "help", "just", "world", "would",
            "Would", "also", "Also", "heiß", "eñ",
            "añ", "ür", "ción", "kön", "müs", 
            "ße", "ance", "ence", "able", "ible",
            "sion", "less", "ious",  "chen", "ling",
            "isch", "rung", "ring", "heit", "keit",
            "schaft", "ship", "equip", "small", "medium",
            "large", "ment", "port", "point", "idle",
            "ué", "ómo", "más" //98 totals
        };
        
        static bool EncodeString(string str, out byte[] data)
        {
            var writer = new BitWriter();
            data = null;
            for (int i = 0; i < str.Length; i++)
            {
                //Codebook (34 max)
                bool usedCodebook = false;
                for (int j = 0; j < codebook.Length; j++) 
                {
                    if (str.AsSpan().Slice(i).StartsWith(codebook[j]))
                    {
                        writer.PutUInt(ESCAPE, 6);
                        int dictIndex = LATIN_EXTRA.Length + j;
                        uint ch = ESC_DICT;
                        while (dictIndex > 63) {
                            ch++;
                            dictIndex -= 64;
                        }
                        writer.PutUInt(ch, 6);
                        writer.PutUInt((uint)dictIndex, 6);
                        i += (codebook[j].Length - 1);
                        usedCodebook = true;
                        break;
                    }
                }
                if (usedCodebook) continue;
                // String packing
                int idx;
                if ((idx = DIRECT_CHARS.IndexOf(str[i])) != -1) {
                    writer.PutUInt((uint)idx, 6);
                }
                else if (str[i] == ' ') {
                    writer.PutUInt(SPACE, 6);
                }
                else if (str[i] == '\n') {
                    writer.PutUInt(ESCAPE, 6);
                    writer.PutUInt(ESC_NEWLINE, 6);
                }
                else {
                    writer.PutUInt(ESCAPE, 6);
                    if ((idx = LATIN_EXTRA.IndexOf(str[i])) != -1)
                    {
                        uint ch = ESC_DICT;
                        while (idx > 63) {
                            ch++;
                            idx -= 64;
                        }
                        writer.PutUInt(ch, 6);
                        writer.PutUInt((uint)idx, 6);
                    }
                    else
                    {
                        char punc = str[i];
                        while (i + 1 < str.Length && str[i + 1] == punc)
                        {
                            writer.PutUInt(ESC_REPEAT, 6);
                            i++;
                        }
                        if ((idx = PUNCTUATION.IndexOf(punc)) == -1)
                        {
                            //Unsupported unicode
                            return false;
                        }
                        if (idx < COMMON_PUNC && i + 1 < str.Length && str[i + 1] == ' ')
                        {
                            writer.PutUInt((uint) (PUNC_SPACE_IDX + idx), 6);
                            i++;
                        }
                        else
                        {
                            writer.PutUInt((uint) idx, 6);
                        }
                    }
                }
            }
            if(!writer.NeedsNewByte(6)) writer.PutUInt(ESCAPE, 6);
            int utf8Count = Encoding.UTF8.GetByteCount(str);
            if (utf8Count > writer.ByteLength)
            {
                data = writer.GetBuffer();
                return true;
            }
            return false;
        }

        
        static string DecodeString(byte[] encoded)
        {
            var reader = new BitReader(encoded, 0);
            StringBuilder builder = new StringBuilder();
            while (reader.BitsLeft >= 6)
            {
                var ch = reader.GetUInt(6);
                if (ch < DIRECT_CHARS.Length)
                    builder.Append(DIRECT_CHARS[(int)ch]);
                else if (ch == SPACE)
                    builder.Append(' ');
                else
                {
                    if (reader.BitsLeft < 6) break; //empty escape at end for padding
                    var esc2 = reader.GetUInt(6);
                    if (esc2 == ESC_NEWLINE)
                    {
                        builder.AppendLine();
                    }
                    else if (esc2 >= ESC_DICT)
                    {
                        uint idxOffset = (esc2 - ESC_DICT) * 64;
                        int dictionaryIndex = (int) (idxOffset + reader.GetUInt(6));
                        if (dictionaryIndex >= LATIN_EXTRA.Length)
                            builder.Append(codebook[dictionaryIndex - LATIN_EXTRA.Length]);
                        else
                            builder.Append(LATIN_EXTRA[dictionaryIndex]);
                    }
                    else
                    {
                        int count = 1;
                        while (esc2 == ESC_REPEAT)
                        {
                            count++;
                            esc2 = reader.GetUInt(6);
                        }

                        var index = (esc2 >= PUNC_SPACE_IDX) ? esc2 - PUNC_SPACE_IDX : esc2;
                        for (int i = 0; i < count; i++) builder.Append(PUNCTUATION[(int) index]);
                        if (esc2 >= PUNC_SPACE_IDX) builder.Append(' ');
                    }
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

        public static bool TryGetStringPacked(this LiteNetLib.Utils.NetDataReader im, out string str, uint maxLength = 2048)
        {
            str = null;
            if (im.AvailableBytes < 1) return false;
            var firstByte = im.PeekByte();
            if (firstByte == 0) { im.SkipBytes(1); return true; }
            if (firstByte == 1) { im.SkipBytes(1); str = ""; return true; }
            var type = (firstByte >> 6);
            uint len;
            if (type == 0 || type == 2)
            {
                len = (uint) ((firstByte & 0x3f) - 1);
                if (len > maxLength) return false;
                if (im.AvailableBytes < len + 1) return false;
                im.SkipBytes(1);
            }
            else
            {
                if (im.AvailableBytes < 64) return false; //63 + im.GetByte()
                int off = 1;
                if (!TryPeekVariableUInt32(im, ref off, out len)) return false;
                len += 63;
                if (len > maxLength) return false;
                if (im.AvailableBytes < off + len) return false;
                im.SkipBytes(off);
            }
            var bytes = im.GetBytes((int)len);
            if (type == 0 || type == 1)
                str = DecodeString(bytes);
            else
                str = Encoding.UTF8.GetString(bytes);
            return true;
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