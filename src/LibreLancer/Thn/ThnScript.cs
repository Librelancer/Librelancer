/* The contents of this file are subject to the Mozilla Public License
 * Version 1.1 (the "License"); you may not use this file except in
 * compliance with the License. You may obtain a copy of the License at
 * http://www.mozilla.org/MPL/
 * 
 * Software distributed under the License is distributed on an "AS IS"
 * basis, WITHOUT WARRANTY OF ANY KIND, either express or implied. See the
 * License for the specific language governing rights and limitations
 * under the License.
 * 
 * 
 * The Initial Developer of the Original Code is Callum McGing (mailto:callum.mcging@gmail.com).
 * Portions created by the Initial Developer are Copyright (C) 2013-2016
 * the Initial Developer. All Rights Reserved.
 */
using System;
using System.Collections.Generic;
using LibreLancer.Thorn;
namespace LibreLancer
{
	public class ThnScript
	{
		#region Runtime
		static Dictionary<string,object> thnEnv = new Dictionary<string, object>();
		static ThnScript()
		{
			//ThnLighting
			thnEnv.Add("LIT_DYNAMIC", ThnLighting.Dynamic);
			thnEnv.Add("LIT_AMBIENT", ThnLighting.Ambient);
			//LightTypes
			thnEnv.Add("L_DIRECT", LightTypes.Direct);
			thnEnv.Add("L_POINT", LightTypes.Point);
			//TargetTypes
			thnEnv.Add("HARDPOINT", TargetTypes.Hardpoint);
			thnEnv.Add("PART", TargetTypes.Part);
			thnEnv.Add("ROOT", TargetTypes.Root);
			//AttachFlags
			thnEnv.Add("POSITION", AttachFlags.Position);
			thnEnv.Add("ORIENTATION", AttachFlags.Orientation);
			thnEnv.Add("LOOK_AT", AttachFlags.LookAt);
			thnEnv.Add("ENTITY_RELATIVE", AttachFlags.EntityRelative);
			thnEnv.Add("ORIENTATION_RELATIVE", AttachFlags.OrientationRelative);
			//EntityTypes
			thnEnv.Add("CAMERA", EntityTypes.Camera);
			thnEnv.Add("PSYS", EntityTypes.PSys);
			thnEnv.Add("MONITOR", EntityTypes.Monitor);
			thnEnv.Add("SCENE", EntityTypes.Scene);
			thnEnv.Add("MARKER", EntityTypes.Marker);
			thnEnv.Add("COMPOUND", EntityTypes.Compound);
			thnEnv.Add("LIGHT", EntityTypes.Light);
			thnEnv.Add("MOTION_PATH", EntityTypes.MotionPath);
			//FogModes
			thnEnv.Add("F_EXP2", FogModes.Exp2);
			//EventTypes
			thnEnv.Add("SET_CAMERA", EventTypes.SetCamera);
			thnEnv.Add("ATTACH_ENTITY", EventTypes.AttachEntity);
			thnEnv.Add("START_SPATIAL_PROP_ANIM", EventTypes.StartSpatialPropAnim);
			thnEnv.Add("START_PSYS", EventTypes.StartPSys);
			thnEnv.Add("START_PSYS_PROP_ANIM", EventTypes.StartPSysPropAnim);
			thnEnv.Add("START_PATH_ANIMATION", EventTypes.StartPathAnimation);
			thnEnv.Add("START_MOTION", EventTypes.StartMotion);
			thnEnv.Add("START_FOG_PROP_ANIM", EventTypes.StartFogPropAnim);
			//Axis
			thnEnv.Add("X_AXIS", VectorMath.UnitX);
			thnEnv.Add("Y_AXIS", VectorMath.UnitY);
			thnEnv.Add("Z_AXIS", VectorMath.UnitZ);
			thnEnv.Add("NEG_X_AXIS", -VectorMath.UnitX);
			thnEnv.Add("NEG_Y_AXIS", -VectorMath.UnitY);
			thnEnv.Add("NEG_Z_AXIS", -VectorMath.UnitZ);
			//Booleans
			thnEnv.Add("Y", true);
			thnEnv.Add("N", false);

		}
		#endregion

		public double Duration;
		public Dictionary<string, ThnEntity> Entities = new Dictionary<string, ThnEntity>();
		public List<ThnEvent> Events = new List<ThnEvent>();
		public ThnScript (string scriptfile)
		{
			var runner = new LuaRunner (thnEnv);
			var output = runner.DoFile (scriptfile);
			Duration = (float)output["duration"];
			var entities = (LuaTable)output["entities"];
			for (int i = 0; i < entities.Capacity; i++)
			{
				var ent = (LuaTable)entities[i];
				var e = GetEntity(ent);
				Entities.Add(e.Name, e);
			}
			var events = (LuaTable)output["events"];
			for (int i = 0; i < events.Capacity; i++)
			{
				var ev = (LuaTable)events[i];
				var e = GetEvent(ev);
				Events.Add(e);
			}
			Events.Sort((x, y) => x.Time.CompareTo(y.Time));
		}
		ThnEvent GetEvent(LuaTable table)
		{
			var e = new ThnEvent();
			e.Time = (float)table[0];
			e.Type = (EventTypes)table[1];
			return e;
		}
		ThnEntity GetEntity(LuaTable table)
		{
			var e = new ThnEntity();
			e.Name = (string)table["entity_name"];
			e.Type = (EntityTypes)table["type"];
			e.LightGroup = (int)(float)table["lt_grp"];
			e.SortGroup = (int)(float)table["srt_grp"];
			e.UserFlag = (int)(float)table["usr_flg"];
			Vector3 tmp;
			if (table.TryGetVector3("ambient", out tmp))
			{
				e.Ambient = tmp;
			}
			if (table.TryGetVector3("up", out tmp))
			{
				e.Up = tmp;
			}
			if (table.TryGetVector3("front", out tmp))
			{
				e.Front = tmp;
			}
			object o;
			if (table.TryGetValue("template_name", out o))
			{
				e.Template = (string)o;
			}
			return e;
		}

	}
}

