using System;
using System.Collections.Generic;
using System.Linq;
using LibreLancer.World;

namespace LancerEdit.Filters;

public class GameObjectFilters : ObjectFiltering<GameObject>
{
    public GameObjectFilters()
    {
        SetPrefix("base", FilterBase);
        SetPrefix("archetype", FilterArchetype);
        SetPrefix("loadout", FilterLoadout);
    }

    static IEnumerable<GameObject> FilterBase(string text, IEnumerable<GameObject> source)
        => source.Where(x => NicknameContains(x.Content().Base, text));


    static IEnumerable<GameObject> FilterArchetype(string text, IEnumerable<GameObject> source)
        => source.Where(x => NicknameContains(x.Content().Archetype, text));

    static IEnumerable<GameObject> FilterLoadout(string text, IEnumerable<GameObject> source) =>
        source.Where(x => NicknameContains(x.Content().Loadout, text));
    
    protected override IEnumerable<GameObject> DefaultFilter(string text, IEnumerable<GameObject> source)
    {
        return source.Where(x => x.Nickname.Contains(text, StringComparison.OrdinalIgnoreCase));
    }
}