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
        static string LoadEmbedded(string name)
        {
            using(var stream = new StreamReader(Assembly.GetExecutingAssembly().GetManifestResourceStream(name)))
            {
                return stream.ReadToEnd();
            }
        }
    }
}
