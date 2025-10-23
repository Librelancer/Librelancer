using System.Linq;
using LibreLancer.World;

namespace LancerEdit.GameContent.Lookups;

public class HardpointLookup : ObjectLookup<Hardpoint>
{
    public HardpointLookup(string id, GameObject obj)
    {
        CreateDropdown(id, obj.GetHardpoints(), x => x.Name, null);
    }
}
