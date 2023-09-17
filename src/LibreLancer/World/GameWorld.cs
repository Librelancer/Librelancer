// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Numerics;
using LibreLancer.Client.Components;
using LibreLancer.GameData;
using LibreLancer.GameData.World;
using LibreLancer.Physics;
using LibreLancer.Render;
using LibreLancer.Server;
using LibreLancer.Server.Components;

namespace LibreLancer.World
{
    public class GameWorld : IDisposable
    {
        public PhysicsWorld Physics;
        public SystemRenderer Renderer;
        public ProjectileManager Projectiles;

        public ServerWorld Server;

        private List<GameObject> objects = new List<GameObject>();
        private Dictionary<int, GameObject> netIDLookup = new Dictionary<int, GameObject>();

        public IReadOnlyList<GameObject> Objects => objects;

        public SpatialLookup SpatialLookup = new SpatialLookup();

        private Func<double> timeSource;

        public GameWorld(SystemRenderer render, Func<double> timeSource, bool initPhys = true)
        {
            if (initPhys)
                Physics = new PhysicsWorld();
            this.timeSource = timeSource;
            if (render != null)
            {
                Renderer = render;
                render.World = this;
                if (initPhys)
                {
                    Renderer.PhysicsHook = () => { Physics.DrawWorld(render.Camera.Frustum, render.Camera.Position); };
                }
            }

            if (initPhys)
                Projectiles = new ProjectileManager(this);
        }

        public GameObject NewObject(SystemObject obj, ResourceManager res, bool server,
            bool changeLoadout = false, ObjectLoadout newLoadout = null, Archetype changedArch = null)
        {
            var arch = changedArch ?? obj.Star ?? obj.Archetype;
            var loadout = changeLoadout ? newLoadout : obj.Loadout;
            var g = new GameObject(changedArch ?? obj.Star ?? arch, res, Renderer != null);
            if (obj.IdsLeft != 0 && obj.IdsRight != 0)
                g.Name = new TradelaneName(g, obj.IdsLeft, obj.IdsRight);
            else
                g.Name = new ObjectName(obj.IdsName);
            g.Nickname = obj.Nickname;
            g.SystemObject = obj;
            g.SetLocalTransform((obj.Rotation ?? Matrix4x4.Identity) * Matrix4x4.CreateTranslation(obj.Position));
            if (loadout != null)
                g.SetLoadout(loadout);
            else if (arch?.Loadout != null)
                g.SetLoadout(arch.Loadout);
            g.World = this;
            g.CollisionGroups = arch.CollisionGroups;
            if (g.RenderComponent != null)
            {
                g.RenderComponent.LODRanges = arch.LODRanges;
                if (g.RenderComponent is ModelRenderer && obj.Spin != Vector3.Zero)
                {
                    g.RenderComponent.Spin = obj.Spin;
                }
            }

            if (obj.Dock != null)
            {
                if (arch.DockSpheres.Count > 0) //Dock with no DockSphere?
                {
                    if (server)
                    {
                        g.Components.Add(new SDockableComponent(g, arch.DockSpheres.ToArray())
                        {
                            Action = obj.Dock,
                        });
                    }
                    g.Components.Add(new CDockComponent(g)
                    {
                        Action = obj.Dock,
                        DockAnimation = arch.DockSpheres[0].Script,
                        DockHardpoint = arch.DockSpheres[0].Hardpoint,
                        TriggerRadius = arch.DockSpheres[0].Radius
                    });
                }
            }

            if (server)
            {
                g.Components.Add(new SHealthComponent(g) {InfiniteHealth = true, CurrentHealth = 100, MaxHealth = 100});
                if (arch.IsUpdatableSolar() || obj.Faction != null)
                    g.Components.Add(new SSolarComponent(g) { Faction = obj.Faction });
            }

            g.Register(Physics);
            AddObject(g);
            return g;
        }


        public void LoadSystem(StarSystem sys, ResourceManager res, bool server, bool loadRenderer = true)
        {
            foreach (var g in objects)
                g.Unregister(Physics);

            if (Renderer != null && loadRenderer)
                Renderer.LoadSystem(sys);

            objects = new List<GameObject>();
            if (Renderer != null)
                AddObject((new GameObject()
                    {Nickname = "projectiles", RenderComponent = new ProjectileRenderer(Projectiles)}));

            foreach (var obj in sys.Objects)
            {
                NewObject(obj, res, server);
            }

            foreach (var field in sys.AsteroidFields)
            {
                var g = new GameObject();
                g.Resources = res;
                g.World = this;
                g.Components.Add(new CAsteroidFieldComponent(field, res, g));
                AddObject(g);
                g.Register(Physics);
            }

            GC.Collect();
        }
#if DEBUG
        public List<Vector3> DebugPoints = new List<Vector3>();
        public bool RenderDebugPoints = false;
        public void DrawDebug(Vector3 point)
        {
            if (RenderDebugPoints)
                DebugPoints.Add(point);
        }
#else
        public void DrawDebug(Vector3 point) {}
#endif

        public void AddObject(GameObject obj)
        {
            objects.Add(obj);
            if (timeSource != null)
                obj.AnimationComponent?.SetTimeSource(timeSource);
            if (obj.NetID != 0)
                netIDLookup.Add(obj.NetID, obj);
            SpatialLookup.AddObject(obj, Vector3.Transform(Vector3.Zero, obj.WorldTransform));
        }

