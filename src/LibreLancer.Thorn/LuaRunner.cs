using System;
using System.Collections.Generic;
using System.IO;
namespace LibreLancer.Thorn
{
	public class LuaRunner
	{
		public Dictionary<string,object> Env;
		public LuaRunner(Dictionary<string,object> env)
		{
			Env = env;
		}

		public Dictionary<string,object> DoFile(string filename)
		{
			using (var stream = File.OpenRead (filename)) {
				LuaPrototype p = null;
				try {
					p = Undump.Load(stream);
				} catch (Exception) {
					stream.Position = 0;
				}
				if (p != null) {
					var runtime = new LuaBinaryRuntime (p);
					runtime.Env = Env;
					runtime.Run ();
					return runtime.Globals;
				} else {
					throw new NotImplementedException ();
				}
			}
		}

	}
}

