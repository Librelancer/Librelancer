// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Collections.Generic;
using System.Numerics;
using LibreLancer.Graphics;
using LibreLancer.Utf.Anm;
using LibreLancer.Utf.Cmp;
using LibreLancer.Utf.Dfm;

namespace LibreLancer.Render
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

        private Connection HeadConnection;
        private Connection LeftHandConnection;
        private Connection RightHandConnection;

        class Connection
        {
            private readonly HardpointDefinition parentHp;
            private readonly BoneInstance parentBone;
            private readonly HardpointDefinition childHp;
            private readonly BoneInstance childBone;
            private readonly HardpointDefinition connectionHp;
            private readonly BoneInstance connectionBone;
            private readonly Matrix4x4 invBindPose;

            public Matrix4x4 Transform { get; private set; }
            public Matrix4x4 Bone { get; private set; }

            public Connection(DfmSkinning parent, DfmSkinning child,
                string parentHpName, string childHpName, string connectionHpName)
            {
                parent.GetHardpoint(parentHpName, out parentHp, out parentBone);
                child.GetHardpoint(childHpName, out childHp, out childBone);
                parent.GetHardpoint(connectionHpName, out connectionHp, out connectionBone);
                CalculateTransform();
                Matrix4x4.Invert(Transform, out Matrix4x4 invConn);
                Matrix4x4.Invert(connectionHp.Transform * connectionBone.LocalTransform * invConn, out invBindPose);
                CalculateBone();
            }

            private void CalculateTransform()
            {
                var child = childHp.Transform * childBone.LocalTransform;
                Matrix4x4.Invert(child, out child);
                var parent = parentHp.Transform * parentBone.LocalTransform;
                Transform = child * parent;
            }

            private void CalculateBone()
            {
                Matrix4x4.Invert(Transform, out Matrix4x4 invConn);
                Bone = invBindPose * connectionHp.Transform * connectionBone.LocalTransform * invConn;
            }

            public void Update()
            {
                CalculateTransform();
                CalculateBone();
            }
        }

        //Scripting
        List<ScriptInstance> RunningScripts = new List<ScriptInstance>();
        struct ResolvedJoint
        {
            public BoneInstance Bone;
            public int JointMapIndex;
            public ResolvedJoint(BoneInstance bone, int jm)
            {
                Bone = bone;
                JointMapIndex = jm;
            }
        }

        //Used for object maps
        public bool ApplyRootMotion => _rootMotionInstance != null;
        public Vector3 RootTranslation => _rootMotionInstance.RootTranslation;

        public Vector3 RootTranslationOrigin
        {
            get => _rootMotionInstance.RootTranslationOrigin;
            set => _rootMotionInstance.RootTranslationOrigin = value;
        }

        public Quaternion RootRotation => _rootMotionInstance.RootRotation;

        public Quaternion RootRotationOrigin
        {
            get => _rootMotionInstance.RootRotationOrigin;
            set => _rootMotionInstance.RootRotationOrigin = value;
        }

        private ScriptInstance _rootMotionInstance;
        class ScriptInstance
        {
            public double T;
            public float StartTime;
            public float TimeScale;
            public float Duration;
            public bool Loop;

            public List<ResolvedJoint> Joints = new List<ResolvedJoint>();
            public DfmSkeletonManager Parent;
            public Vector3 RootTranslation;
            public Vector3 RootTranslationOrigin;
            public Quaternion RootRotation = Quaternion.Identity;
            public Quaternion RootRotationOrigin = Quaternion.Identity;

            public Script Script;
            public bool RunScript(double delta)
            {
                T += delta;
                var ft = (float) (T * TimeScale) + StartTime;
                bool running = false;
                foreach (var j in Joints)
                {
                    ref var ch = ref Script.JointMaps[j.JointMapIndex].Channel;
                    var cht = ft;
                    if (Duration > 0 && Loop) cht = ft % ch.Duration;
                    if (ch.HasOrientation)
                        j.Bone.Rotation = ch.QuaternionAtTime(cht);
                    if (ch.HasPosition)
                        j.Bone.Translation = ch.PositionAtTime(cht);
                    if (ft < ch.Duration)
                        running = true;
                }

                for (int i = 0; i < Script.ObjectMaps.Length; i++)
                {
                    ref var o = ref Script.ObjectMaps[i];
                    if (!o.ParentName.Equals("Root", StringComparison.OrdinalIgnoreCase)) continue;
                    Vector3 translate = Vector3.Zero;
                    Quaternion rotate = Quaternion.Identity;
                    var cht = ft;
                    if (Duration > 0 && Loop)
                    {
                        var trOne = Vector3.Zero;
                        Quaternion qOne = Quaternion.Identity;
                        if (o.Channel.HasPosition) trOne = o.Channel.PositionAtTime(o.Channel.Duration);
                        if (o.Channel.HasOrientation) qOne = o.Channel.QuaternionAtTime(o.Channel.Duration);
                        int hangDetect = 0;
                        while (cht > o.Channel.Duration)
                        {
                            translate += trOne;
                            rotate *= qOne;
                            cht -= o.Channel.Duration;
                            if (hangDetect++ > 10000)
                                throw new Exception($"Hang in root object map code: broke {Script.Name}");
                        }
                    }
                    if (o.Channel.HasPosition) translate += o.Channel.PositionAtTime(cht);
                    if (o.Channel.HasOrientation) rotate *= o.Channel.QuaternionAtTime(cht);
                    RootTranslation = translate;
                    RootRotation = rotate;
                    Parent._rootMotionInstance = this;
                }
                if (Duration > 0)
                    return T < Duration;
                else
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
                HeadConnection = new(BodySkinning, HeadSkinning, "hp_head", "hp_head", "hp_neck");
            }
            if (leftHand != null)
            {
                LeftHand = leftHand;
                LeftHandSkinning = new DfmSkinning(leftHand);
                LeftHandConnection = new(BodySkinning, LeftHandSkinning, "hp_left b", "hp_left b", "hp_left a");
            }
            if (rightHand != null)
            {
                RightHand = rightHand;
                RightHandSkinning = new DfmSkinning(rightHand);
                RightHandConnection = new(BodySkinning, RightHandSkinning, "hp_right b", "hp_right b", "hp_right a");
            }
        }

        public void UpdateScripts(double delta)
        {
            _rootMotionInstance = null;
            List<ScriptInstance> toRemove = new List<ScriptInstance>();
            foreach (var sc in RunningScripts)
            {
                if(!sc.RunScript(delta)) toRemove.Add(sc);
            }
            if (RunningScripts.Count > 0)
            {
                BodySkinning.UpdateBones();
                HeadSkinning?.UpdateBones();
                LeftHandSkinning?.UpdateBones();
                RightHandSkinning?.UpdateBones();
                HeadConnection?.Update();
                LeftHandConnection?.Update();
                RightHandConnection?.Update();
            }
            foreach(var sc in toRemove) RunningScripts.Remove(sc);
        }

        public void UploadBoneData(UniformBuffer bonesBuffer, ref int offset, ref int lastSet)
        {
            BodySkinning.SetBoneData(bonesBuffer,  ref offset, ref lastSet);
            HeadSkinning?.SetBoneData(bonesBuffer, ref offset, ref lastSet, HeadConnection.Bone);
            LeftHandSkinning?.SetBoneData(bonesBuffer, ref offset, ref lastSet, LeftHandConnection.Bone);
            RightHandSkinning?.SetBoneData(bonesBuffer, ref offset, ref lastSet, RightHandConnection.Bone);
        }

        public void GetTransforms(Matrix4x4 source, out Matrix4x4 head, out Matrix4x4 leftHand, out Matrix4x4 rightHand)
        {
            if (Head != null)
                head = HeadConnection.Transform * source;
            else
                head = source;
            if (LeftHand != null)
                leftHand = LeftHandConnection.Transform * source;
            else
                leftHand = source;
            if (RightHand != null)
                rightHand = RightHandConnection.Transform * source;
            else
                rightHand = source;
        }

        public void StartScript(Script anmScript, float start_time, float time_scale, float duration, bool loop = false)
        {
            if(anmScript.HasRootHeight) RootHeight = anmScript.RootHeight;
            var inst = new ScriptInstance();
            inst.Script = anmScript;
            inst.StartTime = start_time;
            inst.TimeScale = time_scale;
            inst.Duration = duration;
            inst.Loop = loop;
            inst.Parent = this;
            for(int i = 0; i < anmScript.JointMaps.Length; i++)
            {
                ref var jm = ref anmScript.JointMaps[i];
                if (BodySkinning.Bones.TryGetValue(jm.ChildName, out BoneInstance bb))
                    inst.Joints.Add(new ResolvedJoint(bb, i));
                else if (Head != null && HeadSkinning.Bones.TryGetValue(jm.ChildName, out BoneInstance bh))
                    inst.Joints.Add(new ResolvedJoint(bh, i));
                else if (LeftHand != null && LeftHandSkinning.Bones.TryGetValue(jm.ChildName, out BoneInstance bl))
                    inst.Joints.Add(new ResolvedJoint(bl, i));
                else if (RightHand != null && RightHandSkinning.Bones.TryGetValue(jm.ChildName, out BoneInstance br))
                    inst.Joints.Add(new ResolvedJoint(br, i));
            }
            RunningScripts.Add(inst);
        }

        public void DebugDraw(LineRenderer lines, Matrix4x4 world, DfmDrawMode mode)
        {
            GetTransforms(world, out var h, out var lh, out var rh);
            HeadSkinning?.DebugDraw(lines, h, mode);
            BodySkinning?.DebugDraw(lines, world, mode);
            LeftHandSkinning?.DebugDraw(lines, world, mode);
            RightHandSkinning?.DebugDraw(lines, world, mode);
        }
    }
}
