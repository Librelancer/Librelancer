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
	public class StarSystem
	{
		public string Id;
		public string Name;
		//Background
		public Color4 BackgroundColor;
		//Starsphere
		public IDrawable StarsBasic;
		public IDrawable StarsComplex;
		public IDrawable StarsNebula;
		//Lighting
		public Color4 AmbientColor;
		public List<RenderLight> LightSources = new List<RenderLight>();
		//Objects
		public List<SystemObject> Objects = new List<SystemObject>();
		//Nebulae
		public List<Nebula> Nebulae = new List<Nebula>();
		//Asteroid Fields
		public List<AsteroidField> AsteroidFields = new List<AsteroidField>();
		//Zones
		public List<Zone> Zones = new List<Zone>();
		//Music
		public string MusicSpace;
		//Clipping
		public float FarClip;
		public StarSystem ()
		{
		}
	}
}

