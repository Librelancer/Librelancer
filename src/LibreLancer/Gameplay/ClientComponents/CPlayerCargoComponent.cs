using System.Linq;
using LibreLancer.GameData.Items;

namespace LibreLancer;

public class CPlayerCargoComponent : AbstractCargoComponent
{
    private CGameSession session;
    public CPlayerCargoComponent(GameObject parent, CGameSession session) : base(parent)
    {
        this.session = session;
    }

    public override bool TryConsume(Equipment item) => session.Items.Any(x => x.Count > 0 && x.Equipment == item);
}