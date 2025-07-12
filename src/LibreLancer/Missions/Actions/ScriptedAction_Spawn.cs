// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using LibreLancer.Data.Ini;
using LibreLancer.Data.Missions;
using LibreLancer.Server;
using LibreLancer.Server.Ai;
using LibreLancer.Server.Components;
using LibreLancer.World;

namespace LibreLancer.Missions.Actions
{
    public class Act_SpawnSolar : ScriptedAction
    {
        public string Solar = string.Empty;

        public Act_SpawnSolar()
        {
        }

        public Act_SpawnSolar(MissionAction act) : base(act)
        {
            Solar = act.Entry[0].ToString();
        }

        public override void Invoke(MissionRuntime runtime, MissionScript script)
        {
            var sol = script.Solars[Solar];
            var arch = sol.Archetype;
            runtime.ObjectSpawned(sol.Nickname);
            runtime.Player.MissionWorldAction(() =>
            {
                var obj = runtime.Player.Space.World.SpawnSolar(
                    sol.Nickname,
                    arch,
                    sol.Loadout,
                    sol.Faction,
                    sol.Position,
                    sol.Orientation,
                    sol.StringId,
                    sol.Base
                    );
                if(obj.TryGetComponent<SDestroyableComponent>(out var dstComp))
                {
                    dstComp.OnKilled = () => {
                        runtime.ObjectDestroyed(Solar);
                    };
                }
            });
        }
    }

    public class Act_MarkObj : ScriptedAction
    {
        public string Object = string.Empty;
        public int Value;

        public Act_MarkObj()
        {
        }

        public Act_MarkObj(MissionAction act) : base(act)
        {
            Object = act.Entry[0].ToString();
            Value = act.Entry[1].ToInt32();
        }

        public override void Write(IniBuilder.IniSectionBuilder section)
        {
            section.Entry("Act_MarkObj", Object, Value);
        }

        public override void Invoke(MissionRuntime runtime, MissionScript script)
        {
            runtime.Player.MissionWorldAction(() =>
            {
                var obj = runtime.Player.Space.World.GameWorld.GetObject(Object);
                if (obj == null)
                {
                    FLLog.Warning("Mission", $"Object not found for MarkObj `{Object}`");
                    return;
                }
                if (Value != 0)
                {
                    obj.Flags |= GameObjectFlags.Important;
                    runtime.Player.RpcClient.MarkImportant(obj.NetID, true);
                }
                else
                {
                    obj.Flags &= ~GameObjectFlags.Important;
                    runtime.Player.RpcClient.MarkImportant(obj.NetID, false);
                }
            });
        }
    }

    public abstract class ShipSpawnBase : ScriptedAction
    {
        protected ShipSpawnBase() {}
        protected ShipSpawnBase(MissionAction act) : base(act) { }

        protected void SpawnShip(string msnShip, Vector3? spawnpos, Quaternion? spawnorient, string objList, MissionScript script, MissionRuntime runtime)
        {
            var ship = script.Ships[msnShip];
            var npcDef = script.NPCs[ship.NPC];
            script.NpcShips.TryGetValue(npcDef.NpcShipArch, out var shipArch);
            runtime.ObjectSpawned(ship.Nickname);
            if (shipArch == null)
            {
                shipArch = runtime.Player.Game.GameData.Ini.NPCShips.ShipArches.First(x =>
                    x.Nickname.Equals(npcDef.NpcShipArch, StringComparison.OrdinalIgnoreCase));
            }

            var archPos = spawnpos ?? ship.Position;
            var orient = spawnorient ?? ship.Orientation;
            MissionDirective[] directives = null;
            if (!string.IsNullOrEmpty(objList))
            {
                if (script.ObjLists.TryGetValue(objList, out var ol))
                {
                    directives = ol.Directives;
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

                var pos = archPos;
                GameObject relObj;
                // Spawn relative to object
                if (ship.RelativePosition != null &&
                    (relObj = runtime.Player.Space.World.GameWorld.GetObject(ship.RelativePosition.ObjectName)) != null)
                {
                    var dir = new Vector3(runtime.Random.NextFloat(-1, 1),
                        runtime.Random.NextFloat(-0.1f, 0.1f),
                        runtime.Random.NextFloat(-1, 1)).Normalized();
                    var range = runtime.Random.NextFloat(ship.RelativePosition.MinRange, ship.RelativePosition.MaxRange);
                    pos = relObj.WorldTransform.Position + (dir * range);
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
                    ld, pilot, pos, orient, null, runtime);
                var drComp = obj.GetComponent<DirectiveRunnerComponent>();
                drComp.SetDirectives(directives);
                var dstComp = obj.GetComponent<SDestroyableComponent>();
                dstComp.OnKilled = () => {
                    runtime.ObjectDestroyed(msnShip);
                };
            });
        }
    }

