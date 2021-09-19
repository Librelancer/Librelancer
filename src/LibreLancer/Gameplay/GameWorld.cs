// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Numerics;
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

        public ServerWorld Server;

		public List<GameObject> Objects = new List<GameObject>();
		public delegate void RenderUpdateHandler(double delta);
		public event RenderUpdateHandler RenderUpdate;
		public delegate void PhysicsUpdateHandler(double delta);
		public event PhysicsUpdateHandler PhysicsUpdate;

		public GameWorld(SystemRenderer render, bool initPhys = true)
		{
            if(initPhys)
            Physics = new PhysicsWorld();
            if (render != null)
            {
                Renderer = render;
                render.World = this;
                if (initPhys)
                {
                    Renderer.PhysicsHook = () => { Physics.DrawWorld(); };
                }
            }
            if(initPhys)
            Physics.FixedUpdate += FixedUpdate;
            Projectiles = new ProjectileManager(this);
		}

        public void LoadSystem(StarSystem sys, ResourceManager res, double timeOffset = 0)
		{
            foreach (var g in Objects)
                g.Unregister(Physics);

            if(Renderer != null) Renderer.StarSystem = sys;
           
            Objects = new List<GameObject>();
            if(Renderer != null) Objects.Add((new GameObject() { Nickname = "projectiles", RenderComponent = new ProjectileRenderer(Projectiles) }));

            foreach (var obj in sys.Objects)
            {
                var g = new GameObject(obj.Archetype, res, Renderer != null);
                g.Name = obj.DisplayName;
                g.Nickname = obj.Nickname;
                g.SetLocalTransform((obj.Rotation ?? Matrix4x4.Identity) * Matrix4x4.CreateTranslation(obj.Position));
                g.SetLoadout(obj.Loadout, obj.LoadoutNoHardpoint, timeOffset);
                g.World = this;
                g.CollisionGroups = obj.Archetype.CollisionGroups;
                if (g.RenderComponent != null)
                {
                    g.RenderComponent.LODRanges = obj.Archetype.LODRanges;
                    if (g.RenderComponent is ModelRenderer && obj.Spin != Vector3.Zero) {
                        g.RenderComponent.Spin = obj.Spin;
                    }
                }
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
                g.Components.Add(new CAsteroidFieldComponent(field, g));
                Objects.Add(g);
                g.Register(Physics);
            }
            GC.Collect();
        }
#if DEBUG
        public List<Vector3> DebugPoints = new List<Vector3>();
        public bool RenderDebugPoints = false;
        public void DrawDebug(Vector3 point)
        {
            if(RenderDebugPoints)
                DebugPoints.Add(point);
        }
        #else
        public void DrawDebug(Vector3 point) {}
#endif

        public GameObject GetObject(uint crc)
        {
            if (crc == 0) return null;
            foreach (var obj in Objects)
            {
                if (obj.NicknameCRC == crc) return obj;
            }
            return null;
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

        void FixedUpdate(double time)
        {
            Projectiles.FixedUpdate(time);
            for (int i = 0; i < Objects.Count; i++)
                Objects[i].FixedUpdate(time);
            if (PhysicsUpdate != null) PhysicsUpdate(time);
        }

        public void Update(double t)
		{
            Physics?.Step(t);
			for (int i = 0; i < Objects.Count; i++)
				Objects[i].Update(t);
            RenderUpdate?.Invoke(t);
            if (Renderer != null)
            {
                #if DEBUG
                Renderer.UseDebugPoints(DebugPoints);
                #endif
                Renderer.Update(t);
            }
		}

		public event Action<GameObject, GameMessageKind> MessageBroadcasted;

		public void BroadcastMessage(GameObject sender, GameMessageKind kind)
		{
			if (MessageBroadcasted != null)
				MessageBroadcasted(sender, kind);
		}

        public void Dispose()
        {
            Physics?.Dispose();
        }
	}
}

