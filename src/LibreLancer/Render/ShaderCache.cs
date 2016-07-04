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
using System.IO;
using System.Reflection;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace LibreLancer
{
    static class ShaderCache
    {
        static Dictionary<Tuple<string, string>, Shader> shaders = new Dictionary<Tuple<string, string>, Shader>();
		static Dictionary<Tuple<string, string,string >, Shader> shaderGeo = new Dictionary<Tuple<string, string,string>, Shader>();
        public static Shader Get(string vs, string fs)
		{
			
			var k = new Tuple<string, string> (vs, fs);
			if (!shaders.ContainsKey (k)) {
				FLLog.Debug ("Shader", "Compiling [ " + vs + " , " + fs + " ]");
				shaders.Add (k, new Shader (
					LoadEmbedded ("LibreLancer.Shaders." + vs), ProcessIncludes(LoadEmbedded ("LibreLancer.Shaders." + fs))
				));
			}
			return shaders [k];
		}
		//includes in form '#pragma include (file.inc)'
		static string ProcessIncludes(string src)
		{
			Regex findincludes = new Regex(@"^\s*#\s*pragma include\s+[<\(]([^>\)]*)[>\)]\s*", RegexOptions.Multiline);
			var m = findincludes.Match(src);
			string newsrc = src;
			while (m.Success)
			{
				var inc = ProcessIncludes(LoadEmbedded("LibreLancer.Shaders." + m.Groups[1].Value));
				newsrc = newsrc.Remove(m.Index, m.Length).Insert(m.Index, inc);
				m = findincludes.Match(newsrc);
			}
			return newsrc;
		}
		public static Shader Get(string vs, string fs, string gs)
		{
			var k = new Tuple<string, string,string> (vs, fs, gs);
			if (!shaderGeo.ContainsKey (k)) {
				FLLog.Debug ("Shader", "Compiling [ " + vs + " , " + fs + " , " + gs + " ]");
				shaderGeo.Add (k, new Shader (
					LoadEmbedded ("LibreLancer.Shaders." + vs), 
					LoadEmbedded ("LibreLancer.Shaders." + fs),
					LoadEmbedded("LibreLancer.Shaders." + gs)
				));
			}
			return shaderGeo [k];
		}
        static string LoadEmbedded(string name)
        {
            using(var stream = new StreamReader(Assembly.GetExecutingAssembly().GetManifestResourceStream(name)))
            {
                return stream.ReadToEnd();
            }
        }
    }
}
