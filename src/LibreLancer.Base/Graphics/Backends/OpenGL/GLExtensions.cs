// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System.Collections.Generic;

namespace LibreLancer.Graphics.Backends.OpenGL
{
    static class GLExtensions
    {
		public static List<string> ExtensionList;
        static bool s3tc;
        static bool rgtc;
        private static bool debugInfo;
        public static bool S3TC
        {
            get
            {
                PopulateExtensions();
                return s3tc;
            }
        }

        public static bool RGTC
        {
            get
            {
                PopulateExtensions();
                return rgtc;
            }
        }

        public static bool DebugInfo
        {
            get
            {
                PopulateExtensions();
                return debugInfo;
            }
        }

        static bool? _anisotropy;
        public static bool Anisotropy
        {
            get
            {
                if(_anisotropy == null)
                {
                    PopulateExtensions();
                    _anisotropy = ExtensionList.Contains("GL_EXT_texture_filter_anisotropic");
                    if (_anisotropy.Value) FLLog.Info("OpenGL", "Anisotropy available");
                }
                return _anisotropy.Value;
            }
        }

        //Global method for checking extensions. Called upon GraphicsDevice creation
        public static void PopulateExtensions()
		{
			if (ExtensionList != null)
				return;
			int n;
			GL.GetIntegerv (GL.GL_NUM_EXTENSIONS, out n);
			ExtensionList = new List<string> (n);
			for (int i = 0; i < n; i++)
				ExtensionList.Add (GL.GetStringi (GL.GL_EXTENSIONS, i));
            FLLog.Info("GL", "Extensions: \n" + string.Join(", ", ExtensionList));
            s3tc = ExtensionList.Contains("GL_EXT_texture_compression_s3tc");
            // RGTC support is core in Desktop GL (at least the extension is not reported on macOS)
            rgtc = !GL.GLES || ExtensionList.Contains("GL_EXT_texture_compression_rgtc");
            debugInfo = ExtensionList.Contains("GL_KHR_debug");
            if (debugInfo)
                FLLog.Info("GL", "KHR_debug supported");
            if (s3tc)
            {
                FLLog.Info("GL", "S3TC extension supported");
            }
            else
            {
                FLLog.Info("GL", "S3TC extension not supported");
            }

            if (GL.GLES)
            {
                if (rgtc)
                    FLLog.Info("GL", "RGTC extension supported");
                else
                    FLLog.Info("GL", "RGTC extension not supported");
            }
        }
    }
}
