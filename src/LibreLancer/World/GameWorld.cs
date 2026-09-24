// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
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
        private const float SelectionMaxDistance = 10000000f;
        private const float TradelaneSelectionHeight = 0.25f;

        public readonly PhysicsWorld? Physics;
        public readonly SystemRenderer? Renderer;
        public readonly SoundManager? Sounds;
        public readonly ProjectileManager Projectiles = null!;

        public ServerWorld? Server;

        private List<GameObject> objects = [];
        private Dictionary<int, GameObject> netIDLookup = new();

        public IReadOnlyList<GameObject> Objects => objects;

        public IReadOnlyList<GameObject> AllObjects => objects;

        public readonly SpatialLookup SpatialLookup = new();
        public ZoneLookup? Zones;

        private Func<double>? timeSource;
        private readonly ResourceManager? resources;
        int atmosphereVersion = 0;
        int atmosphereSetVersion = -1;
        private BoundingSphere[] atmospheres = null!;

        static GameWorld()
        {
            EquipmentHandlers.Register();
        }

        public GameWorld(SystemRenderer? render, SoundManager? sounds, ResourceManager? resources, Func<double>? timeSource,
            bool initPhys = true)
        {
            if (initPhys)
            {
                Physics = new PhysicsWorld(resources.ConvexCollection);
            }

            this.timeSource = timeSource;
            this.Sounds = sounds;
            this.resources = resources;

            if (render != null)
            {
                Renderer = render;
                render.World = this;

                if (initPhys)
                {
                    Renderer.PhysicsHook = () => { Physics!.DrawWorld(render.Camera); };
                }
            }

            if (initPhys)
            {
                Projectiles = new ProjectileManager(this);
            }
        }

        public void SpawnTempFx(ResolvedFx? fx, Vector3 position)
        {
            if (fx == null || Renderer == null || resources == null)
                return;
            var particle = fx.GetEffect(resources);
            Renderer.SpawnTempFx(particle, position);
            if (fx.Sound != null && Sounds != null)
            {
                var snd = Sounds.GetInstance(fx.Sound.Nickname, 0, -1, -1, position);
                snd?.Play();
            }
        }

        public void InitObject(GameObject g, bool reinit, SystemObject obj, ResourceManager res, SoundManager? snd,
            bool server,
            bool changeLoadout = false, ObjectLoadout? newLoadout = null, Archetype? changedArch = null,
            OptionalArgument<Sun> changedStar = default,
            Func<int>? netId = null)
        {
            if (reinit)
            {
                RemoveObject(g);
                g.ClearAll(this);
            }

            var arch = (changedArch ?? obj.Archetype)!;
            var sun = changedStar.Get(obj.Star!);
            var loadout = changeLoadout ? newLoadout : obj.Loadout;
            g.InitWithArchetype(arch, sun, res, Renderer != null);

            if (obj.IdsLeft != 0 && obj.IdsRight != 0)
            {
                g.Name = new TradelaneName(g, obj.IdsLeft, obj.IdsRight);
            }
            else
            {
                g.Name = new ObjectName(obj.IdsName);
            }

            g.Nickname = obj.Nickname;
            g.SystemObject = obj;
            g.SetLocalTransform(new Transform3D(obj.Position, obj.Rotation));

            if (loadout != null)
            {
                g.SetLoadout(loadout, res, snd);
            }
            else if (arch.Loadout != null)
            {
                g.SetLoadout(arch.Loadout, res, snd);
            }

            if (g.RenderComponent is ModelRenderer mr)
            {
                mr.LODRanges = arch.LODRanges;
                mr.Spin = obj.Spin;
            }

            // Dock with no DockSphere?
            if (obj.Dock != null && arch.DockSpheres.Count > 0)
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

            if (server)
            {
                g.AddComponent(new SHealthComponent(g) { InfiniteHealth = true, CurrentHealth = 100, MaxHealth = 100 });

                if (arch.IsUpdatableSolar() || obj.Faction != null)
                {
                    g.AddComponent(new SSolarComponent(g) { Faction = obj.Faction });
                }

                if (netId != null)
                {
                    g.NetID = netId();
                    CrcTranslation.Add(new CrcIdMap(g.NetID, g.NicknameCRC));
                }
            }

            AddObject(g);
            g.Register(this);
        }

        public void NewObject(SystemObject obj, ResourceManager res, SoundManager? snd, bool server,
            bool changeLoadout = false, ObjectLoadout? newLoadout = null, Archetype? changedArch = null,
            OptionalArgument<Sun> changedStar = default, Func<int>? netId = null)
        {
            var g = new GameObject();
            InitObject(g, false, obj, res, snd, server, changeLoadout, newLoadout, changedArch, changedStar, netId);
        }

        public void LoadSystem(StarSystem sys, ResourceManager res, SoundManager? snd, bool server,
            bool loadRenderer = true)
        {
            Zones?.Dispose();
            Zones = new(sys.Zones);

            if (Physics is not null)
            {
                foreach (var g in objects)
                {
                    g.Unregister(this);
                }
            }

            if (Renderer != null && loadRenderer)
            {
                Renderer.LoadSystem(sys);
            }

            objects = [];

            if (Renderer != null)
            {
                AddObject((new GameObject()
                { Nickname = "projectiles", RenderComponent = new ProjectileRenderer(Projectiles) }));
            }

            Func<int>? netId = null;
            List<int> toFree = [];

            // Allocate netIds for system objects, use even numbers only
            // so that NPCs can be encoded in fewer bytes
            if (server)
            {
                netId = () =>
                {
                    toFree.Add(Server!.IdGenerator.Allocate());
                    return Server.IdGenerator.Allocate();
                };
            }

            foreach (var obj in sys.Objects)
            {
                NewObject(obj, res, snd, server, false, null, null, default, netId);
            }

            if (server)
            {
                foreach (var id in toFree)
                {
                    Server!.IdGenerator.Free(id);
                }
            }

            foreach (var field in sys.AsteroidFields)
            {
                var g = new GameObject();
                g.AddComponent(new AsteroidFieldComponent(field, res, g));
                AddObject(g);
                g.Register(this);
            }
        }
        public List<SystemRenderer.DebugLine> DebugLines = new List<SystemRenderer.DebugLine>();
        public bool RenderAutopilotDebug = false;
        public bool RenderFormationDebug = false;

        public void DrawDebugLine(Vector3 start, Vector3 end, Color4 color)
        {
            if (RenderAutopilotDebug)
                DebugLines.Add(new SystemRenderer.DebugLine(start, end, color));
        }

        public void DrawFormationDebugLine(Vector3 start, Vector3 end, Color4 color)
        {
            if (RenderFormationDebug)
                DebugLines.Add(new SystemRenderer.DebugLine(start, end, color));
        }

