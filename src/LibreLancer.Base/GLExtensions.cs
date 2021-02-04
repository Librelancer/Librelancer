// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Collections.Generic;
using System.Text;
namespace LibreLancer
{
    public static class GLExtensions
    {
		public static List<string> ExtensionList;
		static bool? _computeShaders;

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

        static bool? _directStateAccess;
        public static bool DSA
        {
            get
            {
                if (GL.GLES) return false;
                if(_directStateAccess == null)
                {
                    PopulateExtensions();
                    _directStateAccess = ExtensionList.Contains("GL_ARB_direct_state_access");
                    if (_directStateAccess.Value) FLLog.Info("OpenGL", "DSA available");
                }
                return _directStateAccess.Value;
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
				ExtensionList.Add (GL.GetString (GL.GL_EXTENSIONS, i));
            if (GL.GLES) {
                versionInteger = 310;
            }
            else {
                var versionStr = GL.GetString(GL.GL_VERSION).Trim();
                versionInteger = int.Parse(versionStr[0].ToString()) * 100 + int.Parse(versionStr[2].ToString()) * 10;
            }
            FLLog.Debug("GL", "Extensions: \n" + string.Join("\n", ExtensionList));
		}
        public static void CheckExtensions()
        {
            if (GL.GLES)
                return;
			PopulateExtensions ();
			if (!ExtensionList.Contains ("GL_EXT_texture_compression_s3tc")) {
				throw new NotSupportedException ("OPENGL ERROR: Texture Compression (s3tc) not supported");
			}

        }
    }
}