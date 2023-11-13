using System.Linq;
using LibreLancer.GameData.Items;
using LibreLancer.World;
using LibreLancer.World.Components;

namespace LibreLancer.Client.Components;

public class CPlayerCargoComponent : AbstractCargoComponent
{
    private CGameSession session;
    public CPlayerCargoComponent(GameObject parent, CGameSession session) : base(parent)
    {
        this.session = session;
    }

    public override int TryConsume(Equipment item, int maxCount = 1)
    {
        var slot = session.Items.FirstOrDefault(x => x.Equipment == item);
        if (slot == null) return 0;
        return slot.Count > maxCount ? maxCount : slot.Count;
    }

    public override T FirstOf<T>()
    {
        var slot = session.Items.FirstOrDefault(x => x.Equipment is T);
        return (T) slot?.Equipment;
    }
}
