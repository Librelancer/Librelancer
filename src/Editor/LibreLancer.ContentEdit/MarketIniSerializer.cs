using System;
using System.Collections.Generic;
using System.Linq;
using LibreLancer.Data;
using LibreLancer.Data.GameData;
using LibreLancer.Data.Ini;
using LibreLancer.Data.Schema.Goods;

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

    public static IEnumerable<Section> SerializeMarketFile(MarketFile file)
    {
        foreach (var baseGoods in file.BaseGoods.OrderBy(x => x.Key))
        {
            var section = new Section("BaseGood");
            section.Add(CreateEntry(section, "base", baseGoods.Key));

            foreach (var sold in baseGoods.Value.OrderBy(x => x.Good.Nickname))
                section.Add(CreateMarketGoodEntry(section, sold));

            yield return section;
        }
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
