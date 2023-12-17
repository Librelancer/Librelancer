// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using LibreLancer.Utf.Cmp;
using LibreLancer.Utf.Dfm;

namespace LibreLancer.Render
{
    public class DfmSkinning
    {
        DfmFile dfm;
        Matrix4x4[] boneMatrices;
        private BoneInstance[] instanceArray;
        List<BoneInstance> starts = new List<BoneInstance>();
        public Dictionary<string, BoneInstance> Bones = new Dictionary<string, BoneInstance>(StringComparer.OrdinalIgnoreCase);
        public int BufferOffset;
        public DfmSkinning(DfmFile dfm)
        {
            this.dfm = dfm;
            int length = (dfm.Parts.Keys.Max() + 1);
            boneMatrices = new Matrix4x4[length];
            instanceArray = new BoneInstance[length];

            foreach (var kv in dfm.Parts)
            {
                var inst = new BoneInstance();
                inst.Name = kv.Value.objectName;
                Matrix4x4.Invert(kv.Value.Bone.BoneToRoot, out inst.InvBindPose);
                inst.BoneMatrix = inst.InvBindPose;
                instanceArray[kv.Key] = inst;
                Bones.Add(inst.Name, inst);
            }

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
                inst.OriginalRotation = con.Rotation;
                inst.Origin = con.Origin;
            }
            foreach (var b in Bones.Values)
            {
                if(b.Parent == null) starts.Add(b);
            }
            for (int j = 0; j < boneMatrices.Length; j++)
                boneMatrices[j] = Matrix4x4.Identity;
        }

        public bool GetHardpoint(string hp, out HardpointDefinition def, out BoneInstance bone)
        {
            var hardpoint = dfm.GetHardpoints().First(x => x.Hp.Name.Equals(hp, StringComparison.OrdinalIgnoreCase));
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

        public void SetBoneData(UniformBuffer bonesBuffer, ref int offset)
        {
            for(int i = 0; i < starts.Count; i++)
                starts[i].Update(Matrix4x4.Identity);
            for (int i = 0; i < boneMatrices.Length; i++)
            {
                if (instanceArray[i] != null) boneMatrices[i] = instanceArray[i].BoneMatrix;
            }
            BufferOffset = offset;
            bonesBuffer.SetData(boneMatrices, offset);
            offset += boneMatrices.Length;
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
            0,8, 1,8, 2,8, 3,8
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
                    var tr = instanceArray[i].LocalTransform();
                    DrawCube(lines, tr * world, scale, Color4.Red);
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
                        var tr = (hp.Hp.Transform * bi.LocalTransform()) * world;
                        DrawCube(lines, tr, scale, Color4.Green);
                    }
                }
            }
        }
    }
}
