using System.Collections.Generic;
using System.Numerics;
using LibreLancer.Server.Components;
using LibreLancer.World;
using LibreLancer.World.Components;

namespace LibreLancer.Server;

public class FormationTools
{
    public static void EnterFormation(GameObject self, GameObject tgt, Vector3 offset)
    {
        if (tgt.Formation == null)
        {
            tgt.Formation = new ShipFormation(tgt, self);
        }
        else
        {
            if(!tgt.Formation.Contains(self))
                tgt.Formation.Add(self);
        }
        self.Formation = tgt.Formation;
        if(offset != Vector3.Zero)
            tgt.Formation.SetShipOffset(self, offset);
        if (self.TryGetComponent<AutopilotComponent>(out var ap))
        {
            ap.StartFormation();
        }
    }

    public static void MakeNewFormation(GameObject obj, string formation, List<string> others)
    {
        //TODO: Gross
        var formDef = obj.World.Server.Server.GameData.Items.GetFormation(formation);
        GameObject player = null;
        bool playerLead = false;
        // Preserve player (required)
        if (obj.Formation != null)
        {
            if (obj.Formation.LeadShip.TryGetComponent<SPlayerComponent>(out _))
            {
                player = obj.Formation.LeadShip;
                playerLead = true;
            }
            else
            {
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
        if (playerLead && player != null)
        {
            form.Add(obj);
        }

        obj.Formation = form;
        foreach (var tgt in others)
        {
            var o = obj.World.GetObject(tgt);
            if (o == null)
            {
                continue;
            }

            if (tgt != null)
            {
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
    }
}
