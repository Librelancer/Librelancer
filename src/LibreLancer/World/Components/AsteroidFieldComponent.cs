// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using BepuUtilities.Collections;
using LibreLancer.Data.GameData;
using LibreLancer.Data.GameData.World;
using LibreLancer.Physics;
using LibreLancer.Resources;
using LibreLancer.Server.Components;

namespace LibreLancer.World.Components
{
    // Generates light static objects the player can hit for asteroid fields
    public class AsteroidFieldComponent : GameComponent
    {
        public AsteroidField Field;
        private ConvexMeshCollider? shape;
        private readonly StaticAsteroid[] mines;
        private readonly Dictionary<Vector3, float> mineRechargeTimes = new();

        public AsteroidFieldComponent(AsteroidField field, ResourceManager res, GameObject parent) : base(parent)
        {
            Field = field;
            mines = field.Cube?
                .Where(x => x.Archetype is { MineExplosion: not null, MineDetectRadius: > 0 })
                .ToArray() ?? [];

            var rdist = 0f;

            if (field.Zone!.Shape is ShapeKind.Ellipsoid or ShapeKind.Box)
            {
                var s = field.Zone!.Size;
                rdist = Math.Max(Math.Max(s.X, s.Y), s.Z);
            }
            else
            {
                // Radius
                rdist = field.Zone!.Size.X;
            }

            rdist += COLLIDE_DISTANCE;
            activateDist = rdist * rdist;
        }

        private readonly float activateDist;

        private AsteroidExclusionZone? GetExclusionZone(Vector3 pt)
        {
            return Field.ExclusionZones.FirstOrDefault(f => f.Zone?.ContainsPoint(pt) ?? false);
        }

        private PhysicsWorld? phys;

        public override void Register(GameWorld world)
        {
            if (world.Physics == null)
            {
                return;
            }

            phys = world.Physics;
            var resourceManager = GetResourceManager(world);

            if (Field.Cube is not null && resourceManager is not null)
            {
                foreach (var asteroid in Field.Cube)
                {
                    if (asteroid.Archetype?.PhantomPhysics == true)
                    {
                        continue;
                    }

                    var sur = asteroid.Archetype?.ModelFile?.LoadFile(resourceManager, MeshLoadMode.CPU)!.Collision;

                    if (sur is not null && sur.Value.Valid)
                    {
                        shape ??= new ConvexMeshCollider(phys);
                        shape.AddPart(sur.Value.FileId, new ConvexShapeId(0, 0),
                            new Transform3D(asteroid.Position * Field.CubeSize, asteroid.Rotation), null);
                    }
                }
            }

            spawnedA = new QuickList<SpawnedCube>(64, phys.BufferPool);
            spawnedB = new QuickList<SpawnedCube>(64, phys.BufferPool);
        }

        public override void Unregister(GameWorld world)
        {
            if (phys != world.Physics)
            {
                throw new InvalidOperationException();
            }

            if (phys == null)
            {
                return;
            }

            var oldList = useA ? ref spawnedA : ref spawnedB;
            for (var i = 0; i < oldList.Count; i++)
            {
                RemoveStatic(ref oldList[i]);
            }

            shape?.Dispose();
            shape = null;
            mineRechargeTimes.Clear();
            spawnedA.Dispose(phys.BufferPool);
            spawnedB.Dispose(phys.BufferPool);

            phys = null;
        }

        private const float COLLIDE_DISTANCE = 600;

        private struct SpawnedCube
        {
            public Vector3 Position;
            public Quaternion Rotation;
            public UnmanagedStatic Object;
        }

        private void RemoveStatic(ref SpawnedCube cube)
        {
            if (cube.Object.Valid)
            {
                phys!.RemoveUnmanagedStatic(ref cube.Object);
            }
        }

        private QuickList<SpawnedCube> spawnedA;
        private QuickList<SpawnedCube> spawnedB;
        private bool useA = false;

        // Turn a Span<long> into a bitarray
        private ref struct StackBitArray
        {
            private Span<long> items;

            public StackBitArray(Span<long> stack)
            {
                items = stack;
            }

            public bool this[int index]
            {
                get => (items[index >> 6] & (1L << (index & 0x3F))) != 0;
                set
                {
                    if (value)
                    {
                        items[index >> 6] |= (1L << (index & 0x3F));
                    }
                    else
                    {
                        items[index >> 6] &= ~(1L << (index & 0x3F));
                    }
                }
            }
        }


