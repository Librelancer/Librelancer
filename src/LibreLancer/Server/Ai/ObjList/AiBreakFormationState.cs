using LibreLancer.Server.Components;
using LibreLancer.World;

namespace LibreLancer.Server.Ai.ObjList
{

    public class AiBreakFormationState : AiObjListState
    {
        public override void OnStart(GameObject obj, SNPCComponent ai)
        {
            obj.Formation?.Remove(obj);
            ai.SetState(Next);
        }

        public override void Update(GameObject obj, SNPCComponent ai, double dt) { }
    }
}