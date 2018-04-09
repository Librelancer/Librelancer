/* The contents of this file are subject to the Mozilla Public License
 * Version 1.1 (the "License"); you may not use this file except in
 * compliance with the License. You may obtain a copy of the License at
 * http://www.mozilla.org/MPL/
 * 
 * Software distributed under the License is distributed on an "AS IS"
 * basis, WITHOUT WARRANTY OF ANY KIND, either express or implied. See the
 * License for the specific language governing rights and limitations
 * under the License.
 * 
 * 
 * The Initial Developer of the Original Code is Callum McGing (mailto:callum.mcging@gmail.com).
 * Portions created by the Initial Developer are Copyright (C) 2013-2018
 * the Initial Developer. All Rights Reserved.
 */
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
            foreach(var part in children) {
                if(part.Tag == tag) {
                    for (int i = part.Index; i < (part.Index + part.Count); i++) {
                        btCompound.UpdateChildTransform(i, tr);
                    }
                    break;
                }
            }
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
        }
    }

}
