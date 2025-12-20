using System;
using System.Collections.Generic;
using System.Linq;
using LibreLancer.Data.GameData.World;

namespace LancerEdit.GameContent.Filters;

public class LoadoutFilters : ObjectFiltering<ObjectLoadout>
{
    private HashSet<string> hps = new (StringComparer.OrdinalIgnoreCase);
    public LoadoutFilters(string[] objectHps)
    {
        foreach (var hp in objectHps ?? Array.Empty<string>()) {
            hps.Add(hp);
        }

        SetExtra("compatible", CompatibleFilter);
        SetPrefix("equip", EquipFilter);
    }

    private IEnumerable<ObjectLoadout> EquipFilter(string text, IEnumerable<ObjectLoadout> source)
    {
        foreach(var s in source)
        {
            if(s.Items.Any(s => s.Equipment.Nickname.Contains(text, StringComparison.OrdinalIgnoreCase)) ||
               s.Cargo.Any(s => s.Item.Nickname.Contains(text, StringComparison.OrdinalIgnoreCase)))
                yield return s;
        }
    }

    private IEnumerable<ObjectLoadout> CompatibleFilter(IEnumerable<ObjectLoadout> source)
    {
        foreach (var s in source)
        {
            bool compatible = true;
            foreach (var e in s.Items)
            {
                if (!string.IsNullOrEmpty(e.Hardpoint) && !hps.Contains(e.Hardpoint))
                {
                    compatible = false;
                    break;
                }
            }
            if (compatible)
                yield return s;
        }
    }

    protected override IEnumerable<ObjectLoadout> DefaultFilter(string text, IEnumerable<ObjectLoadout> source)
    {
        return source.Where(x => x.Nickname.Contains(text, StringComparison.OrdinalIgnoreCase));
    }
}
