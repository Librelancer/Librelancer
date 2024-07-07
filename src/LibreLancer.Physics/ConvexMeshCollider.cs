// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.IO;
using System.Collections.Generic;
using System.Diagnostics;
using System.Numerics;
using System.Runtime.InteropServices;
using BepuPhysics;
using BepuPhysics.Collidables;
using BepuUtilities;
using BepuUtilities.Collections;
using BepuUtilities.Memory;

namespace LibreLancer.Physics
{
    public class ConvexMeshCollider : Collider
    {
        private QuickList<CompoundChild> childBuilder;

        private List<CollisionPart> children = new List<CollisionPart>();
        private PhysicsWorld world;

        //TODO: Fix
        public override float Radius => 10;


        private List<Vector3> partOffsets = new List<Vector3>();
        private List<int> childIndices = new List<int>();


        private int refinementCounter = 0;
        private int lastRefinement = 0;

        public int BepuChildCount =>
            Handle.Exists ? BepuBigCompound().Children.Length : childBuilder.Count;

        //Helper functions for dealing with compound parts
        //Keeps index order valid for managing the hierarchy
        void AddCompoundPart(TypedIndex shape, Vector3 offset, Transform3D transform)
        {
            var t = new Transform3D(offset, Quaternion.Identity) * transform;
            partOffsets.Add(offset);
            if (Handle.Exists)
            {
                BepuBigCompound().Add(new CompoundChild()
                {
                    LocalOrientation = t.Orientation,
                    LocalPosition = t.Position,
                    ShapeIndex = shape,
                }, pool, sim.Shapes);
                childIndices.Add(BepuBigCompound().Children.Length - 1);
            }
            else
            {
                childBuilder.Add(new CompoundChild()
                {
                    LocalOrientation = t.Orientation,
                    LocalPosition = t.Position,
                    ShapeIndex = shape
                }, pool);
                childIndices.Add(childBuilder.Count - 1);
            }
        }
        void RemoveCompoundPart(int index)
        {
            if (Handle.Exists)
            {
                ref BigCompound sh = ref BepuBigCompound();
                var bepuIdx = childIndices[index];
                var movedChildIndex = sh.Tree.RemoveAt(bepuIdx);
                if (movedChildIndex >= 0)
                {
                    sh.Children[bepuIdx] = sh.Children[movedChildIndex];
                    var old = childIndices.IndexOf(movedChildIndex);
                    if (old != -1)
                        childIndices[old] = bepuIdx;
                }
                pool.Resize(ref sh.Children, sh.Children.Length - 1, sh.Children.Length - 1);
            }
            else
            {
                childBuilder.RemoveAt(index);
            }
            childIndices.RemoveAt(index);
            partOffsets.RemoveAt(index);
        }

        void UpdateCompoundTransform(int index, Transform3D transform)
        {
            var t = new Transform3D(partOffsets[index], Quaternion.Identity) * transform;
            if (Handle.Exists)
            {
                ref var child = ref BepuBigCompound().Children[childIndices[index]];
                child.LocalOrientation = t.Orientation;
                child.LocalPosition = t.Position;
            }
            else
            {
                childBuilder[index].LocalOrientation = t.Orientation;
                childBuilder[index].LocalPosition = t.Position;
            }
        }

        public ConvexMeshCollider(PhysicsWorld world)
        {
            this.world = world;
            this.sim = world.Simulation;
            this.pool = world.Simulation.BufferPool;
            childBuilder = new QuickList<CompoundChild>(1, this.pool);
        }

        public override Symmetric3x3 CalculateInverseInertia(float mass)
        {
            return new Symmetric3x3() {XX = 1, YY = 1, ZZ = 1};
        }

        internal override void Create(Simulation sim, BufferPool pool)
        {
            if (!Handle.Exists)
            {
                childBuilder.Compact(pool);
                var compound = new BigCompound(childBuilder.Span.Slice(0, childBuilder.Count), world.Simulation.Shapes, world.BufferPool);
                Handle = sim.Shapes.Add(compound);
                childBuilder = new QuickList<CompoundChild>();
            }
        }

        ref BigCompound BepuBigCompound()
        {
            return ref sim.Shapes.GetShape<BigCompound>(Handle.Index);
        }

        public bool Dump = false;

