// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;

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
				LuaPrototype p;
				if (Undump.Load(stream, out p)) {
					var runtime = new LuaBinaryRuntime (p);
					runtime.Env = Env;
					runtime.Run ();
					return runtime.Globals;
				} else {
					stream.Position = 0;
                    return DoString(new StreamReader(stream).ReadToEnd());
                }
			}
		}

        [DllImport("thorncompiler")]
        static extern bool thn_compile(
            [MarshalAs(UnmanagedType.LPUTF8Str)] string input,
            [MarshalAs(UnmanagedType.LPUTF8Str)] string name,
            out IntPtr outputBuffer,
            out int outputSize
            );

        [DllImport("thorncompiler")]
        static extern IntPtr thn_geterror();

        [DllImport("thorncompiler")]
        static extern void thn_free(IntPtr buffer);

		public Dictionary<string, object> DoString(string str, string name = "[string]")
		{
            if (!thn_compile(str, name, out IntPtr buf, out int sz))
            {
                var err = thn_geterror();
                var errstring = UnsafeHelpers.PtrToStringUTF8(err);
                thn_free(err);
                throw new Exception(errstring);
            }
            var compiled = new byte[sz];
            Marshal.Copy(buf, compiled, 0, sz);
            thn_free(buf);
            using (var stream = new MemoryStream(compiled))
            {
                LuaPrototype p;
                if(!Undump.Load(stream, out p))
                    throw new Exception("Undump failed");
                var runtime = new LuaBinaryRuntime(p);
                runtime.Env = Env;
                runtime.Run();
                return runtime.Globals;
            }
        }
        
	}
}

