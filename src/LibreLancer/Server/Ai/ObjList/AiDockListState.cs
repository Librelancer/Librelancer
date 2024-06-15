using LibreLancer.Server.Components;
using LibreLancer.World;
using LibreLancer.World.Components;

namespace LibreLancer.Server.Ai.ObjList;

public class AiDockListState : AiObjListState
{
    private AiDockState dockState;
    private string target;
    //TODO: AI should tradelane specifically in this direction
    private string exit;
    public AiDockListState(string target, string exit)
    {
        this.target = target;
        this.exit = exit;
    }

    public override void OnStart(GameObject obj, SNPCComponent ai)
    {
        var tgt = obj.World.GetObject(target);
        dockState = new AiDockState(tgt);
        dockState.Cruise = false;
        dockState.OnStart(obj, ai);
    }

    public override void Update(GameObject obj, SNPCComponent ai, double dt)
    {
        dockState?.Update(obj, ai, dt);
        if (dockState != null && obj.TryGetComponent<AutopilotComponent>(out var ap))
        {
            if(ap.CurrentBehavior != AutopilotBehaviors.Dock)
                ai.SetState(Next);
        }
    }
}