        Vector4i CalculateCubeExtents(BoundingBox boundingBox)
        {
            var boxCubes = (boundingBox.Max - boundingBox.Min) / Field.CubeSize + Vector3.One;
            return new((int)boxCubes.X, (int)boxCubes.Y, (int)boxCubes.Z, 0);
        }

        public override void Update(double time, GameWorld world)
        {
            if (phys == null)
            {
                return;
            }

            foreach (var key in mineRechargeTimes.Keys.ToArray())
            {
                var rechargeTime = mineRechargeTimes[key] - (float) time;
                if (rechargeTime <= 0)
                {
                    mineRechargeTimes.Remove(key);
                }
                else
                {
                    mineRechargeTimes[key] = rechargeTime;
                }
            }

            var amountCubes = (int) Math.Floor((COLLIDE_DISTANCE / Field.CubeSize)) + 1;

            // Create series of bounding boxes, merging some as we go
            var fillBoxes = new QuickList<(BoundingBox Bb, Vector4i Dims)>(8, phys.BufferPool);
            var mineQueries = new QuickList<SphereQuery>(8, phys.BufferPool);

            foreach (var pobj in phys.DynamicObjects)
            {
                var pos = pobj.Position;
                if (Vector3.DistanceSquared(Field.Zone!.Position, pos) > activateDist)
                {
                    continue;
                }

                var c = AsteroidFieldShared.GetCloseCube(pos, Field.CubeSize);
                var mind = c - new Vector3(amountCubes * Field.CubeSize);
                var maxd = c + new Vector3(amountCubes * Field.CubeSize);
                var objbox = new BoundingBox(mind, maxd);
                var add = true;

                for (var i = 0; i < fillBoxes.Count; i++)
                {
                    if (fillBoxes[i].Bb.Intersects(objbox))
                    {
                        fillBoxes[i].Bb = BoundingBox.CreateMerged(fillBoxes[i].Bb, objbox);
                        add = false;
                        break;
                    }
                }

                if (add)
                {
                    fillBoxes.Add((objbox, default), phys.BufferPool);
                }
            }

            // Merge bounding boxes second pass
            while (true)
            {
                var changed = false;

                for (var i = 0; i < fillBoxes.Count; i++)
                {
                    for (var j = 0; j < fillBoxes.Count; j++)
                    {
                        if (i != j && fillBoxes[i].Bb.Intersects(fillBoxes[j].Bb))
                        {
                            fillBoxes[i].Bb = BoundingBox.CreateMerged(fillBoxes[i].Bb, fillBoxes[j].Bb);
                            fillBoxes.FastRemoveAt(j); //remove other box
                            changed = true;
                            break;
                        }
                    }

                    if (changed)
                    {
                        break;
                    }
                }

                if (!changed)
                {
                    break;
                }
            }

            // Clear out everything if we have no bounding boxes
            ref var oldList = ref useA ? ref spawnedA : ref spawnedB;
            ref var bodies = ref useA ? ref spawnedB : ref spawnedA;

            if (fillBoxes.Count == 0)
            {
                for (var i = 0; i < oldList.Count; i++)
                {
                    RemoveStatic(ref oldList[i]);
                }

                spawnedA.Count = 0;
                spawnedB.Count = 0;
                fillBoxes.Dispose(phys.BufferPool);
                mineQueries.Dispose(phys.BufferPool);
                return;
            }

            // Allocate bitmaps on stack
            var arrayLength = 0;

            for (var i = 0; i < fillBoxes.Count; i++)
            {
                var boxCubes = CalculateCubeExtents(fillBoxes[i].Bb);
                var strideX = (int) (boxCubes.Y * boxCubes.Z);
                var amount = ((int) (boxCubes.X * boxCubes.Y * boxCubes.Z) >> 6) + 1;
                fillBoxes[i].Dims = new Vector4i(strideX, (int) boxCubes.Z, 0, arrayLength);
                arrayLength += amount;
            }

            StackBitArray present = new StackBitArray(stackalloc long[arrayLength]);

            static int Index(Vector4i dims, int x, int y, int z) => dims.W + (x * dims.X) + (y * dims.Y) + z;

            // Swap buffers and reset current buffer
            useA = !useA;
            bodies.Count = 0;

            // Fill bitmap with currently present cubes
            for (var i = 0; i < oldList.Count; i++)
            {
                var remove = true;

                for (var j = 0; j < fillBoxes.Count; j++)
                {
                    if (fillBoxes[j].Bb.Contains(oldList[i].Position) == ContainmentType.Disjoint)
                    {
                        continue;
                    }

                    remove = false;
                    bodies.Add(oldList[i], world?.Physics?.BufferPool);
                    AddMineQueries(ref mineQueries, oldList[i]);
                    var p = (oldList[i].Position - fillBoxes[j].Bb.Min) / Field.CubeSize;
                    present[Index(fillBoxes[j].Dims, (int) p.X, (int) p.Y, (int) p.Z)] = true;
                    break;
                }

                if (remove)
                {
                    RemoveStatic(ref oldList[i]);
                }
            }

            // Fill remaining cubes if needed
            foreach (var box in fillBoxes)
            {
                var boxCubes = CalculateCubeExtents(box.Bb);

                for (var x = 0; x < boxCubes.X; x++)
                {
                    for (var y = 0; y < boxCubes.Y; y++)
                    {
                        for (var z = 0; z < boxCubes.Z; z++)
                        {
                            if (present[Index(box.Dims, x, y, z)]) // Already added, skip checks
                            {
                                continue;
                            }

                            var center = box.Bb.Min + new Vector3(x, y, z) * new Vector3(Field.CubeSize);
                            if (!Field.Zone!.ContainsPoint(center))
                            {
                                continue;
                            }

                            if (!AsteroidFieldShared.CubeExists(center, Field.EmptyCubeFrequency, out var tval))
                            {
                                continue;
                            }

                            if (GetExclusionZone(center) != null)
                            {
                                continue;
                            }

                            var cubeRotation = Field.CubeRotation!.GetRotation(tval);
                            var transform = new Transform3D(center, cubeRotation);
                            bodies.Add(new SpawnedCube()
                            {
                                Position = center,
                                Rotation = cubeRotation
                            }, phys.BufferPool);
                            AddMineQueries(ref mineQueries, bodies[bodies.Count - 1]);
                            if (shape is { BepuChildCount: > 0 })
                            {
                                phys.CreateUnmanagedStatic(ref bodies[bodies.Count - 1].Object, transform, shape);
                            }
                        }
                    }
                }
            }

            if (mines.Length > 0)
            {
                phys.DynamicObjectSphereQuery(mineQueries);
                CheckMines(world, mineQueries);
            }
            fillBoxes.Dispose(phys.BufferPool);
            mineQueries.Dispose(phys.BufferPool);
        }