#if DEBUG
        public List<Vector3> DebugPoints = new List<Vector3>();
        public bool RenderDebugPoints = false;
        public void DrawDebug(Vector3 point)
        {
            if (RenderDebugPoints)
                DebugPoints.Add(point);
        }

        public void DrawFormationDebug(Vector3 point)
        {
            if (RenderFormationDebug)
                DebugPoints.Add(point);
        }
#else
        public void DrawDebug(Vector3 point)
        {
        }

        public void DrawFormationDebug(Vector3 point)
        {
        }
#endif

        public List<CrcIdMap> CrcTranslation = [];

        public void SetCrcTranslation(IEnumerable<CrcIdMap> translation)
        {
            CrcTranslation = new List<CrcIdMap>(translation);

            foreach (var tr in CrcTranslation)
            {
                var obj = GetObject(tr.CRC)!;
                obj.NetID = tr.NetID;
                netIDLookup.Add(tr.NetID, obj);
            }
        }

        public void AddObject(GameObject obj)
        {
            objects.Add(obj);

            if (timeSource != null)
            {
                obj.AnimationComponent?.SetTimeSource(timeSource);
            }

            if (obj.NetID != 0)
            {
                netIDLookup.Add(obj.NetID, obj);
            }

            if(obj.SystemObject != null)
            {
                atmosphereVersion++;
            }

            SpatialLookup.AddObject(obj, obj.WorldTransform.Position);
        }

        public void RemoveObject(GameObject obj)
        {
            if (obj.NetID != 0)
            {
                netIDLookup.Remove(obj.NetID);
            }

            if(obj.SystemObject != null)
            {
                atmosphereVersion++;
            }

            objects.Remove(obj);
            SpatialLookup.RemoveObject(obj);
        }

        // TODO: Update calls to use TryGet when can be null, remove nullability from below function
        public GameObject? GetNetObject(int id)
        {
            netIDLookup.TryGetValue(id, out var go);
            return go;
        }

        public GameObject? GetObject(ObjNetId id)
        {
            netIDLookup.TryGetValue(id.Value, out var go);
            return go;
        }

        public GameObject? GetObject(uint crc)
        {
            return crc == 0 ? null : objects.FirstOrDefault(obj => obj.NicknameCRC == crc);
        }

        public GameObject? GetObject(string? nickname)
        {
            return nickname == null
                ? null
                : objects.FirstOrDefault(obj => nickname.Equals(obj.Nickname, StringComparison.OrdinalIgnoreCase));
        }

        public float ZoneDamageAt(Vector3 position)
        {
            float damage = 0;
            Zones?.ZonesAtPosition(position, z => damage += z.Damage);
            return damage;
        }

        public bool InAtmosphere(Vector3 position)
        {
            if (atmosphereSetVersion != atmosphereVersion)
            {
                atmospheres = objects.Where(x => x.SystemObject != null && x.SystemObject.AtmosphereRange > 0)
                    .Select(x => new BoundingSphere(x.LocalTransform.Position, x.SystemObject!.AtmosphereRange))
                    .ToArray();
                atmosphereSetVersion = atmosphereVersion;
            }
            for (int i = 0; i < atmospheres.Length; i++)
            {
                if (atmospheres[i].Contains(position) != ContainmentType.Disjoint)
                    return true;
            }
            return false;
        }

        public void RegisterAll()
        {
            foreach (var obj in objects)
                obj.Register(this);
        }

        public void Update(double t)
        {
            Projectiles?.Update(t);
            for (int i = 0; i < objects.Count; i++)
            {
                objects[i].PhysicsComponent?.SetOldTransform();
                objects[i].Update(t, this);
            }

            Physics?.StepSimulation((float)t);

            for (int i = 0; i < objects.Count; i++)
            {
                objects[i].PhysicsComponent?.Update(t, this);
                SpatialLookup.UpdatePosition(objects[i], objects[i].WorldTransform.Position);
            }
        }


        public void UpdateInterpolation(float fraction)
        {
            foreach (var obj in objects)
            {
                obj.PhysicsComponent?.UpdateInterpolation(fraction);
            }
        }

        public void RenderUpdate(double t)
        {
#if DEBUG
            Renderer?.UseDebugPoints(DebugPoints);
#endif
            Renderer?.UseDebugLines(DebugLines);
            Renderer?.Update(t);

            foreach (var obj in objects)
                obj.RenderUpdate(t);
        }


        public GameObject? GetSelection(ICamera camera, GameObject self, float x, float y, float vpWidth,
            float vpHeight)
        {
            var cameraProjection = camera.Projection;
            var cameraView = camera.View;

            var vp = new Vector2(vpWidth, vpHeight);
            var start = Vector3Ex.UnProject(new Vector3(x, y, 0f), cameraProjection, cameraView, vp);
            var end = Vector3Ex.UnProject(new Vector3(x, y, 1f), cameraProjection, cameraView, vp);
            var direction = (end - start).Normalized();

            GameObject? selected = null;
            var selectedDistance = SelectionMaxDistance;

            if (Physics != null && Physics.PointRaycast(self.PhysicsComponent?.Body, start, direction,
                    SelectionMaxDistance, false, out var contactPoint, out var body, out _, IsSelectablePhysicsObject) &&
                body?.Tag is GameObject hit)
            {
                selected = hit;
                selectedDistance = Vector3.Dot(contactPoint - start, direction);
            }

            if (TryGetTradelaneSelection(start, direction, SelectionMaxDistance, out var tradelane,
                    out var tradelaneDistance) && tradelaneDistance < selectedDistance)
            {
                selected = tradelane;
            }

            return selected;
        }

        private static bool IsSelectablePhysicsObject(PhysicsObject? body) =>
            body?.Tag is GameObject go &&
            go.Kind is not (GameObjectKind.Debris or GameObjectKind.DynamicAsteroid);

        private bool TryGetTradelaneSelection(Vector3 rayOrigin, Vector3 direction, float maxDistance,
            [MaybeNullWhen(false)] out GameObject selected, out float selectedDistance)
        {
            selected = null;
            selectedDistance = maxDistance;

            foreach (var obj in objects)
            {
                if (obj.SystemObject?.Dock is not { Kind: DockKinds.Tradelane } dock ||
                    !obj.TryGetComponent<DockInfoComponent>(out var dockInfo))
                {
                    continue;
                }

                var radius = GetTradelaneSelectionRadius(dockInfo);

                if (radius <= 0 || !TryGetTradelaneOrientation(obj, dock, out var orientation))
                {
                    continue;
                }

                var extents = GetTradelaneSelectionExtents(obj, orientation, radius);
                if (!RayIntersectsOrientedBox(rayOrigin, direction, maxDistance, obj.WorldTransform.Position,
                        orientation, extents, out var distance) || distance >= selectedDistance)
                {
                    continue;
                }

                selected = obj;
                selectedDistance = distance;
            }

            return selected != null;
        }

        private static int GetTradelaneSelectionRadius(DockInfoComponent dockInfo)
        {
            var radius = 0;
            foreach (var sphere in dockInfo.Spheres)
            {
                if ((sphere.Hardpoint.Equals("HpLeftLane", StringComparison.OrdinalIgnoreCase) ||
                     sphere.Hardpoint.Equals("HpRightLane", StringComparison.OrdinalIgnoreCase)) &&
                    sphere.Radius > radius)
                {
                    radius = sphere.Radius;
                }
            }

            return radius;
        }

        private bool TryGetTradelaneOrientation(GameObject obj, DockAction dock,
            out Quaternion orientation)
        {
            var laneDirection = Vector3.Transform(-Vector3.UnitZ, obj.WorldTransform.Orientation);
            var target = GetObject(dock.Target) ?? GetObject(dock.TargetLeft);
            if (target != null)
            {
                var targetDirection = target.WorldTransform.Position - obj.WorldTransform.Position;
                if (targetDirection.LengthSquared() > 0.0001f)
                {
                    laneDirection = Vector3.Normalize(targetDirection);
                }
            }

            if (laneDirection.LengthSquared() <= 0.0001f)
            {
                orientation = Quaternion.Identity;
                return false;
            }

            var up = Vector3.Transform(Vector3.UnitY, obj.WorldTransform.Orientation);
            if (MathF.Abs(Vector3.Dot(laneDirection, up)) > 0.99f)
            {
                up = MathF.Abs(laneDirection.Y) < 0.99f ? Vector3.UnitY : Vector3.UnitX;
            }


            orientation = QuaternionEx.LookRotation(-laneDirection, up);
            return true;
        }

        private static Vector3 GetTradelaneSelectionExtents(GameObject obj, Quaternion orientation, float radius)
        {
            var body = obj.PhysicsComponent?.Body;
            if (body == null)
            {
                return new Vector3(radius, radius * 0.25f, radius);
            }

            var bounds = body.GetBoundingBox();
            var inverse = Quaternion.Conjugate(orientation);
            var localMin = new Vector3(float.MaxValue);
            var localMax = new Vector3(float.MinValue);
            var center = obj.WorldTransform.Position;
            Span<Vector3> corners = stackalloc Vector3[BoundingBox.CornerCount];
            bounds.GetCorners(corners);

            foreach (var corner in corners)
            {
                var local = Vector3.Transform(corner - center, inverse);
                localMin = Vector3.Min(localMin, local);
                localMax = Vector3.Max(localMax, local);
            }

            var halfWidth = MathF.Max(radius, MathF.Max(MathF.Abs(localMin.X), MathF.Abs(localMax.X)));
            var boundHeight = localMax.Y - localMin.Y;
            var halfHeight = boundHeight > 0 ? boundHeight * (TradelaneSelectionHeight / 2) :
                radius * (TradelaneSelectionHeight / 2);
            return new Vector3(halfWidth, halfHeight, radius);
        }

        private static bool RayIntersectsOrientedBox(Vector3 rayOrigin, Vector3 direction, float maxDistance,
            Vector3 center, Quaternion orientation, Vector3 halfExtents, out float distance)
        {
            var inverse = Quaternion.Conjugate(orientation);
            var localOrigin = Vector3.Transform(rayOrigin - center, inverse);
            var localDirection = Vector3.Transform(direction, inverse);
            var enter = 0f;
            var exit = maxDistance;

            if (!UpdateRayBoxInterval(localOrigin.X, localDirection.X, halfExtents.X, ref enter, ref exit) ||
                !UpdateRayBoxInterval(localOrigin.Y, localDirection.Y, halfExtents.Y, ref enter, ref exit) ||
                !UpdateRayBoxInterval(localOrigin.Z, localDirection.Z, halfExtents.Z, ref enter, ref exit))
            {
                distance = 0;
                return false;
            }

            distance = enter;
            return true;
        }

        private static bool UpdateRayBoxInterval(float origin, float direction, float extent,
            ref float enter, ref float exit)
        {
            if (MathF.Abs(direction) < 0.000001f)
            {
                return origin >= -extent && origin <= extent;
            }

            var first = (-extent - origin) / direction;
            var second = (extent - origin) / direction;
            if (first > second)
            {
                (first, second) = (second, first);
            }

            enter = MathF.Max(enter, first);
            exit = MathF.Min(exit, second);
            return enter <= exit;
        }

        public void Dispose()
        {
            Physics?.Dispose();
            Zones?.Dispose();
        }
    }
}