        public void RemoveObject(GameObject obj)
        {
            if (obj.NetID != 0)
                netIDLookup.Remove(obj.NetID);
            objects.Remove(obj);
            SpatialLookup.RemoveObject(obj);
        }

        public GameObject GetFromNetID(int netId)
        {
            netIDLookup.TryGetValue(netId, out var go);
            return go;
        }

        public GameObject GetObject(uint crc)
        {
            if (crc == 0) return null;
            foreach (var obj in objects)
            {
                if (obj.NicknameCRC == crc) return obj;
            }

            return null;
        }

        public GameObject GetObject(string nickname)
        {
            if (nickname == null) return null;
            foreach (var obj in objects)
            {
                if (nickname.Equals(obj.Nickname, StringComparison.OrdinalIgnoreCase)) return obj;
            }

            return null;
        }

        public void RegisterAll()
        {
            foreach (var obj in objects)
                obj.Register(Physics);
        }


        public void Update(double t)
        {
            Projectiles?.Update(t);
            for (int i = 0; i < objects.Count; i++)
                objects[i].Update(t);
            Physics?.StepSimulation((float) t);
            for (int i = 0; i < objects.Count; i++)
                objects[i].PhysicsComponent?.Update(t);
            for (int i = 0; i < objects.Count; i++)
            {
                SpatialLookup.UpdatePosition(objects[i], Vector3.Transform(Vector3.Zero, objects[i].WorldTransform));
            }
        }

        public void RenderUpdate(double t)
        {
            if (Renderer != null)
            {
#if DEBUG
                Renderer.UseDebugPoints(DebugPoints);
#endif
                Renderer.Update(t);
            }

            for (int i = 0; i < objects.Count; i++)
                objects[i].RenderUpdate(t);
        }

        public event Action<GameObject, GameMessageKind> MessageBroadcasted;

        public void BroadcastMessage(GameObject sender, GameMessageKind kind)
        {
            if (MessageBroadcasted != null)
                MessageBroadcasted(sender, kind);
        }

        public GameObject GetSelection(ICamera camera, GameObject self, float x, float y, float vpWidth, float vpHeight)
        {

            var cameraProjection = camera.Projection;
            var cameraView = camera.View;

            var vp = new Vector2(vpWidth, vpHeight);
            var start = Vector3Ex.UnProject(new Vector3(x, y, 0f), cameraProjection, cameraView, vp);
			var end = Vector3Ex.UnProject(new Vector3(x, y, 1f), cameraProjection, cameraView, vp);
            var dir = (end - start).Normalized();

			PhysicsObject rb;
            var result = SelectionCast(
                camera,
				start,
				dir,
				50000,
                self,
				out rb
			);
			if (result && rb.Tag is GameObject)
				return (GameObject)rb.Tag;
			return null;
		}

		//Select by bounding box, not by mesh
        bool SelectionCast(ICamera camera, Vector3 rayOrigin, Vector3 direction, float maxDist, GameObject self, out PhysicsObject body)
		{
			float dist = float.MaxValue;
			body = null;
			var jitterDir = direction * maxDist;
            var md2 = maxDist * maxDist;
			foreach (var rb in Physics.Objects)
            {
				if (rb.Tag == self) continue;
                if (Vector3.DistanceSquared(rb.Position, camera.Position) > md2) continue;
                if (rb.Collider is SphereCollider)
				{
					//Test spheres
					var sph = (SphereCollider)rb.Collider;
                    var ray = new Ray(rayOrigin, direction);
                    var sphere = new BoundingSphere(rb.Position, sph.Radius);
                    var res = ray.Intersects(sphere);
                    if (res != null)
                    {
                        var p2 = rayOrigin + (direction * res.Value);
                        if (res == 0.0) p2 = rb.Position;
                        var nd = Vector3.DistanceSquared(p2, camera.Position);
                        if (nd < dist)
                        {
                            dist = nd;
                            body = rb;
                        }
                    }
                }
				else
				{
					//var tag = rb.Tag as GameObject;
                    var box = rb.GetBoundingBox();
                    if (!rb.GetBoundingBox().RayIntersect(ref rayOrigin, ref jitterDir)) continue;
                    var nd = Vector3.DistanceSquared(rb.Position, camera.Position);
                    if (nd < dist)
                    {
                        dist = nd;
                        body = rb;
                    }
					/*if (tag == null || tag.CmpParts.Count == 0)
					{
						//Single part
						var nd = Vector3.DistanceSquared(rb.Position, camera.Position);
						if (nd < dist)
						{
							dist = nd;
							body = rb;
						}
					}
					else
					{
						//Test by cmp parts
						var sh = (CompoundSurShape)rb.Shape;
						for (int i = 0; i < sh.Shapes.Length; i++)
						{
							sh.Shapes[i].UpdateBoundingBox();
							var bb = sh.Shapes[i].BoundingBox;
							bb.Min += rb.Position;
							bb.Max += rb.Position;
							if (bb.RayIntersect(ref rayOrigin, ref jitterDir))
							{

								var nd = Vector3.DistanceSquared(rb.Position, camera.Position);
								if (nd < dist)
								{
									dist = nd;
									body = rb;
								}
								break;
							}
						}
					}*/
				}
			}
			return body != null;
		}

        public void Dispose()
        {
            Physics?.Dispose();
        }
    }
}
