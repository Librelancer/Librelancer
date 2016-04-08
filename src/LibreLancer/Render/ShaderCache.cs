using System;
using System.IO;
using System.Reflection;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
					LoadEmbedded ("LibreLancer.Shaders." + vs), LoadEmbedded ("LibreLancer.Shaders." + fs)
				));
			}
			return shaders [k];
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
