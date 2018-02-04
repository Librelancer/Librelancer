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

        bool needsHashCalculation;
        int _hash;

		public int Hash
		{
			get
			{
				if (needsHashCalculation)
					CalculateHash();
				return _hash;
			}
		}

		void CalculateHash()
		{
			needsHashCalculation = false;
			if (!Enabled)
			{
				_hash = 0;
				return;
			}
			_hash = 17;
			unchecked
			{
				_hash = _hash * 23 + Ambient.GetHashCode();
                for (int i = 0; i < Lights.Count; i++)
                {
                    _hash = _hash * 23 + Lights[i].GetHashCode();
                }
				if (FogMode != FogModes.None)
				{
					_hash = _hash * 23 + FogColor.GetHashCode();
					_hash = _hash * 23 + FogRange.GetHashCode();
				}
			}
		}

		public static Lighting Create()
        {
            return new Lighting
            {
                needsHashCalculation = true,
                Enabled = true,
                FogColor = Color4.White,
                FogMode = FogModes.None,
                Ambient = Color4.Black
            };
        }
        [StructLayout(LayoutKind.Sequential)]
        public unsafe struct LightsArray
        {
            static int Stride = Marshal.SizeOf(typeof(RenderLight));
            public int Count;
            RenderLight _l0;
            RenderLight _l1;
            RenderLight _l2;
            RenderLight _l3;
            RenderLight _l4;
            RenderLight _l5;
            RenderLight _l6;
            RenderLight _l7;
            RenderLight _l8;
            public unsafe RenderLight this [int index]
            {
                get
                {
                    if (index < 0 || index > Count)
                        throw new IndexOutOfRangeException();
                    return GetLight(ref this, index);
                }
                private set
                {
                    if (index < 0 || index > Count)
                        throw new IndexOutOfRangeException();
                    SetLight(ref this, index, value);
                }
            }
            public void Add(RenderLight light)
            {
                if (Count == 8)
                    throw new Exception("Too many lights!");
                SetLight(ref this, Count++, light);
            }
            static RenderLight GetLight(ref LightsArray lt, int index)
            {
                fixed(LightsArray *lights = &lt)
                {
                    var ptr = (ulong)lights;
                    ptr += sizeof(int);
                    ptr += (ulong)(Stride * index);
                    return *((RenderLight*)ptr);
                }
            }
            static void SetLight(ref LightsArray lt, int index, RenderLight value)
            {
                fixed (LightsArray* lights = &lt)
                {
                    var ptr = (ulong)lights;
                    ptr += sizeof(int);
                    ptr += (ulong)(Stride * index);
                    *((RenderLight*)ptr) = value;
                }
            }
        }
	}
}

