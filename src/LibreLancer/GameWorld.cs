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
using LibreLancer.GameData;

namespace LibreLancer
{
	public class GameWorld
	{
		public SystemRenderer Renderer;
		public List<GameObject> Objects = new List<GameObject>();

		public GameWorld(SystemRenderer render)
		{
			Renderer = render;
		}

		public void LoadSystem(StarSystem sys)
		{
			foreach (var g in Objects)
				g.Unregister();
			
			Renderer.StarSystem = sys;

			Objects = new List<GameObject>();

			foreach (var obj in sys.Objects)
			{
				var g = new GameObject(obj.Archetype, true);
				g.Name = obj.DisplayName;
				g.Nickname = obj.Nickname;
				g.Transform = (obj.Rotation ?? Matrix4.Identity) * Matrix4.CreateTranslation(obj.Position);
				g.SetLoadout(obj.Loadout);
				g.StaticPosition = obj.Position;
				g.Register(Renderer);
				Objects.Add(g);
			}

			GC.Collect();
		}

		public void Update(TimeSpan t)
		{
			Renderer.Update(t);
			foreach (var g in Objects)
				g.Update(t);
		}
	}
}

