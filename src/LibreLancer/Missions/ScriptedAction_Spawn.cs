// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using LibreLancer.Data.Missions;
using LibreLancer.Server.Ai;
using LibreLancer.Server.Components;
using LibreLancer.World;

namespace LibreLancer.Missions
{
    public class Act_SpawnSolar : ScriptedAction
    {
        public string Solar;
        public Act_SpawnSolar(MissionAction act) : base(act)
        {
            Solar = act.Entry[0].ToString();
        }

        public override void Invoke(MissionRuntime runtime, MissionScript script)
        {
            var sol = script.Solars[Solar];
            var arch = sol.Archetype;
            runtime.Player.MissionWorldAction(() =>
            {
                runtime.Player.Space.World.SpawnSolar(
                    sol.Nickname,
                    arch,
                    sol.Loadout,
                    sol.Position,
                    sol.Orientation,
                    sol.StringId,
                    sol.Base
                    );
            });
        }
    }

    public class Act_MarkObj : ScriptedAction
    {
        public string Object;
        public int Value;

        public Act_MarkObj(MissionAction act) : base(act)
        {
            Object = act.Entry[0].ToString();
            Value = act.Entry[1].ToInt32();
        }

        public override void Invoke(MissionRuntime runtime, MissionScript script)
        {
            if (Value != 1)
            {
                FLLog.Warning("Mission", $"MarkObj val {Value} not implemented");
                return;
            }
            if (script.Ships.TryGetValue(Object, out var ship))
            {
                runtime.Player.Space.World.NPCs.NpcDoAction(Object, (o) =>
                {
                    o.Flags |= GameObjectFlags.Important;
                    runtime.Player.RpcClient.MarkImportant(o.NetID);
                });
            }
            else
            {
                FLLog.Warning("Mission", $"Ship not found for MarkObj {Object}");
            }
        }
    }

    public abstract class ShipSpawnBase : ScriptedAction
    {
        protected ShipSpawnBase(MissionAction act) : base(act) { }

        protected void SpawnShip(string msnShip, Vector3? spawnpos, Quaternion? spawnorient, string objList, MissionScript script, MissionRuntime runtime)
        {
            var ship = script.Ships[msnShip];
            var npcDef = script.NPCs[ship.NPC];
            script.NpcShips.TryGetValue(npcDef.NpcShipArch, out var shipArch);
            foreach (var lbl in ship.Labels)
                runtime.LabelIncrement(lbl);
            if (shipArch == null)
            {
                shipArch = runtime.Player.Game.GameData.Ini.NPCShips.ShipArches.First(x =>
                    x.Nickname.Equals(npcDef.NpcShipArch, StringComparison.OrdinalIgnoreCase));
            }

            var pos = spawnpos ?? ship.Position;
            var orient = spawnorient ?? ship.Orientation;
            AiState state = null;
            if (!string.IsNullOrEmpty(objList))
            {
                if (script.ObjLists.TryGetValue(objList, out var ol))
                {
                    state = ol.AiState;
                }
                else {
                    FLLog.Warning("Mission", $"Missing object list {objList}");
                }
            }

            runtime.Player.MissionWorldAction(() =>
            {
                runtime.Player.Space.World.Server.GameData.TryGetLoadout(shipArch.Loadout, out var ld);
                var pilot = runtime.Player.Space.World.Server.GameData.GetPilot(shipArch.Pilot);
                ObjectName oName;
                if (ship.RandomName)
                {
                    oName = runtime.Player.Space.World.NPCs.RandomName(npcDef.Affiliation);
                }
                else
                {
                    oName = new ObjectName(npcDef.IndividualName);
                }

                string commHead = null;
                string commBody = null;
                string commHelmet = null;
                if (npcDef.SpaceCostume != null && npcDef.SpaceCostume.Length > 0)
                    commHead = npcDef.SpaceCostume[0];
                if (npcDef.SpaceCostume != null && npcDef.SpaceCostume.Length > 1)
                    commBody = npcDef.SpaceCostume[1];
                if (npcDef.SpaceCostume != null && npcDef.SpaceCostume.Length > 2)
                    commHelmet = npcDef.SpaceCostume[2];
                var obj = runtime.Player.Space.World.NPCs.DoSpawn(
                    oName,
                    ship.Nickname,
                    npcDef.Affiliation,
                    shipArch?.StateGraph ?? "FIGHTER",
                    commHead, commBody, commHelmet,
                    ld, pilot, pos, orient, runtime);
                var npcComp = obj.GetComponent<SNPCComponent>();
                npcComp.OnKilled = () => {
                    runtime.NpcKilled(msnShip);
                    foreach (var lbl in ship.Labels)
                        runtime.LabelKilled(lbl);
                };
                npcComp.SetState(state);
            });
        }
    }

