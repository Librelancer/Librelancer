// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.IO;
using System.Collections.Generic;
using BulletSharp;
using BM = BulletSharp.Math;
using LibreLancer.Physics.Sur;
namespace LibreLancer.Physics
{
    public class SurCollider : Collider
    {
        /* Sur Caching */
        static Dictionary<string, SurFile> cachedsurs = new Dictionary<string, SurFile>();
        static SurFile GetSur(string path)
        {
            var real = Path.GetFullPath(path);
            SurFile sur;
            if(!cachedsurs.TryGetValue(real, out sur)) {
                using(var stream = File.OpenRead(real)) {
                    sur = new SurFile(stream);
                }
                cachedsurs.Add(real, sur);
            }
            return sur;
        }

        internal override CollisionShape BtShape {
            get {
                return btCompound;
            }
        }

        CompoundShape btCompound;
        List<SurFile> surs = new List<SurFile>();
        public SurCollider(string path)
        {
            surs.Add(GetSur(path));
            btCompound = new CompoundShape();
        }

        List<CollisionPart> children = new List<CollisionPart>();

        int currentIndex = 0;
        public void AddPart(uint meshId, Matrix4 localTransform, object tag, int suridx = 0)
        {
            var sur = surs[suridx];
            if (!sur.HasShape(meshId)) return;
            var hulls = sur.GetShape(meshId);
            var pt = new CollisionPart() { Tag = tag, Index = currentIndex, Count = hulls.Length };
            var tr = localTransform.Cast();
            foreach(var h in hulls) {
                btCompound.AddChildShape(tr, h);
            }
            currentIndex += hulls.Length;
            children.Add(pt);
        }

        public int LoadSur(string path)
        {
            surs.Add(GetSur(path));
            return surs.Count - 1;
        }
        public void UpdatePart(object tag, Matrix4 localTransform)
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
        public void FinishUpdatePart()
        {
            btCompound.RecalculateLocalAabb();
        }

        public IEnumerable<BoundingBox> GetBoxes(Matrix4 transform)
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
            public Matrix4 CurrentTransform;
        }
    }

}
