// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Collections.Generic;
using System.Linq;
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
            public Matrix4 OriginalRotation = Matrix4.Identity;
            public Vector3 Origin = Vector3.Zero;
            
            public Quaternion Rotation = Quaternion.Identity;
            public Vector3 Translation = Vector3.Zero;
            public Matrix4 LocalTransform()
            {
                var mine = Matrix4.CreateFromQuaternion(Rotation)  * (OriginalRotation * Matrix4.CreateTranslation(Translation + Origin));
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

        public UniformBuffer BonesBuffer;
        public CharacterSkinning(DfmFile dfm)
        {
            this.dfm = dfm;
            int length = (dfm.Parts.Keys.Max() + 1);
            boneMatrices = new Matrix4[length];
            instanceArray = new BoneInstance[length];
            
            foreach (var kv in dfm.Parts)
            {
                var inst = new BoneInstance();
                inst.Name = kv.Value.objectName;
                inst.InvBindPose = kv.Value.Bone.BoneToRoot.Inverted();
                instanceArray[kv.Key] = inst;
                boneInstances.Add(inst.Name, inst);
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

                inst.OriginalRotation = con.Rotation;
                inst.Origin = con.Origin;
            }
            for (int j = 0; j < boneMatrices.Length; j++)
                boneMatrices[j] = Matrix4.Identity;
            BonesBuffer = new UniformBuffer(200, 64, typeof(Matrix4));
            BonesBuffer.SetData(boneMatrices);
        }
        bool hasPose = false;
        //HACK: Dfms could have transparency
        private double totalTime = 0;

        void ProcessJointMap(JointMap jm)
        {
            if (!boneInstances.ContainsKey(jm.ChildName)) return;
            var joint = boneInstances[jm.ChildName];
            var t = (float) totalTime;
            if (jm.Channel.HasOrientation)
                joint.Rotation = jm.Channel.QuaternionAtTime(t);
            if (jm.Channel.HasPosition)
                joint.Translation = jm.Channel.PositionAtTime(t);
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
                if(instanceArray[i] != null) boneMatrices[i] = instanceArray[i].BoneMatrix();
            }
            BonesBuffer.SetData(boneMatrices);
        }

        public void SetPose(Script anmScript)
        {
            hasPose = true;
            curScript = anmScript;
            totalTime = 0;
        }
    }
}
