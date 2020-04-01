// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

using LibreLancer.Utf.Dfm;
using LibreLancer.Utf.Cmp;

namespace LibreLancer
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
        }
    }
}
