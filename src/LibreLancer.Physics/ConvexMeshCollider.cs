// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.IO;
using System.Collections.Generic;
using System.Numerics;
using System.Runtime.InteropServices;
using BulletSharp;
using BM = BulletSharp.Math;

namespace LibreLancer.Physics
{
    public class ConvexMeshCollider : Collider
    {
        /* Sur Caching */
       

        internal override CollisionShape BtShape {
            get {
                return btCompound;
            }
        }

        CompoundShape btCompound;
        List<IConvexMeshProvider> surs = new List<IConvexMeshProvider>();
        public ConvexMeshCollider(IConvexMeshProvider mesh)
        {
            surs.Add(mesh);
            btCompound = new CompoundShape();
        }

        List<CollisionPart> children = new List<CollisionPart>();

        private Dictionary<(uint meshId, int surIdx), ConvexTriangleMeshShape[]> shapes =
            new Dictionary<(uint meshId, int surIdx), ConvexTriangleMeshShape[]>();

        unsafe ConvexTriangleMeshShape[] GetShape(uint meshId, int suridx)
        {
            var sur = surs[suridx];
            if (!sur.HasShape(meshId)) return null;
            if (!shapes.TryGetValue((meshId, suridx), out var hull))
            {
                var source = sur.GetMesh(meshId);
                hull = new ConvexTriangleMeshShape[source.Length];
                for (int i = 0; i < source.Length; i++)
                {
                    var vertices = new BM.Vector3[source[i].Vertices.Length];
                    fixed (Vector3* sptr = source[i].Vertices)
                    fixed(BM.Vector3* bptr = vertices)
                    {
                        Buffer.MemoryCopy(sptr, bptr, vertices.Length * sizeof(float) * 3, vertices.Length * sizeof(float) * 3);
                    }
                    hull[i] = new ConvexTriangleMeshShape(new TriangleIndexVertexArray(source[i].Indices, vertices));
                }
                shapes[(meshId, suridx)] = hull;
            }
            return hull;
        }

        int currentIndex = 0;
        public void AddPart(uint meshId, Matrix4x4 localTransform, object tag, int suridx = 0)
        {
            var hulls = GetShape(meshId, suridx);
            if (hulls == null) return;
            var pt = new CollisionPart() { Tag = tag, Index = currentIndex, Count = hulls.Length };
            var tr = localTransform.Cast();
            foreach(var h in hulls) {
                btCompound.AddChildShape(tr, h);
            }
            currentIndex += hulls.Length;
            children.Add(pt);
        }

        public int AddMeshProvider(IConvexMeshProvider mesh)
        {
            surs.Add(mesh);
            return surs.Count - 1;
        }
        public void UpdatePart(object tag, Matrix4x4 localTransform)
        {
            var tr = localTransform.Cast();
            foreach (var part in children)
            {
                if (part.Tag == tag)
                {
                    if (part.CurrentTransform == localTransform) return;
                    part.CurrentTransform = localTransform;
                    for (int i = part.Index; i < (part.Index + part.Count); i++)
                    {
                        btCompound.UpdateChildTransform(i, tr, false);
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
                        btCompound.RemoveChildShapeByIndex(part.Index);
                        k++;
                    }
                    children.RemoveAt(i);
                    i--;
                }
            }
        }
        public void FinishUpdatePart()
        {
            btCompound.RecalculateLocalAabb();
        }

        public IEnumerable<BoundingBox> GetBoxes(Matrix4x4 transform)
        {
            foreach(var shape in btCompound.ChildList) {
                BM.Vector3 min, max;
                shape.ChildShape.GetAabb(shape.Transform * transform.Cast(), out min, out max);
                yield return new BoundingBox(min.Cast(), max.Cast());
            }
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