        public void AddPart(uint provider, uint meshId, Transform3D localTransform, object tag)
        {
            var hulls = world.GetConvexShapes(provider, meshId);
            if (hulls.Length == 0) return;
            var pt = new CollisionPart() {Tag = tag, Index = childIndices.Count, Count = hulls.Length};
            foreach (var h in hulls)
            {
                AddCompoundPart(h.Shape, h.Center, localTransform);
            }
            children.Add(pt);
            if (Handle.Exists) {
                BepuBigCompound().Tree.RefitAndRefine(pool, refinementCounter++);
                lastRefinement = refinementCounter;
            }
        }

        public void UpdatePart(object tag, Transform3D localTransform)
        {
            foreach (var part in children)
            {
                if (part.Tag == tag)
                {
                    if (part.CurrentTransform == localTransform) return;
                    part.CurrentTransform = localTransform;
                    for (int i = part.Index; i < (part.Index + part.Count); i++)
                    {
                        UpdateCompoundTransform(i, localTransform);
                    }
                    break;
                }
            }
            if (Handle.Exists)
            {
                refinementCounter++;
            }
        }

        public void RemovePart(object tag)
        {
            for(int i = 0; i < children.Count; i++)
            {
                var part = children[i];
                if(part.Tag == tag)
                {
                    for(int j = i+1; j < children.Count; j++)
                    {
                        children[j].Index -= part.Count;
                    }
                    int k = 0;
                    while(k < part.Count)
                    {
                        RemoveCompoundPart(part.Index);
                        k++;
                    }
                    children.RemoveAt(i);
                    i--;
                }
            }
            if (Handle.Exists) {
                BepuBigCompound().Tree.RefitAndRefine(pool, refinementCounter++);
                lastRefinement = refinementCounter;
            }
        }

        internal override void Draw(Matrix4x4 transform, IDebugRenderer renderer)
        {
            ref var sh = ref BepuBigCompound();
            for (int sidx = 0; sidx < sh.Children.Length; sidx++)
            {
                var childTransform = Matrix4x4.CreateFromQuaternion(sh.Children[sidx].LocalOrientation) *
                                     Matrix4x4.CreateTranslation(sh.Children[sidx].LocalPosition)
                                     * transform;
                if (sh.Children[sidx].ShapeIndex.Type == Triangle.Id)
                {
                    ref var tr = ref world.Simulation.Shapes.GetShape<Triangle>(sh.Children[sidx].ShapeIndex.Index);
                    var a = Vector3.Transform(tr.A, childTransform);
                    var b = Vector3.Transform(tr.B, childTransform);
                    var c = Vector3.Transform(tr.C, childTransform);
                    renderer.DrawLine(a, b, Color4.Red);
                    renderer.DrawLine(b, c, Color4.Red);
                    renderer.DrawLine(a, c, Color4.Red);
                }
                else if (sh.Children[sidx].ShapeIndex.Type == ConvexHull.Id)
                {
                    ref var hull = ref world.Simulation.Shapes.GetShape<ConvexHull>(sh.Children[sidx].ShapeIndex.Index);
                    for (int i = 0; i < hull.FaceToVertexIndicesStart.Length; ++i)
                    {
                        hull.GetVertexIndicesForFace(i, out var faceVertexIndices);
                        hull.GetPoint(faceVertexIndices[0], out var faceOrigin);
                        hull.GetPoint(faceVertexIndices[1], out var previousEdgeEnd);
                        for (int j = 2; j < faceVertexIndices.Length; ++j)
                        {
                            var a = Vector3.Transform(faceOrigin, childTransform);
                            var b = Vector3.Transform(previousEdgeEnd, childTransform);
                            hull.GetPoint(faceVertexIndices[j], out previousEdgeEnd);
                            var c = Vector3.Transform(previousEdgeEnd, childTransform);
                            renderer.DrawLine(a, b, Color4.White);
                            renderer.DrawLine(b, c, Color4.White);
                            renderer.DrawLine(a, c, Color4.White);
                        }
                    }
                }
            }
        }

        public void FinishUpdatePart()
        {
            if (Handle.Exists && refinementCounter != lastRefinement)
            {
                BepuBigCompound().Tree.Refit();
                lastRefinement = refinementCounter;
            }
        }

        class CollisionPart
        {
            public object Tag;
            public int Index = 0;
            public int Count = 0;
            public Transform3D CurrentTransform;
        }
    }
}
