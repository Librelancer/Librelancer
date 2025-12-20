using System;
using System.Collections.Generic;
using System.Linq;
using LibreLancer.Data.GameData.World;
using LibreLancer.World;

namespace LancerEdit.GameContent.Filters;

public class GameObjectFilters : ObjectFiltering<GameObject>
{
    public GameObjectFilters()
    {
        SetPrefix("base", FilterBase);
        SetPrefix("archetype", FilterArchetype);
        SetPrefix("loadout", FilterLoadout);
        SetPrefix("rep", FilterReputation);
        SetPrefix("goto", FilterGoto);
    }

    static IEnumerable<GameObject> FilterBase(string text, IEnumerable<GameObject> source)
        => source.Where(x => NicknameContains(x.Content().Base, text));


    static IEnumerable<GameObject> FilterArchetype(string text, IEnumerable<GameObject> source)
        => source.Where(x => NicknameContains(x.Content().Archetype, text));

    static IEnumerable<GameObject> FilterLoadout(string text, IEnumerable<GameObject> source) =>
        source.Where(x => NicknameContains(x.Content().Loadout, text));

    static IEnumerable<GameObject> FilterReputation(string text, IEnumerable<GameObject> source) =>
        source.Where(x => NicknameContains(x.Content().Reputation, text));

    static IEnumerable<GameObject> FilterGoto(string text, IEnumerable<GameObject> source) =>
        source.Where(x => x.Content().Dock?.Kind == DockKinds.Jump &&
                          (x.Content().Dock?.Target?.Contains(text, StringComparison.OrdinalIgnoreCase) ?? false));

    protected override IEnumerable<GameObject> DefaultFilter(string text, IEnumerable<GameObject> source)
    {
        return source.Where(x => x.Nickname.Contains(text, StringComparison.OrdinalIgnoreCase));
    }
}
