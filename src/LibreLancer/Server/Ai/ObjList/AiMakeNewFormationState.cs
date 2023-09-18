using System.Linq;
using LibreLancer.Data.Missions;
using LibreLancer.Server.Components;
using LibreLancer.World;
using LibreLancer.World.Components;

namespace LibreLancer.Server.Ai.ObjList
{
    public class AiMakeNewFormationState : AiObjListState
    {
        public string FormationDef;
        public string[] Others;

        public override void OnStart(GameObject obj, SNPCComponent ai)
        {
            obj.Formation?.Remove(obj);
            //TODO: Gross
            var formDef = obj.World.Server.Server.GameData.GetFormation(FormationDef);
            var form = new ShipFormation(obj, formDef);
            obj.Formation = form;
            foreach (var tgt in Others)
            {
                var o = obj.World.GetObject(tgt);
                if (tgt != null) {
                    o.Formation?.Remove(o);
                    form.Add(o);
                    o.Formation = form;
                }
                if (o.TryGetComponent<AutopilotComponent>(out var ap))
                    ap.StartFormation();
            }
            ai.SetState(Next);
        }

        public override void Update(GameObject obj, SNPCComponent ai, double dt)
        {
        }
    }
}
