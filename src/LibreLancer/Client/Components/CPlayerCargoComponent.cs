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

    public override bool TryConsume(Equipment item) => session.Items.Any(x => x.Count > 0 && x.Equipment == item);
}