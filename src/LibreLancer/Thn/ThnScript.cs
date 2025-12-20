// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Numerics;
using LibreLancer.Data.GameData;
using LibreLancer.Render;
using LibreLancer.Thn.Events;
using LibreLancer.Thorn;
using LibreLancer.Thorn.VM;

namespace LibreLancer.Thn
{
	public class ThnScript
	{
		#region Runtime
		public static Dictionary<string,object> ThnEnv = new Dictionary<string, object>();
		static ThnScript()
		{
			//ThnObjectFlags
			ThnEnv.Add("LIT_DYNAMIC", ThnObjectFlags.LitDynamic);
			ThnEnv.Add("LIT_AMBIENT", ThnObjectFlags.LitAmbient);
			ThnEnv.Add("HIDDEN", ThnObjectFlags.Hidden);
			ThnEnv.Add("REFERENCE", ThnObjectFlags.Reference);
			ThnEnv.Add("SPATIAL", ThnObjectFlags.SoundSpatial);
			//EventFlags
            ThnEnv.Add("STREAM", SoundFlags.Stream);
			ThnEnv.Add("LOOP", SoundFlags.Loop);
			//LightTypes
			ThnEnv.Add("L_DIRECT", LightTypes.Direct);
			ThnEnv.Add("L_POINT", LightTypes.Point);
			ThnEnv.Add("L_SPOT", LightTypes.Spotlight);
			//TargetTypes
			ThnEnv.Add("HARDPOINT", TargetTypes.Hardpoint);
			ThnEnv.Add("PART", TargetTypes.Part);
			ThnEnv.Add("ROOT", TargetTypes.Root);
			//AttachFlags
			ThnEnv.Add("POSITION", AttachFlags.Position);
			ThnEnv.Add("ORIENTATION", AttachFlags.Orientation);
			ThnEnv.Add("LOOK_AT", AttachFlags.LookAt);
			ThnEnv.Add("ENTITY_RELATIVE", AttachFlags.EntityRelative);
			ThnEnv.Add("ORIENTATION_RELATIVE", AttachFlags.OrientationRelative);
			ThnEnv.Add("PARENT_CHILD", AttachFlags.ParentChild);
			//EntityTypes
			ThnEnv.Add("CAMERA", EntityTypes.Camera);
			ThnEnv.Add("PSYS", EntityTypes.PSys);
			ThnEnv.Add("MONITOR", EntityTypes.Monitor);
			ThnEnv.Add("SCENE", EntityTypes.Scene);
			ThnEnv.Add("MARKER", EntityTypes.Marker);
			ThnEnv.Add("COMPOUND", EntityTypes.Compound);
			ThnEnv.Add("LIGHT", EntityTypes.Light);
			ThnEnv.Add("MOTION_PATH", EntityTypes.MotionPath);
			ThnEnv.Add("DEFORMABLE", EntityTypes.Deformable);
			ThnEnv.Add("SOUND", EntityTypes.Sound);
            ThnEnv.Add("UNKNOWN_ENTITY", EntityTypes.UnknownEntity);
            ThnEnv.Add("SUB_SCENE", EntityTypes.SubScene);
            ThnEnv.Add("DELETED", EntityTypes.Deleted);
            //FogModes
			ThnEnv.Add("F_NONE", FogModes.None);
			ThnEnv.Add("F_EXP2", FogModes.Exp2);
			ThnEnv.Add("F_EXP", FogModes.Exp);
			ThnEnv.Add("F_LINEAR", FogModes.Linear);
			//EventTypes
			ThnEnv.Add("SET_CAMERA", EventTypes.SetCamera);
			ThnEnv.Add("ATTACH_ENTITY", EventTypes.AttachEntity);
			ThnEnv.Add("START_SPATIAL_PROP_ANIM", EventTypes.StartSpatialPropAnim);
			ThnEnv.Add("START_LIGHT_PROP_ANIM", EventTypes.StartLightPropAnim);
			ThnEnv.Add("START_PSYS", EventTypes.StartPSys);
			ThnEnv.Add("START_PSYS_PROP_ANIM", EventTypes.StartPSysPropAnim);
			ThnEnv.Add("START_PATH_ANIMATION", EventTypes.StartPathAnimation);
			ThnEnv.Add("START_MOTION", EventTypes.StartMotion);
			ThnEnv.Add("START_FOG_PROP_ANIM", EventTypes.StartFogPropAnim);
			ThnEnv.Add("START_CAMERA_PROP_ANIM", EventTypes.StartCameraPropAnim);
			ThnEnv.Add("START_SOUND", EventTypes.StartSound);
			ThnEnv.Add("START_AUDIO_PROP_ANIM", EventTypes.StartAudioPropAnim);
            ThnEnv.Add("START_FLR_HEIGHT_ANIM", EventTypes.StartFloorHeightAnim);
			ThnEnv.Add("CONNECT_HARDPOINTS", EventTypes.ConnectHardpoints);
            ThnEnv.Add("START_IK", EventTypes.StartIK);
            ThnEnv.Add("SUBTITLE", EventTypes.Subtitle);
            ThnEnv.Add("START_REVERB_PROP_ANIM", EventTypes.StartReverbPropAnim);
            ThnEnv.Add("USER_EVENT", EventTypes.UserEvent);
            ThnEnv.Add("UNDEFINED_EVENT", EventTypes.UndefinedEvent);
            ThnEnv.Add("START_SUB_SCENE", EventTypes.StartSubScene);
            //Axis
			ThnEnv.Add("X_AXIS", ThnAxis.XAxis);
			ThnEnv.Add("Y_AXIS", ThnAxis.YAxis);
			ThnEnv.Add("Z_AXIS", ThnAxis.ZAxis);
			ThnEnv.Add("NEG_X_AXIS", ThnAxis.NegXAxis);
			ThnEnv.Add("NEG_Y_AXIS", ThnAxis.NegYAxis);
			ThnEnv.Add("NEG_Z_AXIS", ThnAxis.NegZAxis);
			//Booleans
			ThnEnv.Add("Y", true);
			ThnEnv.Add("N", false);
			ThnEnv.Add("y", true);
			ThnEnv.Add("n", false);
		}
		#endregion

