using System.Collections.Generic;
using System.Linq;
using LibreLancer.World;

namespace LancerEdit.GameContent.Lookups;

public class SystemObjectLookup : ObjectLookup<GameObject>
{
    public SystemObjectLookup(string id, IEnumerable<GameObject> objects, GameDataContext ctx, GameObject initial)
    {
        CreateDropdown(id,
            objects.Prepend(null),
            x =>
            {
                if (x == null) return "(none)";
                var idsn = x.Content().IdsName;
                if (idsn != 0)
                {
                    return $"{x.Nickname} ({ctx.Infocards.GetStringResource(idsn)})";
                }
                return x.Nickname;
            }, initial);
    }
}
