using System.Collections.Generic;
using System.Text;
using LibreLancer.Thorn;

namespace LibreLancer.Thn
{
    public static class ThnDecompile
    {
        static ThnDecompile()
        {
            LuaTable.EnumReverse = new Dictionary<string, string>();
            //ThnObjectFlags
            LuaTable.EnumReverse.Add("LitDynamic", "LIT_DYNAMIC");
            LuaTable.EnumReverse.Add("LitAmbient", "LIT_AMBIENT");
            LuaTable.EnumReverse.Add("Hidden", "HIDDEN");
            LuaTable.EnumReverse.Add("Reference", "REFERENCE");
            LuaTable.EnumReverse.Add("SoundSpatial", "SPATIAL");
            LuaTable.EnumReverse.Add("Spatial", "SPATIAL");
            //EventFlags
            LuaTable.EnumReverse.Add("Loop", "LOOP");
            LuaTable.EnumReverse.Add("Stream", "STREAM");
            //LightTypes
            LuaTable.EnumReverse.Add("Direct", "L_DIRECT");
            LuaTable.EnumReverse.Add("Point", "L_POINT");
            LuaTable.EnumReverse.Add("Spotlight", "L_SPOT");
            //TargetTypes
            LuaTable.EnumReverse.Add("Hardpoint", "HARDPOINT");
            LuaTable.EnumReverse.Add("Part", "PART");
            LuaTable.EnumReverse.Add("Root", "ROOT");
            //AttachFlags
            LuaTable.EnumReverse.Add("Position", "POSITION");
            LuaTable.EnumReverse.Add("Orientation", "ORIENTATION");
            LuaTable.EnumReverse.Add("LookAt", "LOOK_AT");
            LuaTable.EnumReverse.Add("EntityRelative", "ENTITY_RELATIVE");
            LuaTable.EnumReverse.Add("OrientationRelative", "ORIENTATION_RELATIVE");
            LuaTable.EnumReverse.Add("ParentChild", "PARENT_CHILD");
            //EntityTypes
            LuaTable.EnumReverse.Add("Camera", "CAMERA");
            LuaTable.EnumReverse.Add("PSys", "PSYS");
            LuaTable.EnumReverse.Add("Monitor", "MONITOR");
            LuaTable.EnumReverse.Add("Scene", "SCENE");
            LuaTable.EnumReverse.Add("Marker", "MARKER");
            LuaTable.EnumReverse.Add("Compound", "COMPOUND");
            LuaTable.EnumReverse.Add("Light", "LIGHT");
            LuaTable.EnumReverse.Add("MotionPath", "MOTION_PATH");
            LuaTable.EnumReverse.Add("Deformable", "DEFORMABLE");
            LuaTable.EnumReverse.Add("Sound", "SOUND");
            LuaTable.EnumReverse.Add("UnknownEntity", "UNKNOWN_ENTITY");
            LuaTable.EnumReverse.Add("Deleted", "DELETED");
            LuaTable.EnumReverse.Add("SubScene", "SUB_SCENE");
            //FogModes
            LuaTable.EnumReverse.Add("None", "F_NONE");
            LuaTable.EnumReverse.Add("Exp2", "F_EXP2");
            LuaTable.EnumReverse.Add("Exp", "F_EXP");
            LuaTable.EnumReverse.Add("Linear", "F_LINEAR");
            //EventTypes
            LuaTable.EnumReverse.Add("SetCamera", "SET_CAMERA");
            LuaTable.EnumReverse.Add("AttachEntity", "ATTACH_ENTITY");
            LuaTable.EnumReverse.Add("StartSpatialPropAnim", "START_SPATIAL_PROP_ANIM");
            LuaTable.EnumReverse.Add("StartLightPropAnim", "START_LIGHT_PROP_ANIM");
            LuaTable.EnumReverse.Add("StartPSys", "START_PSYS");
            LuaTable.EnumReverse.Add("StartPSysPropAnim", "START_PSYS_PROP_ANIM");
            LuaTable.EnumReverse.Add("StartPathAnimation", "START_PATH_ANIMATION");
            LuaTable.EnumReverse.Add("StartMotion", "START_MOTION");
            LuaTable.EnumReverse.Add("StartFogPropAnim", "START_FOG_PROP_ANIM");
            LuaTable.EnumReverse.Add("StartCameraPropAnim", "START_CAMERA_PROP_ANIM");
            LuaTable.EnumReverse.Add("StartSound", "START_SOUND");
            LuaTable.EnumReverse.Add("StartAudioPropAnim", "START_AUDIO_PROP_ANIM");
            LuaTable.EnumReverse.Add("ConnectHardpoints", "CONNECT_HARDPOINTS");
            LuaTable.EnumReverse.Add("StartFloorHeightAnim", "START_FLR_HEIGHT_ANIM");
            LuaTable.EnumReverse.Add("StartIK","START_IK");
            LuaTable.EnumReverse.Add("StartSubScene", "START_SUB_SCENE");
            LuaTable.EnumReverse.Add("UndefinedEvent", "UNDEFINED_EVENT");
            LuaTable.EnumReverse.Add("UserEvent", "USER_EVENT");
            LuaTable.EnumReverse.Add("StartReverbPropAnim", "START_REVERB_PROP_ANIM");
            LuaTable.EnumReverse.Add("Subtitle", "SUBTITLE");
        }
        
        public static string Decompile(string file)
        {
            var builder = new StringBuilder();
            var runner = new LuaRunner(ThnScript.ThnEnv);
            var output = runner.DoFile(file);
            foreach (var kv in output)
            {
                switch (kv.Key.ToLowerInvariant())
                {
                    case "events":
                        ProcessEvents((LuaTable)kv.Value);
                        break;
                    case "entities":
                        ProcessEntities((LuaTable)kv.Value);
                        break;
                }
                builder.AppendLine(string.Format("{0} = {1}", kv.Key, kv.Value));
            }
            return builder.ToString();
        }
        static void ProcessEntities(LuaTable t)
        {
            //Make sure flags aren't integers
            object o;
            for (int ti = 0; ti < t.Capacity; ti++)
            {
                var ent = (LuaTable)t[ti];
                ent["type"] = ThnTypes.Convert<EntityTypes>(ent["type"]);
                if (ent.TryGetValue("lightprops", out o))
                {
                    var lp = (LuaTable)o;
                    if (lp.ContainsKey("type")) lp["type"] = ThnTypes.Convert<LightTypes>(lp["type"]);
                }
                if (ent.ContainsKey("flags")) ent["flags"] = ConvertFlags((EntityTypes)ent["type"], ent);
            }
        }
        static ThnObjectFlags ConvertFlags(EntityTypes type, LuaTable table)
        {
            if (!(table["flags"] is float)) return (ThnObjectFlags)table["flags"];
            var val = (int)(float)table["flags"];
            return ThnTypes.Convert<ThnObjectFlags>(val);
        }
        static void ProcessEvents(LuaTable t)
        {
            for (int ti = 0; ti < t.Capacity; ti++)
            {
                var ev = (LuaTable)t[ti];
                ev[1] = ThnTypes.Convert<EventTypes>(ev[1]);
                if (ev.Capacity >= 4)
                {
                    var props = (LuaTable)ev[3];
                    //TODO: Property flags
                }
            }
        }
    }
}