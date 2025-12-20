// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Collections.Generic;
using System.IO;
using System.Numerics;
using BepuUtilities.Collections;
using LibreLancer.Data.GameData.World;
using LibreLancer.Physics;
using LibreLancer.Resources;

namespace LibreLancer.World.Components
{
	//Generates light static objects the player can hit for asteroid fields
	public class AsteroidFieldComponent : GameComponent
	{
		public AsteroidField Field;
        ConvexMeshCollider shape;
        private ResourceManager res;
		public AsteroidFieldComponent(AsteroidField field, ResourceManager res, GameObject parent) : base(parent)
		{
			Field = field;
			float rdist = 0f;
			if (field.Zone.Shape == ShapeKind.Ellipsoid ||
                     field.Zone.Shape == ShapeKind.Box)
            {
                var s = field.Zone.Size;
				rdist = Math.Max(Math.Max(s.X, s.Y), s.Z);
			}
			else
			{
                //Radius
                rdist = field.Zone.Size.X;
			}
			rdist += COLLIDE_DISTANCE;
			activateDist = rdist * rdist;
            this.res = res;
        }

		float activateDist;

		AsteroidExclusionZone GetExclusionZone(Vector3 pt)
		{
			for (int i = 0; i < Field.ExclusionZones.Count; i++)
			{
				var f = Field.ExclusionZones[i];
				if (f.Zone.ContainsPoint(pt))
					return f;
			}
			return null;
		}

        PhysicsWorld phys;
        public override void Register(PhysicsWorld physics)
        {
            if (physics == null)
                return;
            phys = physics;
            shape = new ConvexMeshCollider(phys);
            var rm = GetResourceManager();
            foreach (var asteroid in Field.Cube)
            {
                var sur = asteroid.Archetype.ModelFile.LoadFile(rm, MeshLoadMode.CPU).Collision;
                if (sur.Valid)
                {
                    shape.AddPart(sur.FileId, new ConvexMeshId(0,0), new Transform3D(asteroid.Position * Field.CubeSize, asteroid.Rotation), null);
                }
            }

            spawnedA = new QuickList<SpawnedCube>(64, physics.BufferPool);
            spawnedB = new QuickList<SpawnedCube>(64, physics.BufferPool);
        }
        public override void Unregister(PhysicsWorld physics)
        {
            if(shape != null) shape.Dispose();
            if (phys != null)
            {
                phys = null;
                var oldList = useA ? ref spawnedA : ref spawnedB;
                for (int i = 0; i < oldList.Count; i++)
                    physics.RemoveUnmanagedStatic(ref oldList[i].Object);
                spawnedA.Dispose(physics.BufferPool);
                spawnedB.Dispose(physics.BufferPool);
            }
        }
		const float COLLIDE_DISTANCE = 600;

        struct SpawnedCube
        {
            public Vector3 Position;
            public UnmanagedStatic Object;
        }

        private QuickList<SpawnedCube> spawnedA;
        private QuickList<SpawnedCube> spawnedB;
        private bool useA = false;

