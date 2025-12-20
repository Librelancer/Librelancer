using System;
using System.Collections.Generic;
using LibreLancer.Data.Ini;

namespace LibreLancer.Data.Schema.RandomMissions;

public enum OfferTextOp
{
    append,
    replace
}

public enum OfferTextType
{
    none,
    singular,
    plural
}

public class OfferTextItem
{
    public OfferTextType Type;
    public int Ids;
    public string[] Args;
}

public class OfferTextEntry
{
    public OfferTextOp Op;
    public OfferTextItem[] Items;

    public static OfferTextEntry FromEntry(Entry e)
    {
        var ot = new OfferTextEntry();
        var itemList = new List<OfferTextItem>();
        ot.Op = Enum.Parse<OfferTextOp>(e[0].ToString());
        for (int i = 1; i < e.Count; i++)
        {
            var item = new OfferTextItem();
            var opOrIds = e[i].ToString().ToLowerInvariant();
            if (opOrIds == "singular")
            {
                item.Type = OfferTextType.singular;
                i++;
            }
            if (opOrIds == "plural")
            {
                item.Type = OfferTextType.plural;
                i++;
            }
            item.Ids = e[i].ToInt32();
            var args = new List<string>();
            while (i + 1 < e.Count)
            {
                if (e[i + 1].TryToInt32(out _))
                    break;
                var op = e[i + 1].ToString();
                if (op.Equals("singular", StringComparison.OrdinalIgnoreCase) ||
                    op.Equals("plural", StringComparison.OrdinalIgnoreCase))
                    break;
                args.Add(op);
                i++;
            }
            item.Args = args.ToArray();
            itemList.Add(item);
        }
        ot.Items = itemList.ToArray();
        return ot;
    }
}
