using System;
using System.Linq;
using LibreLancer.Data.GameData;

namespace LancerEdit.GameContent.Lookups;

public class BodypartLookup : ObjectLookup<Bodypart>
{
    public BodypartLookup(string id, GameDataContext gd, Bodypart initial)
    {
        CreateDropdown(
            id,
            gd.GameData.Items.Bodyparts.OrderBy(x => x.Nickname).Prepend(null),
            x => x?.Nickname ?? "(none)",
            initial);
    }
}