		public double Duration;
		public Dictionary<string, ThnEntity> Entities = new Dictionary<string, ThnEntity>(StringComparer.OrdinalIgnoreCase);
		public List<ThnEvent> Events = new List<ThnEvent>();


        public ThnScript(byte[] bytes, ReadFileCallback readFile, string source)
        {
            var runner = new ThornRunner(ThnEnv, readFile);
            Initialize(runner.DoBytes(bytes), source);
        }

        private void Initialize(IDictionary<string, object> output, string source)
        {
            Duration = (float)output["duration"];
            var entities = (ThornTable)output["entities"];
            foreach(var val in entities.Values)
            {
                var ent = (ThornTable)val;
                var e = GetEntity(ent, source);
                if (Entities.ContainsKey(e.Name))
                {
                    FLLog.Error("Thn", "Overwriting entity: \"" + e.Name + '"');
                    Entities[e.Name] = e;
                }
                else
                    Entities.Add(e.Name, e);
            }
            var events = (ThornTable)output["events"];
            foreach(var val in events.Values)
            {
                var ev = (ThornTable)val;
                var e = GetEvent(ev, source);
                Events.Add(e);
            }
            Events.Sort((x, y) => x.Time.CompareTo(y.Time));
        }

		ThnEvent GetEvent(ThornTable table, string source)
		{
            var t = ThnTypes.Convert<EventTypes>(table[2]);
            switch (t)
            {
                case EventTypes.SetCamera:
                    return new SetCameraEvent(table);
                case EventTypes.StartSound:
                    return new StartSoundEvent(table);
                case EventTypes.StartAudioPropAnim:
                    return new StartAudioPropAnimEvent(table);
                case EventTypes.StartLightPropAnim:
                    return new StartLightPropAnimEvent(table);
                case EventTypes.StartCameraPropAnim:
                    return new StartCameraPropAnimEvent(table);
                case EventTypes.StartPathAnimation:
                    return new StartPathAnimationEvent(table);
                case EventTypes.StartSpatialPropAnim:
                    return new StartSpatialPropAnimEvent(table, source);
                case EventTypes.AttachEntity:
                    return new AttachEntityEvent(table);
                case EventTypes.ConnectHardpoints:
                    return new ConnectHardpointsEvent(table);
                case EventTypes.StartMotion:
                    return new StartMotionEvent(table);
                case EventTypes.StartIK:
                    return new StartIKEvent(table);
                case EventTypes.StartSubScene:
                    return new StartSubSceneEvent(table);
                case EventTypes.StartPSys:
                    return new StartPSysEvent(table);
                case EventTypes.StartPSysPropAnim:
                    return new StartPSysPropAnimEvent(table);
                case EventTypes.StartFogPropAnim:
                    return new StartFogPropAnimEvent(table);
                case EventTypes.StartReverbPropAnim:
                    return new StartReverbPropAnim(table);
                case EventTypes.StartFloorHeightAnim:
                    return new StartFloorHeightAnimEvent(table);
                case EventTypes.Subtitle:
                    return new SubtitleEvent(table);
            }
            throw new ArgumentException($"event type {t}");
        }
		//Flags are stored differently internally between Freelancer and Librelancer
		ThnObjectFlags ConvertFlags(EntityTypes type, ThornTable table)
		{
			var val = (int)(float)table["flags"];
			if (val == 0) return ThnObjectFlags.None;
			if (val == 1) return ThnObjectFlags.Reference; //Should be for all types
			if (type == EntityTypes.Sound)
			{
                if ((val & 2) == 2) {
                    val = (val & ~2) | (int)ThnObjectFlags.SoundSpatial;
                }
			}
			return ThnTypes.Convert<ThnObjectFlags>(val);
		}