        private void AddMineQueries(ref QuickList<SphereQuery> queries, in SpawnedCube cube)
        {
            for (var mineIndex = 0; mineIndex < mines.Length; mineIndex++)
            {
                var mine = mines[mineIndex];
                var archetype = mine.Archetype!;
                var minePosition = cube.Position +
                                   Vector3.Transform(mine.Position * Field.CubeSize, cube.Rotation);
                queries.Add(new SphereQuery
                {
                    Position = minePosition,
                    Radius = archetype.MineDetectRadius
                }, phys!.BufferPool);
            }
        }

        private void CheckMines(GameWorld world, QuickList<SphereQuery> queries)
        {
            for (var i = 0; i < queries.Count; i++)
            {
                ref var query = ref queries[i];
                if (!query.Touched || mineRechargeTimes.ContainsKey(query.Position))
                {
                    continue;
                }

                var archetype = mines[i % mines.Length].Archetype!;
                var explosion = archetype.MineExplosion!;
                world.SpawnTempFx(explosion.Effect, query.Position);
                DamageMineExplosion(world, explosion, query.Position);
                mineRechargeTimes[query.Position] = archetype.MineRechargeTime;
            }
        }

        private void DamageMineExplosion(GameWorld world, Explosion explosion, Vector3 minePosition)
        {
            if (world.Server == null)
            {
                return;
            }

            foreach (var other in phys!.SphereTest(minePosition, explosion.Radius))
            {
                if (other?.Tag is GameObject g && g.TryGetComponent<SHealthComponent>(out var health))
                {
                    health.DamageExplosion(explosion.HullDamage, explosion.EnergyDamage, null, minePosition,
                        explosion.Radius);
                }
            }
        }
    }
}
