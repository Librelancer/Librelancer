// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using LibreLancer.Graphics;
using LibreLancer.Utf.Cmp;
using LibreLancer.Utf.Dfm;

namespace LibreLancer.Render
{
    public class DfmSkinning
    {
        DfmFile dfm;
        private BoneInstance[] instanceArray;
        List<BoneInstance> starts = new List<BoneInstance>();
        public Dictionary<string, BoneInstance> Bones = new Dictionary<string, BoneInstance>(StringComparer.OrdinalIgnoreCase);
        public int BufferOffset;
        public BoundingBox BoundingBox;

        public DfmSkinning(DfmFile dfm)
        {
            this.dfm = dfm;
            int length = (dfm.Parts.Keys.Max() + 1);
            instanceArray = new BoneInstance[length];

            foreach (var kv in dfm.Parts)
            {
                var inst = new BoneInstance(kv.Value.objectName, Transform3D.FromMatrix(kv.Value.Bone.BoneToRoot), kv.Value.Bone.Min, kv.Value.Bone.Max);
                instanceArray[kv.Key] = inst;
                Bones.Add(inst.Name, inst);
            }
            UpdateBounds();

            foreach (var con in dfm.Constructs.Constructs)
            {
                if (!Bones.ContainsKey(con.ChildName)) continue;
                var inst = Bones[con.ChildName];
                if (!string.IsNullOrEmpty(con.ParentName))
                {
                    var parent = Bones[con.ParentName];
                    parent.Children.Add(inst);
                    inst.Parent = parent;
                }
                inst.OriginalRotation = Quaternion.CreateFromRotationMatrix(con.Rotation);
                inst.Origin = con.Origin;
            }
            foreach (var b in Bones.Values)
            {
                if(b.Parent == null) starts.Add(b);
            }
        }

        public IEnumerable<(HardpointDefinition def, BoneInstance bone)> GetHardpoints()
        {
            foreach (var hp in dfm.GetHardpoints())
            {
                if (Bones.TryGetValue(hp.Part.objectName, out BoneInstance bi))
                {
                    yield return (hp.Hp, bi);
                }
                else
                {
                    yield return (hp.Hp, null);
                }
            }
        }
        public bool GetHardpoint(string hp, out HardpointDefinition def, out BoneInstance bone)
        {
            var hardpoint = dfm.GetHardpoints().FirstOrDefault(x => x.Hp.Name.Equals(hp, StringComparison.OrdinalIgnoreCase));
            if (hardpoint == null) {
                def = null;
                bone = null;
                return false;
            }
            if (Bones.TryGetValue(hardpoint.Part.objectName, out BoneInstance bi))
            {
                def = hardpoint.Hp;
                bone = bi;
                return true;
            }
            def = null;
            bone = null;
            return false;
        }

        void UpdateBounds()
        {
            var bounds = new BoundingBox();
            bool set = false;
            for (int i = 0; i < instanceArray.Length; i++)
            {
                if (instanceArray[i] == null)
                    continue;
                if (!set) {
                    set = true;
                    bounds = instanceArray[i].BoundingBox;
                }
                else {
                    bounds = BoundingBox.CreateMerged(bounds, instanceArray[i].BoundingBox);
                }
            }
            BoundingBox = bounds;
        }

        public void UpdateBones()
        {
            foreach (var s in starts)
                s.Update(Transform3D.Identity);
            UpdateBounds();
        }

        public void SetBoneData(UniformBuffer bonesBuffer, ref int offset, ref int lastSet, Transform3D? connectionBone = null)
        {
            var cb = connectionBone ?? Transform3D.Identity;
            for (int i = 0; i < instanceArray.Length; i++)
            {
                if (instanceArray[i] != null) bonesBuffer.Data<Matrix4x4>(i+ offset) = instanceArray[i].BoneMatrix;
                else bonesBuffer.Data<Matrix4x4>(i + offset) = cb.Matrix();
            }
            BufferOffset = offset;
            offset += instanceArray.Length;
            lastSet = offset;
            offset = bonesBuffer.GetAlignedIndex(offset);
        }

        private static readonly Vector3[] cubeVerts = new[]
        {
            //Front
            new Vector3(-1,-1,1),
            new Vector3(-1, 1, 1),
            new Vector3(1, 1, 1),
            new Vector3(1, -1, 1),
            //Back
            new Vector3(-1,-1,-1),
            new Vector3(-1, 1, -1),
            new Vector3(1, 1, -1),
            new Vector3(1, -1, -1),
            //Top
            new Vector3(0,0, 1.8f),
            //Arrow
            new Vector3(0,0, 0),
            new Vector3(0,1.8f, 0),
        };

        private static readonly int[] cubeIndices = new[]
        {
            //Front
            0,1, 1,2, 2,3, 3,0,
            //Back
            4,5, 5,6, 6,7, 7,4,
            //Join
            0,4, 1,5, 2,6, 3,7,
            //Point
            0,8, 1,8, 2,8, 3,8,
            //Arrow
            9,10
        };

        void DrawCube(LineRenderer lines, Matrix4x4 world, float scale, Color4 color)
        {
            for (int i = 0; i < cubeIndices.Length; i += 2)
            {
                var a = Vector3.Transform(cubeVerts[cubeIndices[i]] * scale, world);
                var b = Vector3.Transform(cubeVerts[cubeIndices[i + 1]] * scale, world);
                lines.DrawLine(a,b, color);
            }
        }

        public void DebugDraw(LineRenderer lines, Matrix4x4 world, DfmDrawMode mode)
        {
            const float scale = 0.015f;
            //world blue
            DrawCube(lines, world, scale, Color4.Blue);
            if(mode == DfmDrawMode.DebugBones ||
               mode == DfmDrawMode.DebugBonesHardpoints ||
               mode == DfmDrawMode.DebugMeshBones ||
               mode == DfmDrawMode.DebugMeshBonesHardpoints) {
                //bones red
                for (int i = 0; i < instanceArray.Length; i++)
                {
                    if (instanceArray[i] == null)
                        continue;
                    var tr = instanceArray[i].LocalTransform;
                    var color = instanceArray[i].Name == lines.SkeletonHighlight ? Color4.White : Color4.Red;
                    DrawCube(lines, tr.Matrix() * world, scale, color);
                }
            }

            if (mode == DfmDrawMode.DebugHardpoints ||
                mode == DfmDrawMode.DebugBonesHardpoints ||
                mode == DfmDrawMode.DebugMeshHardpoints ||
                mode == DfmDrawMode.DebugMeshBonesHardpoints)
            {
                //hardpoints green
                foreach (var hp in dfm.GetHardpoints())
                {
                    if (Bones.TryGetValue(hp.Part.objectName, out BoneInstance bi))
                    {
                        var tr = (hp.Hp.Transform * bi.LocalTransform).Matrix() * world;
                        var color = hp.Hp.Name == lines.SkeletonHighlight ? Color4.White : Color4.Green;
                        DrawCube(lines, tr, scale, color);
                    }
                }
            }
        }
    }
}