        // Turn a Span<long> into a bitarray
        ref struct StackBitArray
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
                        items[index >> 6] |= (1L << (index & 0x3F));
                    else
                        items[index >> 6] &= ~(1L << (index & 0x3F));
                }
            }

        }

		public override void Update(double time)
        {
            if (phys == null)
                return;

			var world = Parent.GetWorld();

            int amountCubes = (int)Math.Floor((COLLIDE_DISTANCE / Field.CubeSize)) + 1;

            //Create series of bounding boxes, merging some as we go
            var fillBoxes = new QuickList<(BoundingBox Bb, Vector4i Dims)>(8, phys.BufferPool);
            foreach (var pobj in phys.DynamicObjects)
            {
                var pos = pobj.Position;
                if (Vector3.DistanceSquared(Field.Zone.Position, pos) > activateDist)
                    continue;
                var c = AsteroidFieldShared.GetCloseCube(pos, Field.CubeSize);
                var mind = c - new Vector3(amountCubes * Field.CubeSize);
                var maxd = c + new Vector3(amountCubes * Field.CubeSize);
                var objbox = new BoundingBox(mind, maxd);
                bool add = true;
                for (int i = 0; i < fillBoxes.Count; i++) {
                    if (fillBoxes[i].Bb.Intersects(objbox)) {
                        fillBoxes[i].Bb = BoundingBox.CreateMerged(fillBoxes[i].Bb, objbox);
                        add = false;
                        break;
                    }
                }
                if(add)
                    fillBoxes.Add((objbox, default), phys.BufferPool);
            }

            //Merge bounding boxes second pass
            while (true)
            {
                bool changed = false;
                for (int i = 0; i < fillBoxes.Count; i++) {
                    for (int j = 0; j < fillBoxes.Count; j++)
                    {
                        if (i != j && fillBoxes[i].Bb.Intersects(fillBoxes[j].Bb)) {
                            fillBoxes[i].Bb = BoundingBox.CreateMerged(fillBoxes[i].Bb, fillBoxes[j].Bb);
                            fillBoxes.FastRemoveAt(i);
                            changed = true;
                            break;
                        }
                    }
                    if (changed)
                        break;
                }
                if (!changed)
                    break;
            }

            //Clear out everything if we have no bounding boxes
            ref var oldList = ref useA ? ref spawnedA : ref spawnedB;
            ref var bodies = ref useA ? ref spawnedB : ref spawnedA;

            if (fillBoxes.Count == 0)
            {
                for (int i = 0; i < oldList.Count; i++) {
                    phys.RemoveUnmanagedStatic(ref oldList[i].Object);
                }
                spawnedA.Count = 0;
                spawnedB.Count = 0;
                return;
            }

            //Allocate bitmaps on stack
            int arrayLength = 0;
            for (int i = 0; i < fillBoxes.Count; i++)
            {
                var boxCubes = (fillBoxes[i].Bb.Max - fillBoxes[i].Bb.Min) / Field.CubeSize + Vector3.One;
                var strideX = (int)(boxCubes.Y * boxCubes.Z);
                var amount = ((int)(boxCubes.X * boxCubes.Y * boxCubes.Z) >> 6) + 1;
                fillBoxes[i].Dims = new Vector4i(strideX, (int) boxCubes.Z, 0, arrayLength);
                arrayLength += amount;
            }

            StackBitArray present = new StackBitArray(stackalloc long[arrayLength]);

            static int Index(Vector4i dims, int x, int y, int z) => dims.W + (x * dims.X) + (y * dims.Y) + z;

            //Swap buffers and reset current buffer
            useA = !useA;
            bodies.Count = 0;

            //Fill bitmap with currently present cubes
			for (int i = 0; i < oldList.Count; i++)
            {
                bool remove = true;
                for (int j = 0; j < fillBoxes.Count; j++)
                {
                    if (fillBoxes[j].Bb.Contains(oldList[i].Position) != ContainmentType.Disjoint)
                    {
                        remove = false;
                        bodies.Add(oldList[i], world.Physics.BufferPool);
                        var p = (oldList[i].Position - fillBoxes[j].Bb.Min) / Field.CubeSize;
                        present[Index(fillBoxes[j].Dims, (int) p.X, (int) p.Y, (int) p.Z)] = true;
                        break;
                    }
                }
                if (remove)
                    world.Physics.RemoveUnmanagedStatic(ref oldList[i].Object);
            }

            //Fill remaining cubes if needed
            foreach (var box in fillBoxes)
            {
                var boxCubes = (box.Bb.Max - box.Bb.Min) / Field.CubeSize;
                for (int x = 0; x < boxCubes.X; x++)
                {
                    for (int y = 0; y < boxCubes.Y; y++)
                    {
                        for (int z = 0; z < boxCubes.Z; z++)
                        {
                            if (present[Index(box.Dims, x, y, z)]) //Already added, skip checks
                                continue;
                            var center = box.Bb.Min + new Vector3(x, y, z) * new Vector3(Field.CubeSize);
                            if (!Field.Zone.ContainsPoint(center))
                                continue;
                            if (!AsteroidFieldShared.CubeExists(center, Field.EmptyCubeFrequency, out int tval))
                                continue;
                            if (GetExclusionZone(center) != null)
                                continue;
                            var transform = new Transform3D(center, Field.CubeRotation.GetRotation(tval));
                            bodies.Add(new SpawnedCube() { Position = center }, world.Physics.BufferPool);
                            world.Physics.CreateUnmanagedStatic(ref bodies[bodies.Count - 1].Object, transform, shape);
                        }
                    }
                }
            }

            fillBoxes.Dispose(phys.BufferPool);
		}
	}
}
