using System;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Text;
using LibreLancer.Data.GameData;
using LibreLancer.Missions.Directives;
using LibreLancer.Server.Components;
using LibreLancer.World;
using LibreLancer.World.Components;
using WattleScript.Interpreter;

namespace LibreLancer.Server
{
    [WattleScriptUserData]
    public class NPCWattleScripting
    {
        private NPCManager manager;
        private Script script;
        [WattleScriptHidden]
        public GameDataManager GameData;

        public NPCWattleScripting(NPCManager manager, GameDataManager gameData)
        {
            this.manager = manager;
            this.GameData = gameData;
            this.script = new Script(CoreModules.Preset_HardSandboxWattle);
            script.Options.Syntax = ScriptSyntax.Wattle;
            script.Options.IndexTablesFrom = 0;
            script.Globals["spawnnpc"] = DynValue.FromObject(script, spawnnpc);
            script.Globals["spawnnpcbase"] = DynValue.FromObject(script, spawnnpcbase);
            script.Globals["spawnnpcrot"] = DynValue.FromObject(script, spawnnpcrot);
            script.Globals["getnpc"] = DynValue.FromObject(script, getnpc);
            script.Globals["runscript"] = DynValue.FromObject(script, runscript);
        }

        public DynValue runscript(string file)
        {
            var p = Path.Combine(manager.World.Server.ScriptsFolder, file);
            if (!File.Exists(p)) {
                throw new ScriptRuntimeException($"Script file '{file}' does not exist");
            }
            return script.DoString(File.ReadAllText(p), null, file);
        }

        [WattleScriptHidden]
        public string Run(string code)
        {
            try
            {
                return script.DoString("return " + code).ToPrintString();
            }
            catch (ScriptRuntimeException se)
            {
                return se.DecoratedMessage ?? se.ToString();
            }
            catch (Exception e)
            {
                return e.ToString();
            }
        }

        internal GameObject? LookupObject(DynValue id, string funcName, int argIndex)
        {
            if (id.ToObject() is NPCWattleInstance wi)
            {
                return wi.Object;
            }

            return id.Type switch
            {
                DataType.String => manager.World.GameWorld.Objects.FirstOrDefault(x =>
                    id.String.Equals(x.Nickname, StringComparison.OrdinalIgnoreCase)),
                DataType.Number => manager.World.GameWorld.Objects.FirstOrDefault(x => x.NetID == (int) id.Number),
                _ => throw ScriptRuntimeException.BadArgument(argIndex, funcName, DataType.String, id.Type, false)
            };

        }

        // Meant to be called from WattleScript
        public NPCWattleInstance getnpc(DynValue id)
        {
            var obj = LookupObject(id, "getnpc", 1);
            if(obj != null) return new NPCWattleInstance(obj, manager.World.GameWorld, this);
            throw new ScriptRuntimeException($"Could not find object {id}");
        }

        private int spawnCount = 0;

        public NPCWattleInstance spawnnpc(string loadout, string pilot, float x, float y, float z)
        {
            return DoSpawn(loadout, pilot, x, y, z, null);
        }

        public NPCWattleInstance spawnnpcbase(string loadout, string pilot, string arrivalObj)
        {
            return DoSpawn(loadout, pilot, 0, 0, 0, arrivalObj);
        }

        public NPCWattleInstance spawnnpcrot(string loadout, string pilot, float x, float y, float z,
            float rw, float rx, float ry, float rz)
        {
            return DoSpawn(loadout, pilot, x, y, z, null, new(rx, ry, rz, rw));
        }

        private NPCWattleInstance DoSpawn(string loadout, string? pilot, float x, float y, float z, string? arrivalObj, Quaternion? rot = null)
        {
            if (!manager.World.Server.GameData.Items.TryGetLoadout(loadout, out var resolved))
                throw new ScriptRuntimeException($"Could not get loadout {loadout}");
            Pilot? p = null;
            if (pilot != null)
            {
                p = manager.World.Server.GameData.Items.GetPilot(pilot);
            }

            var position = new Vector3(x, y, z);
            var obj = manager.DoSpawn(new ObjectName("spawned " + ++spawnCount), null, null,  "FIGHTER", null, resolved, p, position, rot ?? Quaternion.Identity, arrivalObj, 0);
            return new NPCWattleInstance(obj, manager.World.GameWorld, this);
        }
    }

    [WattleScriptUserData]
    public class NPCWattleInstance
    {
        [WattleScriptHidden]
        internal GameObject Object;

        [WattleScriptHidden]
        internal NPCWattleScripting Scripting;

        [WattleScriptHidden]
        internal GameWorld World;

        internal NPCWattleInstance(GameObject obj, GameWorld world, NPCWattleScripting scripting)
        {
            Object = obj;
            Scripting = scripting;
            World = world;
        }

        public override string ToString() => Object.NetID.ToString() + " " + Object.ToString();

        public void runfuse(DynValue obj)
        {
            var str = obj.CastToString();
            var fuse = Scripting.GameData.Items.Fuses.Get(str);
            if (fuse == null) throw new ScriptRuntimeException($"Could not find fuse {str}");
            if(Object.TryGetComponent<SFuseRunnerComponent>(out var runner))
                runner.Run(fuse);
        }

        public void dock(DynValue obj)
        {
            var tgt = Scripting.LookupObject(obj, "dock", 1);
            if (tgt == null) throw new ScriptRuntimeException($"Could not find object {obj}");
            if (Object.TryGetComponent<SNPCComponent>(out var n))
                n.DockWith(tgt, World);
        }

        public void attack(DynValue obj)
        {
            var tgt = Scripting.LookupObject(obj, "attack", 1);
            if (tgt == null) throw new ScriptRuntimeException($"Could not find object {obj}");
            if (Object.TryGetComponent<SNPCComponent>(out var n))
                n.SetAttitude(tgt, RepAttitude.Hostile);
        }

        public void gotovec(float x, float y, float z, float range, float maxthrottle)
        {
            if (Object.TryGetComponent<DirectiveRunnerComponent>(out var n))
            {
                n.SetDirectives([
                    new GotoVecDirective()
                    {
                        Target = new(x, y, z),
                        MaxThrottle = maxthrottle,
                        Range = range
                    }
                ], World);
            }
        }

        public string state()
        {
            var builder = new StringBuilder();
            if (Object.TryGetComponent<AutopilotComponent>(out var ap))
            {
                builder.AppendLine($"Autopilot: {ap.CurrentBehavior}");
            }
            if (Object.TryGetComponent<DirectiveRunnerComponent>(out var run))
            {
                if (run.Active)
                {
                    builder.AppendLine("directive runner active");
                }
            }
            return "(null)";
        }

        public void formation(DynValue obj)
        {
            var tgt = Scripting.LookupObject(obj, "formation", 1);
            if (tgt == null) throw new ScriptRuntimeException($"Could not find object {obj}");
            if (tgt.Formation == null)
            {
                tgt.Formation = new ShipFormation(tgt, Object);
            }
            else
            {
                if(!tgt.Formation.Followers.Contains(Object))
                    tgt.Formation.Add(Object);
            }
            Object.Formation = tgt.Formation;
            if (Object.TryGetComponent<AutopilotComponent>(out var ap))
            {
                ap.StartFormation();
            }
        }
    }
}