    public class Act_SpawnFormation : ShipSpawnBase
    {
        public string Formation;
        public Vector3? Position;

        //TODO: implement formations
        private static IReadOnlyList<Vector3> nullOffsets = new List<Vector3>(
        new[]{
            Vector3.Zero,
            new Vector3(-60, 0, 0),
            new Vector3(60, 0, 0),
            new Vector3(0, -60, 0),
            new Vector3(0, 60, 0)
        });
        public Act_SpawnFormation(MissionAction act) : base(act)
        {
            Formation = act.Entry[0].ToString();
            if (act.Entry.Count > 1)
                Position = new Vector3(act.Entry[1].ToSingle(), act.Entry[2].ToSingle(),
                    act.Entry[3].ToSingle());
        }

        public override void Invoke(MissionRuntime runtime, MissionScript script)
        {
            var form = script.Formations[Formation];
            var fpos = Position ?? form.Position;
            var mat = Matrix4x4.CreateFromQuaternion(form.Orientation) *
                      Matrix4x4.CreateTranslation(fpos);
            var formDef = runtime.Player.Game.GameData.GetFormation(form.Formation);
            IReadOnlyList<Vector3> positions = formDef?.Positions ?? nullOffsets;

            for (int i = 0; i < form.Ships.Count; i++)
            {
                var pos = Vector3.Transform(positions[i], mat);
                SpawnShip(form.Ships[i], pos, form.Orientation, null, script, runtime);
            }
        }
    }

    public class Act_SpawnShip : ShipSpawnBase
    {
        public string Ship;
        public string ObjList;
        public Vector3? Position;
        public Quaternion? Orientation;

        public Act_SpawnShip(MissionAction act) : base(act)
        {
            Ship = act.Entry[0].ToString();
            if (act.Entry.Count > 1)
            {
                ObjList = act.Entry[1].ToString();
            }
            if (act.Entry.Count > 2)
            {
                Position = new Vector3(act.Entry[2].ToSingle(), act.Entry[3].ToSingle(), act.Entry[4].ToSingle());
            }
            if (act.Entry.Count > 5)
            {
                Orientation = new Quaternion(act.Entry[6].ToSingle(), act.Entry[7].ToSingle(), act.Entry[8].ToSingle(),
                    act.Entry[5].ToSingle());
            }
        }
        public override void Invoke(MissionRuntime runtime, MissionScript script)
        {
            SpawnShip(Ship, Position, Orientation, ObjList, script, runtime);
        }
    }

    public class Act_Destroy : ScriptedAction
    {
        public string Target;

        public Act_Destroy(MissionAction act) : base(act)
        {
            Target = act.Entry[0].ToString();
        }

        public override void Invoke(MissionRuntime runtime, MissionScript script)
        {
            if (script.Ships.ContainsKey(Target))
            {
                var ship = script.Ships[Target];
                var npcDef = script.NPCs[ship.NPC];
                script.NpcShips.TryGetValue(npcDef.NpcShipArch, out var shipArch);
                foreach (var lbl in ship.Labels)
                    runtime.LabelDecrement(lbl);
                runtime.Player.MissionWorldAction(() => { runtime.Player.Space.World.NPCs.Despawn(runtime.Player.Space.World.GameWorld.GetObject(Target), false); });
            }
        }
    }
}
