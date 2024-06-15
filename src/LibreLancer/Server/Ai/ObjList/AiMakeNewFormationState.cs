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
            //TODO: Gross
            var formDef = obj.World.Server.Server.GameData.GetFormation(FormationDef);
            GameObject player = null;
            bool playerLead = false;
            // Preserve player (required)
            if (obj.Formation != null) {
                if (obj.Formation.LeadShip.TryGetComponent<SPlayerComponent>(out _))
                {
                    player = obj.Formation.LeadShip;
                    playerLead = true;
                }
                else {
                    foreach (var x in obj.Formation.Followers)
                    {
                        if (x.TryGetComponent<SPlayerComponent>(out _))
                        {
                            player = x;
                            break;
                        }
                    }
                }
            }
            obj.Formation?.Remove(obj);
            var form = new ShipFormation(playerLead ? player : obj, formDef);
            if (playerLead && player != null) {
                form.Add(obj);
            }
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
            if (player != null && !obj.Formation.Contains(player))
            {
                form.Add(player);
                player.Formation = form;
            }
            ai.SetState(Next);
        }

        public override void Update(GameObject obj, SNPCComponent ai, double dt)
        {
        }
    }
}
