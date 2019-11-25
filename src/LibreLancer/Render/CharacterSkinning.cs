// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Collections.Generic;
using System.Net;
using System.Numerics;
using LibreLancer.Utf.Dfm;
using LibreLancer.Utf.Anm;
namespace LibreLancer
{
    public class CharacterSkinning
    {
        DfmFile dfm;
        Matrix4[] boneMatrices;
        private BoneInstance[] instanceArray;
        class BoneInstance
        {
            public string Name;
            public BoneInstance Parent;
            public Matrix4 InvBindPose;
            public Matrix4 ZeroState = Matrix4.Identity;
            public Quaternion Rotation = Quaternion.Identity;

            public Matrix4 LocalTransform()
            {
                var mine = Matrix4.CreateFromQuaternion(Rotation) * ZeroState;
                if (Parent != null)
                    mine *= Parent.LocalTransform();
                return mine;
            }

            public Matrix4 BoneMatrix()
            {
                return InvBindPose * LocalTransform();
            }
        }
        Dictionary<string, BoneInstance> boneInstances = new Dictionary<string, BoneInstance>();
        
        public CharacterSkinning(DfmFile dfm)
        {
            this.dfm = dfm;
            boneMatrices = new Matrix4[dfm.Bones.Count];
            instanceArray = new BoneInstance[dfm.Bones.Count];
            int i = 0;
            var boneFiles = new Dictionary<string, BoneInstance>();
            foreach (var bone in dfm.Bones)
            {
                var inst = new BoneInstance();
                inst.Name = bone.Key;
                inst.InvBindPose = bone.Value.BoneToRoot.Inverted();
                boneMatrices[i] = Matrix4.Identity;
                instanceArray[i++] = inst;
                boneFiles.Add(bone.Key, inst);
            }

            foreach (var con in dfm.Parts.Values)
            {
                var objName = con.objectName;
                boneInstances.Add(objName, boneFiles[con.Bone.Name]);
            }

            foreach (var con in dfm.Constructs.Constructs)
            {
                if (!boneInstances.ContainsKey(con.ChildName)) continue;
                var inst = boneInstances[con.ChildName];
                if (!string.IsNullOrEmpty(con.ParentName))
                {
                    var parent = boneInstances[con.ParentName];
                    inst.Parent = parent;
                }
                inst.ZeroState = con.Rotation * Matrix4.CreateTranslation(con.Origin);
            }
        }
        bool hasPose = false;
        //HACK: Dfms could have transparency
        private double totalTime = 0;

        void ProcessJointMap(JointMap jm)
        {
            if (!boneInstances.ContainsKey(jm.ChildName)) return;
            var joint = boneInstances[jm.ChildName];
            double t = totalTime;
            float t1 = 0;
            for (int i = 0; i < jm.Channel.Frames.Length - 1; i++)
            {
                var t0 = jm.Channel.Frames[i].Time ?? (jm.Channel.Interval * i);
                t1 = jm.Channel.Frames[i + 1].Time ?? (jm.Channel.Interval * (i + 1));
                var v1 = jm.Channel.Frames[i].QuatValue;
                var v2 = jm.Channel.Frames[i + 1].QuatValue;
                if (t >= t0 && t <= t1)
                {
                    var blend = (totalTime - t0) / (t1 - t0);
                    joint.Rotation = Quaternion.Slerp(v1, v2, (float) blend);
                }
            }
        }

        private Script curScript;
        public void Update(TimeSpan delta)
        {
            if (!hasPose) return;
            totalTime += delta.TotalSeconds;
            foreach (var jm in curScript.JointMaps)
            {
                ProcessJointMap(jm);
            }

            for (int i = 0; i < boneMatrices.Length; i++)
            {
                boneMatrices[i] = instanceArray[i].BoneMatrix();
            }
            var mesh = dfm.Levels[0];
            foreach (var fg in mesh.FaceGroups)
                fg.Material.Render.SetSkinningData(boneMatrices, ref Lighting.Empty);
        }

        public void SetPose(Script anmScript)
        {
            hasPose = true;
            curScript = anmScript;
            totalTime = 0;
        }


    }
}
