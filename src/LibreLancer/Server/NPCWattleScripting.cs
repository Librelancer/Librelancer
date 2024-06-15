using System;
using System.Linq;
using System.Numerics;
using System.Text;
using LibreLancer.GameData;
using LibreLancer.Server.Ai;
using LibreLancer.Server.Components;
using LibreLancer.World;
using LibreLancer.World.Components;
using WattleScript.Interpreter;

namespace LibreLancer.Server
{
    [WattleScriptUserData]
    public class NPCWattleScripting
    {
        NPCManager manager;
        Script script;

        public NPCWattleScripting(NPCManager manager)
        {
            this.manager = manager;
            this.script = new Script(CoreModules.None);
            script.Options.Syntax = ScriptSyntax.Wattle;
            script.Globals["spawnnpc"] = DynValue.FromObject(script, spawnnpc);
            script.Globals["getnpc"] = DynValue.FromObject(script, getnpc);
        }

        [WattleScriptHidden]
        public string Run(string code)
        {
            try
            {
                return script.DoString("return " + code).ToPrintString();
            }
            catch (Exception e)
            {
                return e.Message;
            }
        }

        internal GameObject LookupObject(DynValue id, string funcName, int argIndex)
        {
            if (id.ToObject() is NPCWattleInstance wi) return wi.Object;
            if (id.Type == DataType.String) {
                return manager.World.GameWorld.Objects.FirstOrDefault(x =>
                    id.String.Equals(x.Nickname, StringComparison.OrdinalIgnoreCase));
            } else if (id.Type == DataType.Number) {
                return manager.World.GameWorld.Objects.FirstOrDefault(x => x.NetID == (int)id.Number);
            }
            else {
                throw ScriptRuntimeException.BadArgument(argIndex, funcName, DataType.String, id.Type, false);
            }
        }

        //Meant to be called from WattleScript
        public NPCWattleInstance getnpc(DynValue id)
        {
            var obj = LookupObject(id, "getnpc", 1);
            if(obj != null) return new NPCWattleInstance(obj, this);
            throw new ScriptRuntimeException($"Could not find object {id}");
        }

        private int spawnCount = 0;

        public NPCWattleInstance spawnnpc(string loadout, string pilot, float x, float y, float z)
        {
            if (!manager.World.Server.GameData.TryGetLoadout(loadout, out var resolved))
                throw new ScriptRuntimeException($"Could not get loadout {loadout}");
            Pilot p = null;
            if (pilot != null)
                p = manager.World.Server.GameData.GetPilot(pilot);
            var position = new Vector3(x, y, z);
            var obj = manager.DoSpawn(new ObjectName("spawned " + ++spawnCount),null, null,  "FIGHTER", null, null, null, resolved, p, position, Quaternion.Identity);
            return new NPCWattleInstance(obj, this);
        }
    }

    [WattleScriptUserData]
    public class NPCWattleInstance
    {
        [WattleScriptHidden]
        internal GameObject Object;

        [WattleScriptHidden]
        internal NPCWattleScripting Scripting;

        internal NPCWattleInstance(GameObject obj, NPCWattleScripting scripting)
        {
            this.Object = obj;
            this.Scripting = scripting;
        }

        public override string ToString() => Object.NetID.ToString() + " " + Object.ToString();


        public void dock(DynValue obj)
        {
            var tgt = Scripting.LookupObject(obj, "dock", 1);
            if (tgt == null) throw new ScriptRuntimeException($"Could not find object {obj}");
            if (Object.TryGetComponent<SNPCComponent>(out var n))
                n.DockWith(tgt);
        }

        public void attack(DynValue obj)
        {
            var tgt = Scripting.LookupObject(obj, "attack", 1);
            if (tgt == null) throw new ScriptRuntimeException($"Could not find object {obj}");
            if (Object.TryGetComponent<SNPCComponent>(out var n))
                n.SetAttitude(tgt, RepAttitude.Hostile);
        }

        void PrintState(AiState state, StringBuilder builder)
        {
            if (state == null) builder.Append("none");
            else
            {
                builder.AppendLine(state.ToString());
                if (state is AiObjListState obj)
                {
                    builder.Append("-> ");
                    PrintState(obj.Next, builder);
                }
            }
        }
        public string state()
        {
            var builder = new StringBuilder();
            if (Object.TryGetComponent<AutopilotComponent>(out var ap))
            {
                builder.AppendLine($"Autopilot: {ap.CurrentBehavior}");
            }
            if (Object.TryGetComponent<SNPCComponent>(out var n))
            {
                PrintState(n.CurrentDirective, builder);
                return builder.ToString();
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
