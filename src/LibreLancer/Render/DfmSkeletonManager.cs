// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Collections.Generic;
using LibreLancer.Utf.Anm;
using LibreLancer.Utf.Cmp;
using LibreLancer.Utf.Dfm;

namespace LibreLancer
{
    public class DfmSkeletonManager
    {
        public float FloorHeight;
        public float RootHeight;
        
        public DfmFile Head;
        public DfmFile Body;
        public DfmFile LeftHand;
        public DfmFile RightHand;
        
        public DfmSkinning HeadSkinning;
        public DfmSkinning BodySkinning;
        public DfmSkinning LeftHandSkinning;
        public DfmSkinning RightHandSkinning;
        
        //Connect Head
        private HardpointDefinition HeadBodyHp;
        private BoneInstance HeadBodyBone;
        private HardpointDefinition BodyHeadHp;
        private BoneInstance BodyHeadBone;
        //Connect Left Hand
        private HardpointDefinition LeftBodyHp;
        private BoneInstance LeftBodyBone;
        private HardpointDefinition BodyLeftHp;
        private BoneInstance BodyLeftBone;
        //Connect Right Hand
        private HardpointDefinition RightBodyHp;
        private BoneInstance RightBodyBone;
        private HardpointDefinition BodyRightHp;
        private BoneInstance BodyRightBone;
        //Scripting
        List<ScriptInstance> RunningScripts = new List<ScriptInstance>();
        struct ResolvedJoint
        {
            public BoneInstance Bone;
            public JointMap JointMap;
            public ResolvedJoint(BoneInstance bone, JointMap jm)
            {
                Bone = bone;
                JointMap = jm;
            }
        }

        //Used for object maps
        public bool ApplyRootMotion = false;
        public Matrix4 RootMotion = Matrix4.Identity;
        class ScriptInstance
        {
            public double T;
            public List<ObjectMap> ObjectMaps = new List<ObjectMap>();
            public List<ResolvedJoint> Joints = new List<ResolvedJoint>();
            public DfmSkeletonManager Parent;
            public Matrix4 OriginalMatrix;
            public bool RunScript(TimeSpan delta)
            {
                T += delta.TotalSeconds;
                var ft = (float) T;
                bool running = false;
                foreach (var j in Joints)
                {
                    var ch = j.JointMap.Channel;
                    if (ch.HasOrientation)
                        j.Bone.Rotation = ch.QuaternionAtTime(ft);
                    if (ch.HasPosition)
                        j.Bone.Translation = ch.PositionAtTime(ft);
                    if (ft < ch.Duration)
                        running = true;
                }
                
                foreach (var o in ObjectMaps)
                {
                    if (!o.ParentName.Equals("Root", StringComparison.OrdinalIgnoreCase)) continue;
                    Vector3 translate = Vector3.Zero;
                    Quaternion rotate = Quaternion.Identity;
                    if (o.Channel.HasPosition)
                    {
                        translate = o.Channel.PositionAtTime(ft);
                    }
                    if (o.Channel.HasOrientation)
                    {
                        rotate = o.Channel.QuaternionAtTime(ft);
                    }
                    Parent.ApplyRootMotion = true;
                    Parent.RootMotion = Matrix4.CreateFromQuaternion(rotate) * Matrix4.CreateTranslation(translate) * OriginalMatrix;
                }
               
                return running;
            }
        }

        public DfmSkeletonManager(DfmFile body, DfmFile head = null, DfmFile leftHand = null, DfmFile rightHand = null)
        {
            Body = body;
            BodySkinning = new DfmSkinning(body);
            if (head != null)
            {
                Head = head;
                HeadSkinning = new DfmSkinning(head);
            }
            if (leftHand != null)
            {
                LeftHand = leftHand;
                LeftHandSkinning = new DfmSkinning(leftHand);
            }
            if (rightHand != null)
            {
                RightHand = rightHand;
                RightHandSkinning = new DfmSkinning(rightHand);
            }
            ConnectBones();
        }

        private Matrix4 original;
        public void SetOriginalTransform(Matrix4 mat)
        {
            original = mat;
        }
        
