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
                }
                return _anisotropy.Value;
            }
        }

		static bool? _features430;
		public static bool Features430
		{
			get
			{
				if (GL.GLES) return false;
				if (_features430 == null)
				{
					PopulateExtensions();
					_features430 = ExtensionList.Contains("GL_ARB_shader_storage_buffer_object");
				}
				return _features430.Value;
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
				ExtensionList.Add (GL.GetString (GL.GL_EXTENSIONS, i));
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