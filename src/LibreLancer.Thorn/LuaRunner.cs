// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System.Collections.Generic;
using System.IO;

namespace LibreLancer.Thorn
{
	public class LuaRunner
	{
		public Dictionary<string, object> Env;

		public LuaRunner(Dictionary<string, object> env)
		{
			Env = env;
		}

		public Dictionary<string,object> DoFile(string filename)
		{
            return DoStream(File.OpenRead(filename));            
        }

		public Dictionary<string, object> DoString(string str, string name = "[string]")
		{
            var compiledBytes = LuaCompiler.Compile(str, name);
            return DoBytes(compiledBytes);
        }

        public Dictionary<string, object> DoBytes(byte[] bytes)
        {
            return DoStream(new MemoryStream(bytes));
        }

        public Dictionary<string, object> DoStream(Stream stream)
        {
            if (Undump.Load(stream, out var p))
            {
                var runtime = new LuaBinaryRuntime(p) { Env = Env };
                runtime.Run();
                return runtime.Globals;
            }
            else
            {
                stream.Position = 0;
                return DoString(new StreamReader(stream).ReadToEnd());
            }
        }
    }
}

