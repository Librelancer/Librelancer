// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using LibreLancer.Thorn.VM;

namespace LibreLancer.Thorn
{
	public class ThornRunner
	{
		public Dictionary<string, object> Env;
        public ReadFileCallback ReadFile;
        public bool Log = true;

		public ThornRunner(Dictionary<string, object> env, ReadFileCallback readFile)
		{
			Env = env;
            ReadFile = readFile;
        }

		public Dictionary<string, object> DoString(string str, string name = "[string]")
		{
            var compiledBytes = ThornCompiler.Compile(str, name);
            return DoBytes(compiledBytes, name);
        }

        public Dictionary<string, object> DoBytes(byte[] bytes, string name = "[bytes]")
        {
            return DoStream(new MemoryStream(bytes), name);
        }

        public Dictionary<string, object> DoStream(Stream stream, string source = "[stream]")
        {
            var builder = new StringBuilder();
            try
            {
                var runtime = new ThornRuntime();
                runtime.Env = new Dictionary<string, object>(Env);
                runtime.OnReadFile = ReadFile;
                runtime.OnStdout += e => builder.Append(e);
                runtime.SetBuiltins();
                runtime.DoStream(stream, source);
                return runtime.Globals;
            }
            finally
            {
                if(builder.Length > 0)
                    FLLog.Info("Thorn", $"Log from '{source}': {builder}");
            }
        }
    }
}

