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
using LibreLancer.Utf.Mat;

namespace LibreLancer.GameData
{
	public class Nebula
	{
		public Zone Zone;
		//Exterior
		public string ExteriorFill;
		public Color4 ExteriorColor;
		public bool HasExteriorBits = false;
		public int ExteriorMinBits;
		public int ExteriorMaxBits;
		public float ExteriorBitRadius;
		public float ExteriorBitRandomVariation;
		public float ExteriorMoveBitPercent;
		public WeightedRandomCollection<CloudShape> ExteriorCloudShapes;
		//Fog + Lighting
		public bool FogEnabled;
		public Color4 FogColor;
		public Vector2 FogRange;
		public Color4? AmbientColor;
		//Interior
		public bool HasInteriorClouds = false;
		public WeightedRandomCollection<CloudShape> InteriorCloudShapes;
		public Color3f InteriorCloudColorA;
		public Color3f InteriorCloudColorB;
		public int InteriorCloudRadius;
		public int InteriorCloudCount;
		public int InteriorCloudMaxDistance;
		public Vector2 InteriorCloudFadeDistance;
		public float InteriorCloudMaxAlpha;
		public float InteriorCloudDrift;
		//Exclusion
		public List<ExclusionZone> ExclusionZones;
	}
	public struct CloudShape
	{
		public string Texture;
		public RectangleF Dimensions;
		public CloudShape(string tex, RectangleF dim)
		{
			Texture = tex;
			Dimensions = dim;
		}
	}
}

