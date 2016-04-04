using System;
using System.Collections.Generic;
using LibreLancer.Thorn;
namespace LibreLancer
{
	public class ThnScript
	{
		static Dictionary<string,object> thnEnv = new Dictionary<string, object>();
		static ThnScript()
		{
			//ThnLighting
			thnEnv.Add ("LIT_DYNAMIC", ThnLighting.Dynamic);
			thnEnv.Add ("LIT_AMBIENT", ThnLighting.Ambient);
			//AttachFlags
			thnEnv.Add ("POSITION", AttachFlags.Position);
			thnEnv.Add ("ORIENTATION", AttachFlags.Orientation);
			thnEnv.Add ("LOOKAT", AttachFlags.LookAt);
			thnEnv.Add ("ENTITY_RELATIVE", AttachFlags.EntityRelative);
			//EntityTypes
			thnEnv.Add("CAMERA", EntityTypes.Camera);
			thnEnv.Add ("PSYS", EntityTypes.PSys);
			thnEnv.Add ("MONITOR", EntityTypes.Monitor);
			thnEnv.Add ("SCENE", EntityTypes.Scene);
			thnEnv.Add ("MARKER", EntityTypes.Marker);
			//EventTypes
			thnEnv.Add("SET_CAMERA", EventTypes.SetCamera);
			thnEnv.Add ("ATTACH_ENTITY", EventTypes.AttachEntity);
			thnEnv.Add ("START_SPATIAL_PROP_ANIM", EventTypes.StartSpatialPropAnim);
			thnEnv.Add ("START_PSYS", EventTypes.StartPSys);
		}
		public ThnScript (string scriptfile)
		{
			//create a Lua 3.2 interpreter
			var file = Undump.Load (scriptfile);
			var runtime = new LuaRuntime (file);
			runtime.Env = thnEnv;
			//interpret
			runtime.Run ();
			//TODO: Do things with the results

		}
	}
}

