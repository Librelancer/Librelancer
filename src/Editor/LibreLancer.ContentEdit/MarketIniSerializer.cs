using System;
using System.Collections.Generic;
using System.Linq;
using LibreLancer.Data;
using LibreLancer.Data.GameData;
using LibreLancer.Data.Ini;
using LibreLancer.Data.Schema.Goods;
using Base = LibreLancer.Data.GameData.World.Base;

namespace LibreLancer.ContentEdit;

public static class MarketIniSerializer
{
    public record MarketFile(string Filename, Dictionary<string, List<BaseSoldGood>> BaseGoods);

    public static List<MarketFile> GetMarketFiles(GameItemDb items)
    {
        Dictionary<string, MarketFile> files = new(StringComparer.OrdinalIgnoreCase);

        foreach (var b in items.Bases)
        {
            foreach (var g in b.SoldGoods)
            {
                if (string.IsNullOrWhiteSpace(g.SourceFile))
                    continue;

                if (!files.TryGetValue(g.SourceFile, out var mf))
                {
                    mf = new MarketFile(g.SourceFile, new Dictionary<string, List<BaseSoldGood>>(StringComparer.OrdinalIgnoreCase));
                    files.Add(g.SourceFile, mf);
                }

                if (!mf.BaseGoods.TryGetValue(b.Nickname, out var list))
                {
                    list = [];
                    mf.BaseGoods.Add(b.Nickname, list);
                }
                list.Add(g);
            }
        }

        return files.Values.ToList();
    }

    public static bool ReplaceCommodityMarketGoods(
        List<Section> sections,
        Base b,
        IReadOnlyList<BaseSoldGood> soldGoods,
        Func<string, bool> isCommodity)
    {
        var changed = false;
        var section = sections.FirstOrDefault(x =>
            x.Name.Equals("basegood", StringComparison.OrdinalIgnoreCase) &&
            x["base"] is { Count: > 0 } baseEntry &&
            baseEntry[0].ToString().Equals(b.Nickname, StringComparison.OrdinalIgnoreCase));

        if (section == null)
        {
            if (soldGoods.Count == 0)
                return false;
            section = new Section("basegood");
            section.Add(CreateEntry(section, "base", b.Nickname));
            sections.Add(section);
            changed = true;
        }

        var preserved = section
            .Where(entry => !IsCommodityMarketGoodEntry(entry, isCommodity))
            .ToList();
        changed |= preserved.Count != section.Count;
        section.Clear();
        foreach (var entry in preserved)
            section.Add(entry);

        foreach (var sold in soldGoods
                     .Where(x => x.Good.Ini.Category == GoodCategory.Commodity)
                     .OrderBy(x => x.Good.Nickname))
        {
            section.Add(CreateMarketGoodEntry(section, sold));
            changed = true;
        }

        return changed;
    }

    private static bool IsCommodityMarketGoodEntry(Entry entry, Func<string, bool> isCommodity)
    {
        if (!entry.Name.Equals("marketgood", StringComparison.OrdinalIgnoreCase) || entry.Count == 0)
            return false;
        return isCommodity(entry[0].ToString());
    }

    private static Entry CreateMarketGoodEntry(Section section, BaseSoldGood sold)
    {
        var basePrice = Math.Max(1, sold.Good.Ini.Price);
        var multiplier = (float)(sold.Price / (double)basePrice);
        return CreateEntry(section, "marketgood",
            sold.Good.Nickname,
            sold.Rank,
            sold.Rep,
            sold.ForSale ? 1 : 0,
            sold.ForSale ? 1 : 0,
            0,
            multiplier);
    }

    private static Entry CreateEntry(Section section, string name, params ValueBase[] values)
    {
        var entry = new Entry(section, name);
        foreach (var value in values)
            entry.Add(value);
        return entry;
    }
}
