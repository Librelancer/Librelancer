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
namespace LibreLancer.GameData
{
	public class AsteroidField
	{
		public Zone Zone;
		public AsteroidBand Band;
		public AsteroidCubeRotation CubeRotation;
		public List<StaticAsteroid> Cube;
		public List<ExclusionZone> ExclusionZones;
		public int CubeSize;
		public bool AllowMultipleMaterials = true;
		public float FillDist { get; private set; }
		public void SetFillDist(int fillDist)
		{
			FillDist = fillDist * FILLDIST_MULTIPLIER;
		}
		public float EmptyCubeFrequency;
		public int BillboardCount;
		public float BillboardDistance;
		public float BillboardFadePercentage;
		public TextureShape BillboardShape;
		public Vector2 BillboardSize;
		public Color3f BillboardTint;

		//Multiplier hardcoded in Freelancer's common.dll
		const float FILLDIST_MULTIPLIER = 1.74f;

	}
}

