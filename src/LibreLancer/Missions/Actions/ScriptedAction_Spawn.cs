// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using LibreLancer.Data.Ini;
using LibreLancer.Data.Schema.Missions;
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
            GetString(nameof(Solar),  0, out Solar, act.Entry);
        }

        public override void Write(IniBuilder.IniSectionBuilder section)
        {
            section.Entry("Act_SpawnSolar", Solar);
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
                    sol.IdsName,
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
        public bool Important;

        public Act_MarkObj()
        {
        }

        public Act_MarkObj(MissionAction act) : base(act)
        {
            GetString(nameof(Object), 0, out Object, act.Entry);
            GetBoolean(nameof(Important), 1, out Important, act.Entry);
        }

        public override void Write(IniBuilder.IniSectionBuilder section)
        {
            section.Entry("Act_MarkObj", Object, Important ? 1 : 0);
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
                if (Important)
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

        protected void SpawnShip(ScriptShip ship, OptionalArgument<Vector3> spawnpos, OptionalArgument<Quaternion> spawnorient, string objList, MissionScript script, MissionRuntime runtime)
        {
            var npcDef = ship.NPC;
            script.NpcShips.TryGetValue(npcDef.NpcShipArch, out var shipArch);
            runtime.ObjectSpawned(ship.Nickname);
            if (shipArch == null)
            {
                shipArch = runtime.Player.Game.GameData.Items.NpcShips.Get(npcDef.NpcShipArch);
            }

            var archPos = spawnpos.Get(ship.Position);
            var orient = spawnorient.Get(ship.Orientation);
            MissionDirective[] directives = null;
            if (!string.IsNullOrEmpty(objList))
            {
                if (script.ObjLists.TryGetValue(objList, out var ol))
                {
                    directives = ol.Directives.ToArray();
                }
                else {
                    FLLog.Warning("Mission", $"Missing object list {objList}");
                }
            }

            runtime.Player.MissionWorldAction(() =>
            {
                runtime.Player.Space.World.Server.GameData.Items.TryGetLoadout(shipArch.Loadout, out var ld);
                var pilot = runtime.Player.Space.World.Server.GameData.Items.GetPilot(shipArch.Pilot);
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
                if (!string.IsNullOrWhiteSpace(ship.RelativePosition.ObjectName) &&
                    (relObj = runtime.Player.Space.World.GameWorld.GetObject(ship.RelativePosition.ObjectName)) != null)
                {
                    var dir = new Vector3(runtime.Random.NextFloat(-1, 1),
                        runtime.Random.NextFloat(-0.1f, 0.1f),
                        runtime.Random.NextFloat(-1, 1)).Normalized();
                    var range = runtime.Random.NextFloat(ship.RelativePosition.MinRange, ship.RelativePosition.MaxRange);
                    pos = relObj.WorldTransform.Position + (dir * range);
                }


                var obj = runtime.Player.Space.World.NPCs.DoSpawn(
                    oName,
                    ship.Nickname,
                    npcDef.Affiliation,
                    shipArch?.StateGraph ?? "FIGHTER",
                    npcDef.SpaceCostume,
                    ld, pilot, pos, orient, null, 0, runtime);
                var drComp = obj.GetComponent<DirectiveRunnerComponent>();
                drComp.SetDirectives(directives);
                var dstComp = obj.GetComponent<SDestroyableComponent>();
                dstComp.OnKilled = () => {
                    runtime.ObjectDestroyed(ship.Nickname);
                };
            });
        }
    }

    public class Act_SpawnFormation : ShipSpawnBase
    {
        public string Formation = string.Empty;
        public OptionalArgument<Vector3> Position;
        public OptionalArgument<Quaternion> Orientation;

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
            GetString(nameof(Formation), 0, out Formation, act.Entry);
            Formation = act.Entry[0].ToString();
            if (act.Entry.Count > 1)
            {
                GetVector3(nameof(Position), 1, out var p, act.Entry);
                Position = p;
            }
            if (act.Entry.Count > 4)
            {
                GetQuaternion(nameof(Orientation), 4, out var q, act.Entry);
                Orientation = q;
            }
        }

        public override void Write(IniBuilder.IniSectionBuilder section)
        {
            List<ValueBase> entry = [Formation];
            if (Position.Present)
            {
                entry.Add(Position.Value.X);
                entry.Add(Position.Value.Y);
                entry.Add(Position.Value.Z);
            }

            if (Orientation.Present)
            {
                entry.Add(Orientation.Value.W);
                entry.Add(Orientation.Value.X);
                entry.Add(Orientation.Value.Y);
                entry.Add(Orientation.Value.Z);
            }

            section.Entry("Act_SpawnFormation", entry.ToArray());
        }

        public override void Invoke(MissionRuntime runtime, MissionScript script)
        {
            var form = script.Formations[Formation];
            var fpos = Position.Get(form.Position);
            var forient = Orientation.Get(form.Orientation);
            var mat = Matrix4x4.CreateFromQuaternion(forient) *
                      Matrix4x4.CreateTranslation(fpos);
            var formDef = runtime.Player.Game.GameData.Items.GetFormation(form.Formation);
            IReadOnlyList<Vector3> positions = formDef?.Positions ?? nullOffsets;

            for (int i = 0; i < form.Ships.Count; i++)
            {
                var pos = Vector3.Transform(positions[i], mat);
                SpawnShip(form.Ships[i], pos, forient, null, script, runtime);
            }

            // make them into a formation
            runtime.Player.MissionWorldAction(() =>
            {
                var world = runtime.Player.Space.World;
                var lead = world.GameWorld.GetObject(form.Ships[0].Nickname);
                FormationTools.MakeNewFormation(lead, formDef?.Nickname, form.Ships.Skip(1)
                    .Select(x => x.Nickname).ToList());
            });
        }
    }

    public class Act_SpawnShip : ShipSpawnBase
    {
        public string Ship = string.Empty;
        public string ObjList = string.Empty;
        public OptionalArgument<Vector3> Position;
        public OptionalArgument<Quaternion> Orientation;

        public Act_SpawnShip()
        {
        }

        public Act_SpawnShip(MissionAction act) : base(act)
        {
            GetString(nameof(Ship), 0, out Ship, act.Entry);
            if (act.Entry.Count > 1)
            {
                ObjList = act.Entry[1].ToString();
            }
            if (act.Entry.Count > 2)
            {
                GetVector3(nameof(Position), 2, out var p, act.Entry);
                Position = p;
            }
            if (act.Entry.Count > 5)
            {
                GetQuaternion(nameof(Orientation), 5, out var q, act.Entry);
                Orientation = q;
            }
        }

        public override void Write(IniBuilder.IniSectionBuilder section)
        {
            List<ValueBase> entry = [Ship];
            if (!string.IsNullOrWhiteSpace(ObjList) && ObjList != "NULL")
            {
                entry.Add(ObjList);

                if (Position.Present)
                {
                    entry.Add(Position.Value.X);
                    entry.Add(Position.Value.Y);
                    entry.Add(Position.Value.Z);

                    if (Orientation.Present)
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
            if(script.Ships.TryGetValue(Ship, out var ship))
                SpawnShip(ship, Position, Orientation, ObjList, script, runtime);
            else
                FLLog.Error("Mission", $"{this}: Ship Missing");
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
            GetString(nameof(Loot), 0, out Loot, act.Entry);
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
                if (lootDef.Archetype == null)
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
                world.SpawnLoot(lootDef.Archetype.LootAppearance, lootDef.Archetype, lootDef.EquipAmount, new Transform3D(pos, Quaternion.Identity), Loot);
                FLLog.Info("Mission", $"Spawned loot {Loot} at {pos}");
            });
        }

        public override void Write(IniBuilder.IniSectionBuilder section)
        {
            section.Entry("Act_SpawnLoot", Loot);
        }
    }


    public enum DestroyKind
    {
        Default,
        SILENT,
        EXPLODE
    }

    public class Act_Destroy : ScriptedAction
    {
        public string Target = string.Empty;
        public DestroyKind Kind;

        public Act_Destroy()
        {

        }

        public Act_Destroy(MissionAction act) : base(act)
        {
            GetString(nameof(Target), 0, out Target, act.Entry);
            if (act.Entry.Count > 1)
            {
                Enum.TryParse<DestroyKind>(act.Entry[1].ToString(), true, out Kind);
            }
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
            if (Kind != DestroyKind.Default)
                section.Entry("Act_Destroy", Target, Kind.ToString());
            else
                section.Entry("Act_Destroy", Target);
        }
    }
}
