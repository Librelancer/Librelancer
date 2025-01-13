using System;
using System.Buffers;
using System.Text;
using LibreLancer.Data;

namespace LibreLancer.Interface;

public struct IdsFormatItem
{
    public readonly ushort Id;
    public readonly string Value;
    public readonly int Ids;

    public readonly char Category => (char)(Id >> 8);
    public readonly int Index => (int)(Id & 0xFF);

    private const int FACTION_COUNT = 100;
    private const int FACTION_OFFSET = 131834;
    private const int FACTION_MIN = 196846;
    private const int FACTION_MAX = FACTION_MIN + FACTION_COUNT;
    private const int ZONE_COUNT = 200;
    private const int ZONE_OFFSET = 70473;
    private const int ZONE_MIN = 261208;
    private const int ZONE_MAX = ZONE_MIN + ZONE_COUNT;

    public readonly string GetString(int variant, InfocardManager infocards)
    {
        if (Value != null)
            return Value;
        int id = Ids;
        if (variant >= 0)
        {
            if (Category == 'F' &&
                (Ids >= FACTION_MIN && Ids <= FACTION_MAX))
            {
                id = Ids + FACTION_OFFSET + variant * FACTION_COUNT;
            }
            else if (Category == 'Z' &&
                     (Ids >= ZONE_MIN && Ids <= ZONE_MAX))
            {
                id = Ids + ZONE_OFFSET + variant * ZONE_COUNT;
            }
            else
            {
                id = Ids + variant;
            }
        }
        return infocards?.GetStringResource(id) ?? "(NULL)";
    }


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
        int idxLast = 0;
        int idxPct = format.IndexOf('%');
        if (idxPct == -1)
        {
            return format;
        }

        var sb = new StringBuilder(format.Length);
        while (idxPct != -1)
        {
            sb.Append(format.AsSpan(idxLast, idxPct - idxLast));
            if (idxPct + 1 < format.Length)
            {
                if (format[idxPct + 1] == '%')
                {
                    sb.Append('%');
                    idxLast = idxPct + 2;
                    idxPct = format.IndexOf('%', idxLast);
                    continue;
                }

                int index = 0;
                int variant = -1;
                char category = format[idxPct + 1];
                if (idxPct + 2 < format.Length && format[idxPct + 2] >= '0' && format[idxPct + 2] <= '9')
                {
                    index = format[idxPct + 2] - '0';
                    if (idxPct + 4 < format.Length
                        && format[idxPct + 3] == 'v'
                        && format[idxPct + 4] >= '0' && format[idxPct + 4] <= '9')
                    {
                        variant = format[idxPct + 4] - '0';
                        idxLast = idxPct + 5; //ingest category + 0v1
                    }
                    else
                    {
                        idxLast = idxPct + 3; // ingest category + 0
                    }
                }
                else
                {
                    idxLast = idxPct + 2;
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

                idxPct = format.IndexOf('%', idxLast);
            }
            else
            {
                sb.Append('%');
                idxLast = format.Length;
                idxPct = -1;
            }
        }

        if (idxLast < format.Length)
        {
            sb.Append(format.AsSpan(idxLast));
        }

        return sb.ToString();
    }
}
