// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using LibreLancer.Utf.Dfm;
using LibreLancer.Utf.Anm;
namespace LibreLancer
{
    public class CharacterSkinning
    {
        DfmFile dfm;
        Matrix4[] boneMatrices;
        public CharacterSkinning(DfmFile dfm)
        {
            this.dfm = dfm;
            boneMatrices = new Matrix4[dfm.Bones.Count];
            for (int i = 0; i < boneMatrices.Length; i++)
                boneMatrices[i] = Matrix4.Identity;
        }
        bool hasPose = false;
        //HACK: Dfms could have transparency
        public void Update()
        {
            if (!hasPose) return;
            var mesh = dfm.Levels[0];
            foreach (var fg in mesh.FaceGroups)
                fg.Material.Render.SetSkinningData(boneMatrices, ref Lighting.Empty);
        }

        public void SetPose(Script anmScript)
        {
            hasPose = true;
            var rand = new Random();
            for(int i = 0; i < dfm.Parts.Count; i++)
            {
                var bone = dfm.Parts[i].Bone;
                boneMatrices[i] = bone.BoneToRoot;
            }
        }


    }
}
