using System.Linq;
using LibreLancer.World;

namespace LancerEdit.GameContent.Lookups;

public class HardpointLookup(GameObject obj)
    : ObjectLookup<Hardpoint>(obj.GetHardpoints(), x => x.Name);
