// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

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
        public ProjectileManager Projectiles;

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
            Projectiles = new ProjectileManager(this);
		}

        public void LoadSystem(StarSystem sys, ResourceManager res)
		{
            foreach (var g in Objects)
                g.Unregister(Physics);

            Renderer.StarSystem = sys;

            Objects = new List<GameObject>();
            Objects.Add((new GameObject() { Nickname = "projectiles", RenderComponent = new ProjectileRenderer(Projectiles) }));

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
            Projectiles.FixedUpdate(timespan);
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

