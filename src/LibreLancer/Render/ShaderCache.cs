// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.IO;
using System.Reflection;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace LibreLancer
{
    static class ShaderCache
    {
        static Dictionary<Strings2, ShaderVariables> shaders = new Dictionary<Strings2, ShaderVariables>();
		public static ShaderVariables Get(string vs, string fs, ShaderCaps caps = ShaderCaps.None)
		{
			var k = new Strings2(vs, fs, caps);
            ShaderVariables sh;
			if (!shaders.TryGetValue(k, out sh)) {
				string prelude;
				if (GLExtensions.Features430)
					prelude = "#version 430\n#define FEATURES430\n" + caps.GetDefines() + "\n#line 0\n";
				else
					prelude = "#version 150\n" + caps.GetDefines() + "\n#line 0\n";
				FLLog.Debug ("Shader", "Compiling [ " + vs + " , " + fs + " ]");
                sh = new ShaderVariables(
					new Shader(
                    prelude +"#define VERTEX_SHADER\n"+ ProcessIncludes(Resources.LoadString("LibreLancer.Shaders." + vs)), 
                        prelude + "#define FRAGMENT_SHADER\n" + ProcessIncludes(Resources.LoadString("LibreLancer.Shaders." + fs)))
                );
                shaders.Add(k, sh);
			}
            return sh;
		}
        //includes in form '#pragma include (file.inc)'
        static Regex findincludes = new Regex(@"^\s*#\s*pragma include\s+[<\(]([^>\)]*)[>\)]\s*", RegexOptions.Multiline | RegexOptions.Compiled);
        static string ProcessIncludes(string src)
		{
			var m = findincludes.Match(src);
			string newsrc = src;
			while (m.Success)
			{
				var inc = ProcessIncludes(Resources.LoadString("LibreLancer.Shaders." + m.Groups[1].Value)) + "\n";
				newsrc = newsrc.Remove(m.Index, m.Length).Insert(m.Index, inc);
				m = findincludes.Match(newsrc);
			}
			return newsrc;
		}

        #region Custom Dictionary Key Structs
        struct Strings2
        {
            public string A;
            public string B;
			public ShaderCaps C;
			public Strings2(string a, string b, ShaderCaps c)
            {
                A = a;
                B = b;
				C = c;
            }
            public override bool Equals(object obj)
            {
                if (!(obj is Strings2))
                    return false;
                var other = (Strings2)obj;
				return other.A == A && other.B == B && other.C == C;
            }
            public override int GetHashCode()
            {
                int hash = 17;
                unchecked
                {
                    hash = hash * 23 + A.GetHashCode();
                    hash = hash * 23 + B.GetHashCode();
					hash += (int)C;
                }
                return hash;
            }
        }
        #endregion
    }
}
