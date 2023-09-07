// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.IO;
using System.Collections.Generic;
using System.Numerics;
using System.Runtime.InteropServices;
using BepuPhysics;
using BepuPhysics.Collidables;
using BepuUtilities;
using BepuUtilities.Memory;

namespace LibreLancer.Physics
{
    public class ConvexMeshCollider : Collider
    {
        private List<CollisionPart> children = new List<CollisionPart>();
        private PhysicsWorld world;
        private Buffer<CompoundChild> childBuffer;

        //TODO: Fix
        public override float Radius => 10;


        private List<Vector3> partOffsets = new List<Vector3>();

        //Helper functions for dealing with compound parts
        //Keeps index order valid for managing the hierarchy
        int AddCompoundPart(TypedIndex shape, Vector3 offset, Matrix4x4 transform)
        {
            ref Compound sh = ref BepuCompound();
            var index = sh.Children.Length;
            if (index >= childBuffer.Length) {
                pool.Resize(ref childBuffer, childBuffer.Length * 2, sh.Children.Length);
            }
            partOffsets.Add(offset);
            var pose = (Matrix4x4.CreateTranslation(offset) * transform).ToPose();
            childBuffer[index] = new CompoundChild() {
                LocalOrientation = pose.Orientation,
                LocalPosition = pose.Position,
                ShapeIndex = shape
            };
            sh.Children = childBuffer.Slice(index + 1);
            return index;
        }
        void RemoveCompoundPart(int index)
        {
            ref Compound sh = ref BepuCompound();
            //Copy parts
            for (int i = index + 1; i < sh.Children.Length; i++)
            {
                childBuffer[i - 1] = childBuffer[i];
            }
            //Shrink
            sh.Children = childBuffer.Slice(sh.Children.Length - 1);
            partOffsets.RemoveAt(index);
        }
        void UpdateCompoundTransform(int index, Matrix4x4 transform)
        {
            var pose = (Matrix4x4.CreateTranslation(partOffsets[index]) * transform).ToPose();
            childBuffer[index].LocalOrientation = pose.Orientation;
            childBuffer[index].LocalPosition = pose.Position;
        }

        public ConvexMeshCollider(PhysicsWorld world)
        {
            Handle = world.Simulation.Shapes.Add(new Compound());
            this.world = world;
            this.sim = world.Simulation;
            this.pool = world.Simulation.BufferPool;
            pool.Take(1, out childBuffer);
        }

        public override Symmetric3x3 CalculateInverseInertia(float mass)
        {
            return new Symmetric3x3() {XX = 1, YY = 1, ZZ = 1};
        }

        internal override void Create(Simulation sim, BufferPool pool) {}

        ref Compound BepuCompound()
        {
            return ref sim.Shapes.GetShape<Compound>(Handle.Index);
        }

        public bool Dump = false;

        public void AddPart(uint provider, uint meshId, Matrix4x4 localTransform, object tag)
        {
            var hulls = world.GetConvexShapes(provider, meshId);
            if (hulls.Length == 0) return;
            var pt = new CollisionPart() {Tag = tag, Index = childBuffer.Length, Count = hulls.Length};
            foreach (var h in hulls)
            {
                AddCompoundPart(h.Shape, h.Center, localTransform);
            }
        }

        public void UpdatePart(object tag, Matrix4x4 localTransform)
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
        }
        public void RemovePart(object tag)
        {
            for(int i = 0; i < children.Count; i++)
            {
                var part = children[i];
                if(part.Tag == tag) {
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
        }

        internal override void Draw(Matrix4x4 transform, IDebugRenderer renderer)
        {
            ref var sh = ref BepuCompound();
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
            //update bigcompound?
        }

        class CollisionPart
        {
            public object Tag;
            public int Index = 0;
            public int Count = 0;
            public Matrix4x4 CurrentTransform;
        }
    }
}
