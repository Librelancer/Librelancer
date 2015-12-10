using System;
using System.Collections.Generic;
using System.Text;
using OpenTK.Graphics.OpenGL;
namespace LibreLancer
{
    public static class GLExtensions
    {
		public static List<string> ExtensionList;
        //Global method for checking extensions. Called upon GraphicsDevice creation
		static void PopulateExtensions()
		{
			if (ExtensionList != null)
				return;
			int n = GL.GetInteger (GetPName.NumExtensions);
			ExtensionList = new List<string> (n);
			for (int i = 0; i < n; i++)
				ExtensionList.Add (GL.GetString (StringNameIndexed.Extensions, i));
		}
        public static void CheckExtensions()
        {
			PopulateExtensions ();
			if (!ExtensionList.Contains ("GL_EXT_texture_compression_s3tc")) {
				throw new NotSupportedException ("OPENGL ERROR: Texture Compression (s3tc) not supported");
			}
        }
    }
}