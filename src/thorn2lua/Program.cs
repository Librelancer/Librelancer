// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using LibreLancer;
using LibreLancer.Thorn;

namespace thorn2lua
{
    class MainClass
    {
        public static Dictionary<string, object> ThnEnv = new Dictionary<string, object>();
        static MainClass()
        {
            LuaTable.EnumReverse = new Dictionary<string, string>();
            //ThnObjectFlags
            ThnEnv.Add("LIT_DYNAMIC", ThnObjectFlags.LitDynamic);
            LuaTable.EnumReverse.Add("LitDynamic", "LIT_DYNAMIC");
            ThnEnv.Add("LIT_AMBIENT", ThnObjectFlags.LitAmbient);
            LuaTable.EnumReverse.Add("LitAmbient", "LIT_AMBIENT");
            ThnEnv.Add("HIDDEN", ThnObjectFlags.Hidden);
            LuaTable.EnumReverse.Add("Hidden", "HIDDEN");
            ThnEnv.Add("REFERENCE", ThnObjectFlags.Reference);
            LuaTable.EnumReverse.Add("Reference", "REFERENCE");
            ThnEnv.Add("SPATIAL", ThnObjectFlags.Spatial);
            LuaTable.EnumReverse.Add("Spatial", "SPATIAL");
            //EventFlags
            ThnEnv.Add("LOOP", SoundFlags.Loop);
            LuaTable.EnumReverse.Add("Loop", "LOOP");
            //LightTypes
            ThnEnv.Add("L_DIRECT", LightTypes.Direct);
            LuaTable.EnumReverse.Add("Direct", "L_DIRECT");
            ThnEnv.Add("L_POINT", LightTypes.Point);
            LuaTable.EnumReverse.Add("Point", "L_POINT");
            ThnEnv.Add("L_SPOT", LightTypes.Spotlight);
            LuaTable.EnumReverse.Add("Spotlight", "L_SPOT");
            //TargetTypes
            ThnEnv.Add("HARDPOINT", TargetTypes.Hardpoint);
            LuaTable.EnumReverse.Add("Hardpoint", "HARDPOINT");
            ThnEnv.Add("PART", TargetTypes.Part);
            LuaTable.EnumReverse.Add("Part", "PART");
            ThnEnv.Add("ROOT", TargetTypes.Root);
            LuaTable.EnumReverse.Add("Root", "ROOT");
            //AttachFlags
            ThnEnv.Add("POSITION", AttachFlags.Position);
            LuaTable.EnumReverse.Add("Position", "POSITION");
            ThnEnv.Add("ORIENTATION", AttachFlags.Orientation);
            LuaTable.EnumReverse.Add("Orientation", "ORIENTATION");
            ThnEnv.Add("LOOK_AT", AttachFlags.LookAt);
            LuaTable.EnumReverse.Add("LookAt", "LOOK_AT");
            ThnEnv.Add("ENTITY_RELATIVE", AttachFlags.EntityRelative);
            LuaTable.EnumReverse.Add("EntityRelative", "ENTITY_RELATIVE");
            ThnEnv.Add("ORIENTATION_RELATIVE", AttachFlags.OrientationRelative);
            LuaTable.EnumReverse.Add("OrientationRelative", "ORIENTATION_RELATIVE");
            ThnEnv.Add("PARENT_CHILD", AttachFlags.ParentChild);
            LuaTable.EnumReverse.Add("ParentChild", "PARENT_CHILD");
            //EntityTypes
            ThnEnv.Add("CAMERA", EntityTypes.Camera);
            LuaTable.EnumReverse.Add("Camera", "CAMERA");
            ThnEnv.Add("PSYS", EntityTypes.PSys);
            LuaTable.EnumReverse.Add("PSys", "PSYS");
            ThnEnv.Add("MONITOR", EntityTypes.Monitor);
            LuaTable.EnumReverse.Add("Monitor", "MONITOR");
            ThnEnv.Add("SCENE", EntityTypes.Scene);
            LuaTable.EnumReverse.Add("Scene", "SCENE");
            ThnEnv.Add("MARKER", EntityTypes.Marker);
            LuaTable.EnumReverse.Add("Marker", "MARKER");
            ThnEnv.Add("COMPOUND", EntityTypes.Compound);
            LuaTable.EnumReverse.Add("Compound", "COMPOUND");
            ThnEnv.Add("LIGHT", EntityTypes.Light);
            LuaTable.EnumReverse.Add("Light", "LIGHT");
            ThnEnv.Add("MOTION_PATH", EntityTypes.MotionPath);
            LuaTable.EnumReverse.Add("MotionPath", "MOTION_PATH");
            ThnEnv.Add("DEFORMABLE", EntityTypes.Deformable);
            LuaTable.EnumReverse.Add("Deformable", "DEFORMABLE");
            ThnEnv.Add("SOUND", EntityTypes.Sound);
            LuaTable.EnumReverse.Add("Sound", "SOUND");
            //FogModes
            ThnEnv.Add("F_NONE", FogModes.None);
            LuaTable.EnumReverse.Add("None", "F_NONE");
            ThnEnv.Add("F_EXP2", FogModes.Exp2);
            LuaTable.EnumReverse.Add("Exp2", "F_EXP2");
            ThnEnv.Add("F_EXP", FogModes.Exp);
            LuaTable.EnumReverse.Add("Exp", "F_EXP");
            ThnEnv.Add("F_LINEAR", FogModes.Linear);
            LuaTable.EnumReverse.Add("Linear", "F_LINEAR");
            //EventTypes
            ThnEnv.Add("SET_CAMERA", EventTypes.SetCamera);
            LuaTable.EnumReverse.Add("SetCamera", "SET_CAMERA");
            ThnEnv.Add("ATTACH_ENTITY", EventTypes.AttachEntity);
            LuaTable.EnumReverse.Add("AttachEntity", "ATTACH_ENTITY");
            ThnEnv.Add("START_SPATIAL_PROP_ANIM", EventTypes.StartSpatialPropAnim);
            LuaTable.EnumReverse.Add("StartSpatialPropAnim", "START_SPATIAL_PROP_ANIM");
            ThnEnv.Add("START_LIGHT_PROP_ANIM", EventTypes.StartLightPropAnim);
            LuaTable.EnumReverse.Add("StartLightPropAnim", "START_LIGHT_PROP_ANIM");
            ThnEnv.Add("START_PSYS", EventTypes.StartPSys);
            LuaTable.EnumReverse.Add("StartPSys", "START_PSYS");
            ThnEnv.Add("START_PSYS_PROP_ANIM", EventTypes.StartPSysPropAnim);
            LuaTable.EnumReverse.Add("StartPSysPropAnim", "START_PSYS_PROP_ANIM");
            ThnEnv.Add("START_PATH_ANIMATION", EventTypes.StartPathAnimation);
            LuaTable.EnumReverse.Add("StartPathAnimation", "START_PATH_ANIMATION");
            ThnEnv.Add("START_MOTION", EventTypes.StartMotion);
            LuaTable.EnumReverse.Add("StartMotion", "START_MOTION");
            ThnEnv.Add("START_FOG_PROP_ANIM", EventTypes.StartFogPropAnim);
            LuaTable.EnumReverse.Add("StartFogPropAnim", "START_FOG_PROP_ANIM");
            ThnEnv.Add("START_CAMERA_PROP_ANIM", EventTypes.StartCameraPropAnim);
            LuaTable.EnumReverse.Add("StartCameraPropAnim", "START_CAMERA_PROP_ANIM");
            ThnEnv.Add("START_SOUND", EventTypes.StartSound);
            LuaTable.EnumReverse.Add("StartSound", "START_SOUND");
            ThnEnv.Add("START_AUDIO_PROP_ANIM", EventTypes.StartAudioPropAnim);
            LuaTable.EnumReverse.Add("StartAudioPropAnim", "START_AUDIO_PROP_ANIM");
            ThnEnv.Add("CONNECT_HARDPOINTS", EventTypes.ConnectHardpoints);
            LuaTable.EnumReverse.Add("ConnectHardpoints", "CONNECT_HARDPOINTS");
            ThnEnv.Add("START_FLR_HEIGHT_ANIM", EventTypes.StartFloorHeightAnim);
            LuaTable.EnumReverse.Add("StartFloorHeightAnim", "START_FLR_HEIGHT_ANIM");
            //Axis
            ThnEnv.Add("X_AXIS", Vector3.UnitX);
            ThnEnv.Add("Y_AXIS", Vector3.UnitY);
            ThnEnv.Add("Z_AXIS", Vector3.UnitZ);
            ThnEnv.Add("NEG_X_AXIS", -Vector3.UnitX);
            ThnEnv.Add("NEG_Y_AXIS", -Vector3.UnitY);
            ThnEnv.Add("NEG_Z_AXIS", -Vector3.UnitZ);
            //Booleans
            ThnEnv.Add("Y", true);
            ThnEnv.Add("N", false);
            ThnEnv.Add("y", true);
            ThnEnv.Add("n", false);
        }
        public static void Main(string[] args)
        {
            if (args.Length == 0)
            {
                Console.Error.WriteLine("thorn2lua: input.thn [output.lua]");
                return;
            }
            else if (args.Length == 1)
                Console.WriteLine(Decompile(args[0]));
            else
                File.WriteAllText(args[1], Decompile(args[0]));

        }
        static string Decompile(string file)
        {
            var builder = new StringBuilder();
            var runner = new LuaRunner(ThnEnv);
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
                ent["type"] = ThnEnum.Check<EntityTypes>(ent["type"]);
                if (ent.TryGetValue("lightprops", out o))
                {
                    var lp = (LuaTable)o;
                    if (lp.ContainsKey("type")) lp["type"] = ThnEnum.Check<LightTypes>(lp["type"]);
                }
                if (ent.ContainsKey("flags")) ent["flags"] = ConvertFlags((EntityTypes)ent["type"], ent);
            }
        }
        static ThnObjectFlags ConvertFlags(EntityTypes type, LuaTable table)
        {
            if (!(table["flags"] is float)) return (ThnObjectFlags)table["flags"];
            var val = (int)(float)table["flags"];
            if (val == 0) return ThnObjectFlags.None;
            if (val == 1) return ThnObjectFlags.Reference; //Should be for all types
            if (type == EntityTypes.Sound)
            {
                switch (val)
                {
                    case 2:
                        return ThnObjectFlags.Spatial;
                    default:
                        throw new NotImplementedException();
                }
            }
            return ThnEnum.FlagsReflected<ThnObjectFlags>(val);
        }
        static void ProcessEvents(LuaTable t)
        {
            for (int ti = 0; ti < t.Capacity; ti++)
            {
                var ev = (LuaTable)t[ti];
                ev[1] = ThnEnum.Check<EventTypes>(ev[1]);
                if (ev.Capacity >= 4)
                {
                    var props = (LuaTable)ev[3];
                    //TODO: Property flags
                }
            }
        }
    }
}
