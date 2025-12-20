// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Numerics;
using LibreLancer.Client.Components;
using LibreLancer.Data.GameData;
using LibreLancer.Data.GameData.Archetypes;
using LibreLancer.Data.GameData.World;
using LibreLancer.Items;
using LibreLancer.Net;
using LibreLancer.Net.Protocol;
using LibreLancer.Physics;
using LibreLancer.Render;
using LibreLancer.Resources;
using LibreLancer.Server;
using LibreLancer.Server.Components;
using LibreLancer.Sounds;
using LibreLancer.World.Components;

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

        static GameWorld()
        {
            EquipmentHandlers.Register();
        }

        public GameWorld(SystemRenderer render, ResourceManager resources, Func<double> timeSource, bool initPhys = true)
        {
            if (initPhys)
                Physics = new PhysicsWorld(resources.ConvexCollection);
            this.timeSource = timeSource;
            if (render != null)
            {
                Renderer = render;
                render.World = this;
                if (initPhys)
                {
                    Renderer.PhysicsHook = () => { Physics.DrawWorld(render.Camera); };
                }
            }

            if (initPhys)
                Projectiles = new ProjectileManager(this);
        }

        public void InitObject(GameObject g, bool reinit, SystemObject obj, ResourceManager res, SoundManager snd, bool server,
            bool changeLoadout = false, ObjectLoadout newLoadout = default, Archetype changedArch = null, OptionalArgument<Sun> changedStar = default,
            Func<int> netId = null)
        {
            if (reinit)
            {
                RemoveObject(g);
                g.ClearAll(Physics);
            }
            var arch = changedArch ?? obj.Archetype;
            var sun = changedStar.Get(obj.Star);
            var loadout = changeLoadout ? newLoadout : obj.Loadout;
            g.InitWithArchetype(arch, sun, res, Renderer != null);
            if (obj.IdsLeft != 0 && obj.IdsRight != 0)
                g.Name = new TradelaneName(g, obj.IdsLeft, obj.IdsRight);
            else
                g.Name = new ObjectName(obj.IdsName);
            g.Nickname = obj.Nickname;
            g.SystemObject = obj;
            g.SetLocalTransform(new Transform3D(obj.Position, obj.Rotation));
            if (loadout != null)
                g.SetLoadout(loadout, snd);
            else if (arch?.Loadout != null)
                g.SetLoadout(arch.Loadout, snd);
            g.World = this;
            if (g.RenderComponent is ModelRenderer mr)
            {
                mr.LODRanges = arch.LODRanges;
                mr.Spin = obj.Spin;
            }
            if (obj.Dock != null)
            {
                if (arch.DockSpheres.Count > 0) //Dock with no DockSphere?
                {
                    if (server)
                    {
                        g.AddComponent(new SDockableComponent(g, obj.Dock, arch.DockSpheres.ToArray()));
                    }
                    g.AddComponent(new DockInfoComponent(g)
                    {
                        Action = obj.Dock,
                        Spheres = arch.DockSpheres.ToArray()
                    });
                }
            }

            if (server)
            {
                g.AddComponent(new SHealthComponent(g) {InfiniteHealth = true, CurrentHealth = 100, MaxHealth = 100});
                if (arch.IsUpdatableSolar() || obj.Faction != null)
                    g.AddComponent(new SSolarComponent(g) { Faction = obj.Faction });
                if (netId != null) {
                    g.NetID = netId();
                    CrcTranslation.Add(new CrcIdMap(g.NetID, g.NicknameCRC));
                }
            }
            AddObject(g);
            g.Register(Physics);
        }

        public GameObject NewObject(SystemObject obj, ResourceManager res, SoundManager snd, bool server,
            bool changeLoadout = false, ObjectLoadout newLoadout = null, Archetype changedArch = null, OptionalArgument<Sun> changedStar = default, Func<int> netId = null)
        {
            var g = new GameObject();
            InitObject(g, false, obj, res, snd, server, changeLoadout, newLoadout, changedArch, changedStar, netId);
            return g;
        }


        public void LoadSystem(StarSystem sys, ResourceManager res, SoundManager snd, bool server, bool loadRenderer = true)
        {
            foreach (var g in objects)
                g.Unregister(Physics);

            if (Renderer != null && loadRenderer)
                Renderer.LoadSystem(sys);

            objects = new List<GameObject>();
            if (Renderer != null && Projectiles != null)
                AddObject((new GameObject()
                    {Nickname = "projectiles", RenderComponent = new ProjectileRenderer(Projectiles)}));

            Func<int> netId = null;
            List<int> toFree = null;
            // Allocate netIds for system objects, use even numbers only
            // so that NPCs can be encoded in fewer bytes
            if (server) {
                toFree = new List<int>();
                netId = () => {
                    toFree.Add(Server.IdGenerator.Allocate());
                    return Server.IdGenerator.Allocate();
                };
            }

            foreach (var obj in sys.Objects)
            {
                NewObject(obj, res, snd, server, false, null, null, default, netId);
            }

            if (server) {
                foreach(var id in toFree)
                    Server.IdGenerator.Free(id);
            }

            foreach (var field in sys.AsteroidFields)
            {
                var g = new GameObject();
                g.Resources = res;
                g.AddComponent(new AsteroidFieldComponent(field, res, g));
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

        public List<CrcIdMap> CrcTranslation = new List<CrcIdMap>();

        public void SetCrcTranslation(IEnumerable<CrcIdMap> translation)
        {
            CrcTranslation = new List<CrcIdMap>(translation);
            foreach (var tr in CrcTranslation) {
                var obj = GetObject(tr.CRC);
                obj.NetID = tr.NetID;
                netIDLookup.Add(tr.NetID, obj);
            }
        }

        public void AddObject(GameObject obj)
        {
            obj.World = this;
            objects.Add(obj);
            if (timeSource != null)
                obj.AnimationComponent?.SetTimeSource(timeSource);
            if (obj.NetID != 0)
                netIDLookup.Add(obj.NetID, obj);
            SpatialLookup.AddObject(obj, obj.WorldTransform.Position);
        }

        public void RemoveObject(GameObject obj)
        {
            if (obj.NetID != 0)
                netIDLookup.Remove(obj.NetID);
            objects.Remove(obj);
            SpatialLookup.RemoveObject(obj);
        }

        public GameObject GetNetObject(int id)
        {
            netIDLookup.TryGetValue(id, out var go);
            return go;
        }

        public GameObject GetObject(ObjNetId id)
        {
            netIDLookup.TryGetValue(id.Value, out var go);
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
            {
                objects[i].PhysicsComponent?.SetOldTransform();
                objects[i].Update(t);
            }
            Physics?.StepSimulation((float) t);
            for (int i = 0; i < objects.Count; i++) {
                objects[i].PhysicsComponent?.Update(t);
                SpatialLookup.UpdatePosition(objects[i], objects[i].WorldTransform.Position);
            }
        }

        public void UpdateInterpolation(float fraction)
        {
            for (int i = 0; i < objects.Count; i++)
            {
                objects[i].PhysicsComponent?.UpdateInterpolation(fraction);
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
                if (rb.Tag is GameObject go && go.Kind == GameObjectKind.Debris) continue;
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