		public static Quaternion GetQuaternion(ThornTable orient)
		{
			var m11 = (float)((ThornTable)orient[1])[1];
			var m12 = (float)((ThornTable)orient[1])[2];
			var m13 = (float)((ThornTable)orient[1])[3];

			var m21 = (float)((ThornTable)orient[2])[1];
			var m22 = (float)((ThornTable)orient[2])[2];
			var m23 = (float)((ThornTable)orient[2])[3];

			var m31 = (float)((ThornTable)orient[3])[1];
			var m32 = (float)((ThornTable)orient[3])[2];
			var m33 = (float)((ThornTable)orient[3])[3];
            return new Matrix4x4(
                m11, m12, m13, 0,
                m21, m22, m23, 0,
                m31, m32, m33, 0,
                0, 0, 0, 1
            ).ExtractRotation();
        }

        static float FuzzyFloat(object o)
        {
            if (o is float f) return f;
            if (o is int i) return i;
            if (o is string s)
                return float.Parse(s, CultureInfo.InvariantCulture);
            throw new InvalidCastException(o.ToString() + " as float");
        }

        static int FuzzyInt(object o) => (int) FuzzyFloat(o);

		ThnEntity GetEntity(ThornTable table, string source)
		{
			object o;

			var e = new ThnEntity();
			e.Name = (string)table["entity_name"];
			e.Type = ThnTypes.Convert<EntityTypes>(table["type"]);
			if (table.TryGetValue("srt_grp", out o))
			{
				e.SortGroup = (int)(float)table["srt_grp"];
			}
			if (table.TryGetValue("usr_flg", out o))
			{
				e.UserFlag = (int)(float)table["usr_flg"];
			}
			if (table.TryGetValue("lt_grp", out o))
			{
				e.LightGroup = (int)(float)table["lt_grp"];
			}

			Vector3 tmp;
			if (table.TryGetVector3("ambient", out tmp))
			{
				e.Ambient = tmp;
			}
			if (table.TryGetValue("up", out o))
            {
                e.Up = ThnTypes.ConvertAxis(o, source);
            }
			if (table.TryGetValue("front", out o))
			{
				e.Front = ThnTypes.ConvertAxis(o, source);
			}

			if (table.TryGetValue("template_name", out o))
			{
				e.Template = (string)o;
			}
			if (table.TryGetValue("flags", out o))
			{
				if (o is float)
				{
					e.ObjectFlags = ConvertFlags(e.Type, table);
				}
				else
				{
					e.ObjectFlags = (ThnObjectFlags)o;
				}
			}
			if (table.TryGetValue("userprops", out o))
			{
				var usrprops = (ThornTable)o;
				if (usrprops.TryGetValue("category", out o))
				{
					e.MeshCategory = (string)o;
				}
				if (usrprops.TryGetValue("nofog", out o))
				{
					e.NoFog = ThnTypes.Convert<bool>(o);
				}
                if (usrprops.TryGetValue("Actor", out o))
                {
                    e.Actor = o.ToString();
                }
                if (usrprops.TryGetValue("TextString", out o))
                {
                    e.DisplayText = new ThnDisplayText();
                    e.DisplayText.TextIDS = FuzzyInt(o);
                    if (usrprops.TryGetValue("TextStart", out o))
                        e.DisplayText.Start = FuzzyFloat(o);
                }

                if (usrprops.TryGetValue("Priority", out o))
                {
                    e.Priority = o.ToString();
                }
                if (usrprops.TryGetValue("main_object", out o))
                {
                    e.MainObject = true;
                }
			}
            if(table.TryGetValue("audioprops", out o))
            {
                var aprops = (ThornTable)o;
                e.AudioProps = new ThnAudioProps();
                if (aprops.TryGetValue("rmix", out o)) e.AudioProps.Rmix = (float)o;
                if (aprops.TryGetValue("ain", out o)) e.AudioProps.Ain = (float)o;
                if (aprops.TryGetValue("dmax", out o)) e.AudioProps.Dmax = (float)o;
                if (aprops.TryGetValue("atout", out o)) e.AudioProps.Atout = (float)o;
                if (aprops.TryGetValue("pan", out o)) e.AudioProps.Pan = (float)o;
                if (aprops.TryGetValue("dmin", out o)) e.AudioProps.Dmin = (float)o;
                if (aprops.TryGetValue("aout", out o)) e.AudioProps.Aout = (float)o;
                if (aprops.TryGetValue("attenuation", out o)) e.AudioProps.Attenuation = (float)o;
            }
            if (table.TryGetValue("spatialprops", out o))
			{
				var spatialprops = (ThornTable)o;
				if (spatialprops.TryGetVector3("pos", out tmp))
				{
					e.Position = tmp;
				}
				if (spatialprops.TryGetValue("orient", out o))
				{
					e.Rotation = GetQuaternion((ThornTable)o);
				}
			}

			if (table.TryGetValue("cameraprops", out o))
			{
				var cameraprops = (ThornTable)o;
				if (cameraprops.TryGetValue("fovh", out o))
				{
					e.FovH = (float)o;
				}
				if (cameraprops.TryGetValue("hvaspect", out o))
				{
					e.HVAspect = (float)o;
				}
                if (cameraprops.TryGetValue("nearplane", out o))
                {
                    e.NearPlane = (float) o;
                }
                if (cameraprops.TryGetValue("farplane", out o))
                {
                    e.FarPlane = (float) o;
                }
			}

            if (table.TryGetValue("compoundprops", out o))
            {
                var compoundprops = (ThornTable)o;
                if(compoundprops.TryGetValue("floor_height", out o)) {
                    e.FloorHeight = (float)o;
                }
            }
			if (table.TryGetValue("lightprops", out o))
			{
				var lightprops = (ThornTable)o;
				e.LightProps = new ThnLightProps();
				if (lightprops.TryGetValue("on", out o))
				{
					e.LightProps.On = ThnTypes.Convert<bool>(o);
				}
				else
					e.LightProps.On = true;
				var r = new RenderLight();
				r.Position = e.Position.Value;
				if (lightprops.TryGetValue("type", out o))
				{
					var tp = ThnTypes.Convert<LightTypes>(o);
					if (tp == LightTypes.Point)
						r.Kind = LightKind.Point;
					if (tp == LightTypes.Direct)
						r.Kind = LightKind.Directional;
					if (tp == LightTypes.Spotlight)
					{
						r.Kind = LightKind.Spotlight;
						r.Falloff = 1f;
					}
				}
				else
					throw new Exception("Light without type");
				if (lightprops.TryGetVector3("diffuse", out tmp))
					r.Color = new Color3f(tmp.X, tmp.Y, tmp.Z);
                if (lightprops.TryGetVector3("ambient", out tmp))
                    r.Ambient = new Color3f(tmp.X, tmp.Y, tmp.Z);
				if (lightprops.TryGetVector3("direction", out tmp))
					r.Direction = tmp;
				if (lightprops.TryGetValue("range", out o))
					r.Range = (int)(float)o;
				if (lightprops.TryGetValue("theta", out o))
					r.Theta = (float)o;
                if (lightprops.TryGetValue("cutoff", out o))
                    r.Phi = (float)o;
				if (lightprops.TryGetVector3("atten", out tmp))
				{
                    r.Attenuation = tmp;
				}
				e.LightProps.Render = r;
			}
			if (table.TryGetValue("pathprops", out o))
			{
				var pathprops = (ThornTable)o;
                if (pathprops.TryGetValue("path_data", out o))
				{
					e.Path = new MotionPath((string)o);
				}
			}
			return e;
		}

	}
}

