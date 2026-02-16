using System.Collections.Generic;
using System.Linq;
using LibreLancer.World;

namespace LancerEdit.GameContent.Lookups;

public class SystemObjectLookup(
    IEnumerable<GameObject> objects,
    GameDataContext ctx)
    : ObjectLookup<GameObject>(objects, x =>
{
    if (x == null) return "(none)";
    var idsn = x.SystemObject.IdsName;
    if (idsn != 0)
    {
        return $"{x.Nickname} ({ctx.Infocards.GetStringResource(idsn)})";
    }
    return x.Nickname;
});
