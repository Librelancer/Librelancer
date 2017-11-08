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
using LibreLancer.Jitter;
using LibreLancer.Jitter.Collision;
using LibreLancer.Jitter.LinearMath;
namespace LibreLancer
{
	
	public class GameWorld
	{
		public World Physics;
		public SystemRenderer Renderer;
		public List<GameObject> Objects = new List<GameObject>();
		public delegate void RenderUpdateHandler(TimeSpan delta);
		public event RenderUpdateHandler RenderUpdate;
		public delegate void PhysicsUpdateHandler(TimeSpan delta);
		public event PhysicsUpdateHandler PhysicsUpdate;
		public GameWorld(SystemRenderer render)
		{
			Renderer = render;
			Physics = new World(new CollisionSystemSAP());
			Physics.CollisionSystem.EnableSpeculativeContacts = true;
			Physics.Gravity = Vector3.Zero;
			Physics.SetDampingFactors(1, 1);
			Physics.Events.PreStep += Events_PreStep;
		}

		public void LoadSystem(StarSystem sys, ResourceManager res)
		{
			foreach (var g in Objects)
				g.Unregister(Physics);
			
			Renderer.StarSystem = sys;

			Objects = new List<GameObject>();

			foreach (var obj in sys.Objects)
			{
				var g = new GameObject(obj.Archetype, res, true);
				g.Name = obj.DisplayName;
				g.Nickname = obj.Nickname;
				g.Transform = (obj.Rotation ?? Matrix4.Identity) * Matrix4.CreateTranslation(obj.Position);
				g.SetLoadout(obj.Loadout, obj.LoadoutNoHardpoint);
				g.StaticPosition = obj.Position;
				g.World = this;
				if (obj.Dock != null)
				{
					g.Components.Add(new DockComponent(g) { 
						Action = obj.Dock,
						DockAnimation = obj.Archetype.DockSpheres[0].Script,
						DockHardpoint = obj.Archetype.DockSpheres[0].Hardpoint,
						TriggerRadius = obj.Archetype.DockSpheres[0].Radius
					});
				}
				g.Register(Renderer, Physics);
				Objects.Add(g);
			}

			GC.Collect();
		}

		public GameObject GetObject(string nickname)
		{
			if (nickname == null) return null;
			foreach (var obj in Objects)
			{
				if (obj.Nickname == nickname) return obj;
			}
			return null;
		}

		public void RegisterAll()
		{
			foreach (var obj in Objects)
				obj.Register(Renderer, Physics);
		}

		void Events_PreStep(float timestep)
		{
			if (PhysicsUpdate != null)
				PhysicsUpdate(TimeSpan.FromSeconds(timestep));
			for (int i = 0; i < Objects.Count; i++)
				Objects[i].FixedUpdate(TimeSpan.FromSeconds(timestep));
		}

		public void Update(TimeSpan t)
		{
			Physics.Step((float)t.TotalSeconds, true, 1f / 120f, 6);
			for (int i = 0; i < Objects.Count; i++)
				Objects[i].Update(t);
			if (RenderUpdate != null)
				RenderUpdate(t);
			Renderer.Update(t);
		}
	}
}

