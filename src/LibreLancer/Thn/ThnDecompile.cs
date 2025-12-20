using System.Collections.Generic;
using System.IO;
using System.Text;
using LibreLancer.Data;
using LibreLancer.Thorn;
using LibreLancer.Thorn.VM;

namespace LibreLancer.Thn
{
    public static class ThnDecompile
    {
        private static bool _inited = false;

        public static void Init()
        {
            if (_inited)
                return;
            _inited = true;
            ThornTable.EnumReverse = new Dictionary<string, string>();
            //ThnObjectFlags
            ThornTable.EnumReverse.Add("LitDynamic", "LIT_DYNAMIC");
            ThornTable.EnumReverse.Add("LitAmbient", "LIT_AMBIENT");
            ThornTable.EnumReverse.Add("Hidden", "HIDDEN");
            ThornTable.EnumReverse.Add("Reference", "REFERENCE");
            ThornTable.EnumReverse.Add("SoundSpatial", "SPATIAL");
            ThornTable.EnumReverse.Add("Spatial", "SPATIAL");
            //EventFlags
            ThornTable.EnumReverse.Add("Loop", "LOOP");
            ThornTable.EnumReverse.Add("Stream", "STREAM");
            //LightTypes
            ThornTable.EnumReverse.Add("Direct", "L_DIRECT");
            ThornTable.EnumReverse.Add("Point", "L_POINT");
            ThornTable.EnumReverse.Add("Spotlight", "L_SPOT");
            //TargetTypes
            ThornTable.EnumReverse.Add("Hardpoint", "HARDPOINT");
            ThornTable.EnumReverse.Add("Part", "PART");
            ThornTable.EnumReverse.Add("Root", "ROOT");
            //AttachFlags
            ThornTable.EnumReverse.Add("Position", "POSITION");
            ThornTable.EnumReverse.Add("Orientation", "ORIENTATION");
            ThornTable.EnumReverse.Add("LookAt", "LOOK_AT");
            ThornTable.EnumReverse.Add("EntityRelative", "ENTITY_RELATIVE");
            ThornTable.EnumReverse.Add("OrientationRelative", "ORIENTATION_RELATIVE");
            ThornTable.EnumReverse.Add("ParentChild", "PARENT_CHILD");
            //EntityTypes
            ThornTable.EnumReverse.Add("Camera", "CAMERA");
            ThornTable.EnumReverse.Add("PSys", "PSYS");
            ThornTable.EnumReverse.Add("Monitor", "MONITOR");
            ThornTable.EnumReverse.Add("Scene", "SCENE");
            ThornTable.EnumReverse.Add("Marker", "MARKER");
            ThornTable.EnumReverse.Add("Compound", "COMPOUND");
            ThornTable.EnumReverse.Add("Light", "LIGHT");
            ThornTable.EnumReverse.Add("MotionPath", "MOTION_PATH");
            ThornTable.EnumReverse.Add("Deformable", "DEFORMABLE");
            ThornTable.EnumReverse.Add("Sound", "SOUND");
            ThornTable.EnumReverse.Add("UnknownEntity", "UNKNOWN_ENTITY");
            ThornTable.EnumReverse.Add("Deleted", "DELETED");
            ThornTable.EnumReverse.Add("SubScene", "SUB_SCENE");
            //FogModes
            ThornTable.EnumReverse.Add("None", "F_NONE");
            ThornTable.EnumReverse.Add("Exp2", "F_EXP2");
            ThornTable.EnumReverse.Add("Exp", "F_EXP");
            ThornTable.EnumReverse.Add("Linear", "F_LINEAR");
            //EventTypes
            ThornTable.EnumReverse.Add("SetCamera", "SET_CAMERA");
            ThornTable.EnumReverse.Add("AttachEntity", "ATTACH_ENTITY");
            ThornTable.EnumReverse.Add("StartSpatialPropAnim", "START_SPATIAL_PROP_ANIM");
            ThornTable.EnumReverse.Add("StartLightPropAnim", "START_LIGHT_PROP_ANIM");
            ThornTable.EnumReverse.Add("StartPSys", "START_PSYS");
            ThornTable.EnumReverse.Add("StartPSysPropAnim", "START_PSYS_PROP_ANIM");
            ThornTable.EnumReverse.Add("StartPathAnimation", "START_PATH_ANIMATION");
            ThornTable.EnumReverse.Add("StartMotion", "START_MOTION");
            ThornTable.EnumReverse.Add("StartFogPropAnim", "START_FOG_PROP_ANIM");
            ThornTable.EnumReverse.Add("StartCameraPropAnim", "START_CAMERA_PROP_ANIM");
            ThornTable.EnumReverse.Add("StartSound", "START_SOUND");
            ThornTable.EnumReverse.Add("StartAudioPropAnim", "START_AUDIO_PROP_ANIM");
            ThornTable.EnumReverse.Add("ConnectHardpoints", "CONNECT_HARDPOINTS");
            ThornTable.EnumReverse.Add("StartFloorHeightAnim", "START_FLR_HEIGHT_ANIM");
            ThornTable.EnumReverse.Add("StartIK","START_IK");
            ThornTable.EnumReverse.Add("StartSubScene", "START_SUB_SCENE");
            ThornTable.EnumReverse.Add("UndefinedEvent", "UNDEFINED_EVENT");
            ThornTable.EnumReverse.Add("UserEvent", "USER_EVENT");
            ThornTable.EnumReverse.Add("StartReverbPropAnim", "START_REVERB_PROP_ANIM");
            ThornTable.EnumReverse.Add("Subtitle", "SUBTITLE");
            //Axis
            ThornTable.EnumReverse.Add("XAxis", "X_AXIS");
            ThornTable.EnumReverse.Add("YAxis", "Y_AXIS");
            ThornTable.EnumReverse.Add("ZAxis", "Z_AXIS");
            ThornTable.EnumReverse.Add("NegXAxis", "NEG_X_AXIS");
            ThornTable.EnumReverse.Add("NegYAxis", "NEG_Y_AXIS");
            ThornTable.EnumReverse.Add("NegZAxis", "NEG_Z_AXIS");
        }

