using System;
using System.Buffers;
using System.Text;
using LibreLancer.Data;

namespace LibreLancer.Interface;

public struct IdsFormatItem
{
    public ushort Id;
    public char Category => (char)(Id >> 8);
    public int Index => (int)(Id & 0xFF);

    public string Value;
    public int Ids;

    public readonly string GetString(int variant, InfocardManager infocards) =>
        Value ?? infocards.GetStringResource(Ids + variant);

    public IdsFormatItem(char category, int index, string value)
    {
        Id = (ushort)(((category & 0xFF) << 8) | (index & 0xFF));
        Value = value;
        Ids = 0;
    }

    public IdsFormatItem(char category, int index, int ids)
    {
        Id = (ushort)(((category & 0xFF) << 8) | (index & 0xFF));
        Value = null;
        Ids = ids;
    }
}

public static class IdsFormatting
{
    public static string Format(string format, InfocardManager infocards, IdsFormatItem item0)
    {
        var b = ArrayPool<IdsFormatItem>.Shared.Rent(1);
        b[0] = item0;
        var r = Format(format, infocards, b.AsSpan(0, 1));
        ArrayPool<IdsFormatItem>.Shared.Return(b);
        return r;
    }

    public static string Format(string format, InfocardManager infocards, IdsFormatItem item0, IdsFormatItem item1)
    {
        var b = ArrayPool<IdsFormatItem>.Shared.Rent(2);
        b[0] = item0;
        b[1] = item1;
        var r = Format(format, infocards, b.AsSpan(0, 2));
        ArrayPool<IdsFormatItem>.Shared.Return(b);
        return r;
    }

    public static string Format(string format, InfocardManager infocards, IdsFormatItem item0, IdsFormatItem item1,
        IdsFormatItem item2)
    {
        var b = ArrayPool<IdsFormatItem>.Shared.Rent(3);
        b[0] = item0;
        b[1] = item1;
        b[2] = item2;
        var r = Format(format, infocards, b.AsSpan(0, 3));
        ArrayPool<IdsFormatItem>.Shared.Return(b);
        return r;
    }


    public static string Format(string format, InfocardManager infocards, IdsFormatItem item0, IdsFormatItem item1,
        IdsFormatItem item2, IdsFormatItem item3)
    {
        var b = ArrayPool<IdsFormatItem>.Shared.Rent(4);
        b[0] = item0;
        b[1] = item1;
        b[2] = item2;
        b[3] = item3;
        var r = Format(format, infocards, b.AsSpan(0, 4));
        ArrayPool<IdsFormatItem>.Shared.Return(b);
        return r;
    }

    public static string Format(string format, InfocardManager infocards, ReadOnlySpan<IdsFormatItem> items)
    {
        var sb = new StringBuilder(format.Length);
        for (int i = 0; i < format.Length; i++)
        {
            if (format[i] == '%' && i + 1 < format.Length)
            {
                int index = 0;
                int variant = 0;
                if (format[i + 1] == '%')
                {
                    sb.Append("%");
                    i++;
                    continue;
                }

                char category = format[i + 1];
                //ingest category char
                if (i + 2 < format.Length && format[i + 2] >= '0' && format[i + 2] <= '9')
                {
                    index = format[i + 2] - '0';
                    if (i + 4 < format.Length && format[i + 3] == 'v' && format[i + 4] >= '0' && format[i + 4] <= '9')
                    {
                        variant = format[i + 4] - '0';
                        i += 4; //ingest category + 0v1
                    }
                    else
                    {
                        i += 2; // ingest category + 0
                    }
                }
                else
                {
                    i++; //ingest category
                }

                var searchId = (ushort)(((category & 0xFF) << 8) | (index & 0xFF));
                for (int j = 0; j < items.Length; j++)
                {
                    if (searchId == items[j].Id)
                    {
                        sb.Append(items[j].GetString(variant, infocards));
                        break;
                    }
                }
            }
            else
            {
                sb.Append(format[i]);
            }
        }

        return sb.ToString();
    }
}