    public class Act_SpawnFormation : ShipSpawnBase
    {
        public string Formation = string.Empty;
        public Vector3? Position;

        public Act_SpawnFormation()
        {
        }

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

        public override void Write(IniBuilder.IniSectionBuilder section)
        {
            List<ValueBase> entry = [Formation];
            if (Position.HasValue)
            {
                entry.Add(Position.Value.X);
                entry.Add(Position.Value.Y);
                entry.Add(Position.Value.Z);
            }

            section.Entry("Act_SpawnFormation", entry.ToArray());
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

            // make them into a formation
            runtime.Player.MissionWorldAction(() =>
            {
                var world = runtime.Player.Space.World;
                var lead = world.GameWorld.GetObject(form.Ships[0]);
                FormationTools.MakeNewFormation(lead, formDef?.Nickname, form.Ships.Skip(1).ToList());
            });
        }
    }

    public class Act_SpawnShip : ShipSpawnBase
    {
        public string Ship = string.Empty;
        public string ObjList = string.Empty;
        public Vector3? Position;
        public Quaternion? Orientation;

        public Act_SpawnShip()
        {
        }

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

        public override void Write(IniBuilder.IniSectionBuilder section)
        {
            List<ValueBase> entry = [Ship];
            if (ObjList != "NULL")
            {
                entry.Add(ObjList);

                if (Position is not null)
                {
                    entry.Add(Position.Value.X);
                    entry.Add(Position.Value.Y);
                    entry.Add(Position.Value.Z);

                    if (Orientation is not null)
                    {
                        entry.Add(Orientation.Value.W);
                        entry.Add(Orientation.Value.X);
                        entry.Add(Orientation.Value.Y);
                        entry.Add(Orientation.Value.Z);
                    }
                }
            }

            section.Entry("Act_SpawnShip", entry.ToArray());
        }

        public override void Invoke(MissionRuntime runtime, MissionScript script)
        {
            SpawnShip(Ship, Position, Orientation, ObjList, script, runtime);
        }
    }

    public class Act_SpawnLoot : ScriptedAction
    {
        public string Loot = string.Empty;

        public Act_SpawnLoot()
        {
        }

        public Act_SpawnLoot(MissionAction act) : base(act)
        {
            Loot = act.Entry[0].ToString();
        }

        public override void Invoke(MissionRuntime runtime, MissionScript script)
        {
            if (!script.Loot.TryGetValue(Loot, out var lootDef))
            {
                FLLog.Error("Mission", $"{this}: Loot Missing");
                return;
            }
            runtime.Player.MissionWorldAction(() =>
            {
                var world = runtime.Player.Space.World;
                var pos = lootDef.Position;
                var arch = world.Server.GameData.Equipment.Get(lootDef.Archetype);
                if (arch == null)
                {
                    FLLog.Error("Mission", $"{this}: Invalid archetype {lootDef.Archetype}");
                    return;
                }
                if (!string.IsNullOrWhiteSpace(lootDef.RelPosObj))
                {
                    var obj = world.GameWorld.GetObject(lootDef.RelPosObj);
                    if (obj == null)
                    {
                        FLLog.Warning("Mission", $"{this}: Loot missing relposobj {lootDef.RelPosObj}");
                        pos = lootDef.RelPosOffset;
                    }
                    else
                    {
                        pos = obj.WorldTransform.Transform(lootDef.RelPosOffset);
                    }
                }
                world.SpawnLoot(arch.LootAppearance, arch, lootDef.EquipAmount, new Transform3D(pos, Quaternion.Identity));
            });
        }

        public override void Write(IniBuilder.IniSectionBuilder section)
        {
            section.Entry("Act_SpawnLoot", Loot);
        }
    }

    public class Act_Destroy : ScriptedAction
    {
        public string Target = string.Empty;

        public Act_Destroy()
        {

        }

        public Act_Destroy(MissionAction act) : base(act)
        {
            Target = act.Entry[0].ToString();
        }

        public override void Invoke(MissionRuntime runtime, MissionScript script)
        {
            if (script.Ships.TryGetValue(Target, out var ship))
            {
                runtime.ObjectDestroyed(ship.Nickname);
                runtime.Player.MissionWorldAction(() => { runtime.Player.Space.World.NPCs.Despawn(runtime.Player.Space.World.GameWorld.GetObject(Target), false); });
            }
        }

        public override void Write(IniBuilder.IniSectionBuilder section)
        {
            section.Entry("Act_Destroy", Target);
        }
    }
}