        public static string Decompile(string file, ReadFileCallback readCallback = null)
        {
            Init();
            var builder = new StringBuilder();
            var runner = new ThornRunner(ThnScript.ThnEnv, readCallback);
            runner.Log = false;
            var output = runner.DoBytes(File.ReadAllBytes(file), file);
            foreach (var kv in output)
            {
                switch (kv.Key.ToLowerInvariant())
                {
                    case "events":
                        ProcessEvents((ThornTable)kv.Value);
                        break;
                    case "entities":
                        ProcessEntities((ThornTable)kv.Value, file);
                        break;
                }
                if (kv.Value is float f)
                    builder.AppendLine($"{kv.Key} = {f.ToStringInvariant()}");
                else if (kv.Value is ThornTable table)
                    builder.AppendLine($"{kv.Key} = {table.Dump(false, 0, false)}");
                else
                    builder.AppendLine($"{kv.Key} = {kv.Value}");
            }
            return builder.ToString();
        }
        static void ProcessEntities(ThornTable t, string source)
        {
            //Make sure flags aren't integers
            object o;
            foreach(var e in t.Values)
            {
                var ent = (ThornTable)e;
                ent["type"] = ThnTypes.Convert<EntityTypes>(ent["type"]);
                if (ent.TryGetValue("lightprops", out o))
                {
                    var lp = (ThornTable)o;
                    if (lp.ContainsKey("type")) lp["type"] = ThnTypes.Convert<LightTypes>(lp["type"]);
                }
                if (ent.ContainsKey("flags")) ent["flags"] = ConvertFlags((EntityTypes)ent["type"], ent);
                if (ent.ContainsKey("front"))
                    ent["front"] = ThnTypes.ConvertAxis(ent["front"], source);
                if(ent.ContainsKey("up"))
                    ent["up"] = ThnTypes.ConvertAxis(ent["up"], source);
            }
        }
        static ThnObjectFlags ConvertFlags(EntityTypes type, ThornTable table)
        {
            if (!(table["flags"] is float)) return (ThnObjectFlags)table["flags"];
            var val = (int)(float)table["flags"];
            var tp = ThnTypes.Convert<ThnObjectFlags>(val);
            if (type == EntityTypes.Sound && (tp & ThnObjectFlags.LitDynamic) == ThnObjectFlags.LitDynamic)
            {
                return ThnObjectFlags.SoundSpatial | (tp & ~ThnObjectFlags.LitDynamic);
            }
            return tp;
        }
        static void ProcessEvents(ThornTable t)
        {
            foreach(var e in t.Values)
            {
                var ev = (ThornTable)e;
                ev[2] = ThnTypes.Convert<EventTypes>(ev[2]);
                if (ev.Length >= 4)
                {
                    var props = (ThornTable)ev[3];
                    //TODO: Property flags
                }
            }
        }
    }
}
