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
using System.Collections.Generic;
using System.Runtime.InteropServices;
namespace LibreLancer
{
	public struct Lighting
	{
		public const int MAX_LIGHTS = 9;
		public static Lighting Empty = new Lighting() { Enabled = false };
        public bool Enabled;
        public Color4 Ambient;
        public LightsArray Lights;
		public FogModes FogMode;
		public float FogDensity;
        public Color4 FogColor;
        public Vector2 FogRange;
		public int NumberOfTilesX;

		public static Lighting Create()
        {
            return new Lighting
            {
                Enabled = true,
                FogColor = Color4.White,
                FogMode = FogModes.None,
                Ambient = Color4.Black
            };
        }
        [StructLayout(LayoutKind.Sequential)]
        public unsafe struct LightsArray
        {
            public LightBitfield SourceEnabled;
            public SystemLighting SourceLighting;
            public int NebulaCount;
            public RenderLight Nebula0;
        }
	}
}

