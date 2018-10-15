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
 * Portions created by the Initial Developer are Copyright (C) 2013-2018
 * the Initial Developer. All Rights Reserved.
 */
using System;
using System.Collections.Generic;
using LibreLancer.GameData;
using LibreLancer.Physics;
namespace LibreLancer
{
    public class GameWorld : IDisposable
	{
		public PhysicsWorld Physics;
		public SystemRenderer Renderer;
		public List<GameObject> Objects = new List<GameObject>();
		public delegate void RenderUpdateHandler(TimeSpan delta);
		public event RenderUpdateHandler RenderUpdate;
		public delegate void PhysicsUpdateHandler(TimeSpan delta);
		public event PhysicsUpdateHandler PhysicsUpdate;
		public GameWorld(SystemRenderer render)
		{
			Renderer = render;
            render.World = this;
            Physics = new PhysicsWorld();
            Physics.FixedUpdate += FixedUpdate;
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
				if (g.RenderComponent != null) g.RenderComponent.LODRanges = obj.Archetype.LODRanges;
				if (obj.Dock != null)
				{
					if (obj.Archetype.DockSpheres.Count > 0) //Dock with no DockSphere?
					{
						g.Components.Add(new DockComponent(g)
						{
							Action = obj.Dock,
							DockAnimation = obj.Archetype.DockSpheres[0].Script,
							DockHardpoint = obj.Archetype.DockSpheres[0].Hardpoint,
							TriggerRadius = obj.Archetype.DockSpheres[0].Radius
						});
					}
				}
				g.Register(Physics);
				Objects.Add(g);
			}
			foreach (var field in sys.AsteroidFields)
			{
				var g = new GameObject();
				g.Resources = res;
				g.World = this;
				g.Components.Add(new AsteroidFieldComponent(field, g));
				Objects.Add(g);
                g.Register(Physics);
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
				obj.Register(Physics);
		}

        void FixedUpdate(TimeSpan timespan)
        {
            if (PhysicsUpdate != null) PhysicsUpdate(timespan);
            for (int i = 0; i < Objects.Count; i++)
                Objects[i].FixedUpdate(timespan);
        }

		public void Update(TimeSpan t)
		{
            Physics.Step(t);
			for (int i = 0; i < Objects.Count; i++)
				Objects[i].Update(t);
			if (RenderUpdate != null)
				RenderUpdate(t);
			Renderer.Update(t);
		}

		public event Action<GameObject, GameMessageKind> MessageBroadcasted;

		public void BroadcastMessage(GameObject sender, GameMessageKind kind)
		{
			if (MessageBroadcasted != null)
				MessageBroadcasted(sender, kind);
		}

        public void Dispose()
        {
            Physics.Dispose();
        }
	}
}