        void ConnectBones()
        {
            if (Head != null)
            {
                HeadSkinning.GetHardpoint("hp_head", out HeadBodyHp, out HeadBodyBone);
                BodySkinning.GetHardpoint("hp_head", out BodyHeadHp, out BodyHeadBone);
            }
            if (LeftHand != null)
            {
                LeftHandSkinning.GetHardpoint("hp_left b", out LeftBodyHp, out LeftBodyBone);
                BodySkinning.GetHardpoint("hp_left b", out BodyLeftHp, out BodyLeftBone);
            }
            if (RightHand != null)
            {
                RightHandSkinning.GetHardpoint("hp_right b", out RightBodyHp, out RightBodyBone);
                BodySkinning.GetHardpoint("hp_right b", out BodyRightHp, out BodyRightBone);
            }
        }
        //A - attaching object, B - attaching to
        Matrix4 GetAttachmentTransform(HardpointDefinition hpA, BoneInstance boneA, HardpointDefinition hpB, BoneInstance boneB)
        {
            var child = hpA.Transform * boneA.LocalTransform();
            child.Invert();
            var parent = hpB.Transform * boneB.LocalTransform();
            return child * parent;
        }
        
        public void UpdateScripts(TimeSpan delta)
        {
            ApplyRootMotion = false;
            List<ScriptInstance> toRemove = new List<ScriptInstance>();
            foreach (var sc in RunningScripts)
            {
                if(!sc.RunScript(delta)) toRemove.Add(sc);
            }
            foreach(var sc in toRemove) RunningScripts.Remove(sc);
        }
        
        public void UploadBoneData(UniformBuffer bonesBuffer)
        {
            int off = 0;
            BodySkinning.SetBoneData(bonesBuffer, ref off);
            if (Head != null)
            {
                HeadSkinning.SetBoneData(bonesBuffer, ref off);
            }
            if (LeftHand != null)
            {
                LeftHandSkinning.SetBoneData(bonesBuffer, ref off);   
            }
            if (RightHand != null)
            {
                RightHandSkinning.SetBoneData(bonesBuffer, ref off);
            }
        }

        public void GetTransforms(Matrix4 source, out Matrix4 head, out Matrix4 leftHand, out Matrix4 rightHand)
        {
            if (Head != null)
                head = GetAttachmentTransform(HeadBodyHp, HeadBodyBone, BodyHeadHp, BodyHeadBone) * source;
            else
                head = source;
            if (LeftHand != null)
                leftHand = GetAttachmentTransform(LeftBodyHp, LeftBodyBone, BodyLeftHp, BodyLeftBone) * source;
            else
                leftHand = source;
            if (RightHand != null)
                rightHand = GetAttachmentTransform(RightBodyHp, RightBodyBone, BodyRightHp, BodyRightBone) * source;
            else
                rightHand = source;
        }
        
        public void StartScript(Script anmScript)
        {
            if(anmScript.HasRootHeight) RootHeight = anmScript.RootHeight;
            var inst = new ScriptInstance();
            inst.ObjectMaps = anmScript.ObjectMaps;
            inst.OriginalMatrix = original;
            inst.Parent = this;
            foreach (var jm in anmScript.JointMaps)
            {
                if (BodySkinning.Bones.TryGetValue(jm.ChildName, out BoneInstance bb))
                    inst.Joints.Add(new ResolvedJoint(bb, jm));
                else if (Head != null && HeadSkinning.Bones.TryGetValue(jm.ChildName, out BoneInstance bh))
                    inst.Joints.Add(new ResolvedJoint(bh, jm));
                else if (LeftHand != null && LeftHandSkinning.Bones.TryGetValue(jm.ChildName, out BoneInstance bl))
                    inst.Joints.Add(new ResolvedJoint(bl, jm));
                else if (RightHand != null && RightHandSkinning.Bones.TryGetValue(jm.ChildName, out BoneInstance br))
                    inst.Joints.Add(new ResolvedJoint(br, jm));
            }
            RunningScripts.Add(inst);
        }
        
    }
}