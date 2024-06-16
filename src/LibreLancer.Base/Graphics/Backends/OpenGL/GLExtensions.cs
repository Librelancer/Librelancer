// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System.Collections.Generic;

namespace LibreLancer.Graphics.Backends.OpenGL
{
    static class GLExtensions
    {
		public static List<string> ExtensionList;
		static bool? _computeShaders;
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
		public static bool ComputeShaders
		{
			get
			{
				if (GL.GLES) return false;
				if (_computeShaders == null)
				{
					PopulateExtensions();
					_computeShaders = ExtensionList.Contains("GL_ARB_compute_shader");
				}
				return _computeShaders.Value;
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

		public static bool Features430
		{
			get
			{
				if (GL.GLES) return false;
                return versionInteger >= 430;
            }
		}

        private static int versionInteger;
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
            if (GL.GLES) {
                versionInteger = 310;
            }
            else {
                var versionStr = GL.GetString(GL.GL_VERSION).Trim();
                versionInteger = int.Parse(versionStr[0].ToString()) * 100 + int.Parse(versionStr[2].ToString()) * 10;
            }
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
