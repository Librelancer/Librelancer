using LibreLancer.World;

namespace LibreLancer.Client.Components;

public class CLocalPlayerComponent : GameComponent
{
    private CGameSession session;
    public CLocalPlayerComponent(GameObject parent, CGameSession session) : base(parent)
    {
        this.session = session;
    }

    public void BreakFormation()
    {
        session.SpaceRpc.LeaveFormation();
    }

    public void RunDirectiveIndex(int index)
    {
        session.SpaceRpc.RunDirectiveIndex(index);
    }

    public void Dock(GameObject obj)
    {
        session.SpaceRpc.RequestDock(obj);
    }
}
