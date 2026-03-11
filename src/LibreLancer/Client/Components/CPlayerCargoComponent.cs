using System.Collections.Generic;
using System.Linq;
using LibreLancer.Data.GameData.Items;
using LibreLancer.Net.Protocol;
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
        return slot == null ? 0 : slot.Count > maxCount ? maxCount : slot.Count;
    }

    public override T? FirstOf<T>() where T : class
    {
        var slot = session.Items.FirstOrDefault(x => x.Equipment is T);
        return (T?) slot?.Equipment;
    }

    public override int TryAdd(Equipment equipment, int maxCount)
    {
        return 0;
    }

    public override IEnumerable<NetShipCargo> GetCargo(int firstId)
    {
        return session.Items.Where(x => string.IsNullOrEmpty(x.Hardpoint))
            .Select(c => new NetShipCargo(c.ID, c.Equipment!.CRC, null, 255, c.Count));
    }
}
