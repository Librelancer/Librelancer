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
using LibreLancer.World;

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

        public class Connection
        {
            private readonly HardpointDefinition parentHp;
            private readonly BoneInstance parentBone;
            private readonly HardpointDefinition childHp;
            private readonly BoneInstance childBone;
            private readonly HardpointDefinition connectionHp;
            private readonly BoneInstance connectionBone;
            private readonly Transform3D invBindPose;

            public Transform3D Transform { get; private set; }
            public Transform3D Bone { get; private set; }

            public Connection(DfmSkinning parent, DfmSkinning child,
                string parentHpName, string childHpName, string connectionHpName)
            {
                parent.GetHardpoint(parentHpName, out parentHp, out parentBone);
                child.GetHardpoint(childHpName, out childHp, out childBone);
                parent.GetHardpoint(connectionHpName, out connectionHp, out connectionBone);
                CalculateTransform();
                invBindPose = (connectionHp.Transform * connectionBone.LocalTransform * Transform.Inverse()).Inverse();
                CalculateBone();
            }

            private void CalculateTransform()
            {
                var invChild = (childHp.Transform * childBone.LocalTransform).Inverse();
                var parent = parentHp.Transform * parentBone.LocalTransform;
                Transform = invChild * parent;
            }

            private void CalculateBone()
            {
                Bone = invBindPose * connectionHp.Transform * connectionBone.LocalTransform * Transform.Inverse();
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
            public int JointMapCursor;
            public ResolvedJoint(BoneInstance bone, int jm)
            {
                Bone = bone;
                JointMapIndex = jm;
            }
        }

        //Used for object maps
        public bool ApplyRootMotion => _rootMotionInstance != null;
        public Vector3 RootTranslation => _rootMotionInstance.RootTranslation;
        public Quaternion RootRotation => _rootMotionInstance.RootRotation;

        public Quaternion RootRotationAccumulator => _rootMotionInstance.RootRotationAccumulator;

        private ScriptInstance _rootMotionInstance;
        class ScriptInstance
        {
            public double T;
            public float StartTime;
            public float TimeScale;
            public float Duration;
            public bool Loop;

            public RefList<ResolvedJoint> Joints = new RefList<ResolvedJoint>();
            public DfmSkeletonManager Parent;
            public Vector3 RootTranslation = Vector3.Zero;
            public Quaternion RootRotation = Quaternion.Identity;
            public Quaternion RootRotationAccumulator = Quaternion.Identity;
            private int rootCursor = 0;

            public Script Script;

            private float lastFt;

            public ScriptInstance(float startTime)
            {
                lastFt = StartTime = startTime;
            }

            bool EvaluateRoot(float ft, out Quaternion rotate, out Vector3 translate)
            {
                rotate = Quaternion.Identity;
                translate = Vector3.Zero;
                for (int i = 0; i < Script.ObjectMaps.Count; i++)
                {
                    ref var o = ref Script.ObjectMaps[i];
                    if (!o.ParentName.Equals("Root", StringComparison.OrdinalIgnoreCase)) continue;
                    var cht = ft;
                    if (Duration > 0 && Loop)
                    {
                        var trOne = Vector3.Zero;
                        Quaternion qOne = Quaternion.Identity;
                        if (o.Channel.HasPosition)
                            trOne = o.Channel.PositionAtTime(o.Channel.Duration, ref rootCursor);
                        if (o.Channel.HasOrientation)
                            qOne = o.Channel.QuaternionAtTime(o.Channel.Duration, ref rootCursor);
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

                    if (o.Channel.HasPosition) translate += o.Channel.PositionAtTime(cht, ref rootCursor);
                    if (o.Channel.HasOrientation) rotate *= o.Channel.QuaternionAtTime(cht, ref rootCursor);
                    return true;
                }
                return false;
            }

            public bool RunScript(double delta)
            {
                T += delta;
                var ft = (float) (T * TimeScale) + StartTime;
                bool running = false;
                for(int i = 0; i < Joints.Count; i++)
                {
                    ref var j = ref Joints[i];
                    ref var ch = ref Script.JointMaps[j.JointMapIndex].Channel;
                    var cht = ft;
                    if (Loop && ft > ch.Duration)
                        cht = ft % ch.Duration;
                    if (ch.HasOrientation)
                        j.Bone.Rotation = ch.QuaternionAtTime(cht, ref j.JointMapCursor);
                    if (ch.HasPosition)
                        j.Bone.Translation = ch.PositionAtTime(cht, ref j.JointMapCursor);
                    if (ft < ch.Duration || Loop)
                        running = true;
                }

                if (ft - lastFt > 0.001f)  {
                    if (EvaluateRoot(lastFt, out var rot0, out var tr0) &&
                        EvaluateRoot(ft, out var rot1, out var tr1))
                    {
                        RootTranslation = (tr1 - tr0);
                        RootRotation = (Quaternion.Inverse(rot0)) * rot1;
                        RootRotationAccumulator = rot1;
                        Parent._rootMotionInstance = this;
                    }
                    lastFt = ft;
                }

                if (Duration > 0)
                    return T < Duration;
                else
                    return running;
            }
        }

        public Dictionary<string, DfmHardpoint> Hardpoints =
            new Dictionary<string, DfmHardpoint>(StringComparer.OrdinalIgnoreCase);
        public bool GetAccessoryTransform(RigidModel model, string hpAccessory, string hpSkel, Matrix4x4 world, out Matrix4x4 result)
        {
            result = Matrix4x4.Identity;
            //Invert source hardpoint
            Hardpoint srcHardpoint = null;
            foreach (var part in model.AllParts)
            {
                foreach (var hp in part.Hardpoints)
                {
                    if (hp.Name.Equals(hpAccessory, StringComparison.OrdinalIgnoreCase))
                    {
                        srcHardpoint = hp;
                        break;
                    }
                }
            }
            if (srcHardpoint == null)
            {
                return false;
            }

            var invAccessory = srcHardpoint.Transform.Inverse();

            if (HeadSkinning != null && HeadSkinning.GetHardpoint(hpSkel, out var hpDef, out var boneDef))
            {
                result = (invAccessory * hpDef.Transform * boneDef.LocalTransform * HeadConnection.Transform).Matrix() * world;
                return true;
            }
            if (LeftHandSkinning != null && LeftHandSkinning.GetHardpoint(hpSkel, out hpDef, out boneDef))
            {
                result = (invAccessory * hpDef.Transform * boneDef.LocalTransform * LeftHandConnection.Transform).Matrix() * world;
                return true;
            }
            if (RightHandSkinning != null && RightHandSkinning.GetHardpoint(hpSkel, out hpDef, out boneDef))
            {
                result = (invAccessory * hpDef.Transform * boneDef.LocalTransform * RightHandConnection.Transform).Matrix() * world;
                return true;
            }
            if(BodySkinning != null && BodySkinning.GetHardpoint(hpSkel, out hpDef, out boneDef))
            {
                result = (invAccessory * hpDef.Transform * boneDef.LocalTransform).Matrix() * world;
                return true;
            }
            return false;
        }

        void AddHardpoints(DfmSkinning skinning, Connection connection)
        {
            foreach (var hp in skinning.GetHardpoints())
                Hardpoints[hp.def.Name] = new DfmHardpoint()
                {
                    Definition = hp.def,
                    Bone = hp.bone,
                    Connection = connection,
                };
        }
        public DfmSkeletonManager(DfmFile body, DfmFile head = null, DfmFile leftHand = null, DfmFile rightHand = null)
        {
            Body = body;
            BodySkinning = new DfmSkinning(body);
            AddHardpoints(BodySkinning, null);
            if (head != null)
            {
                Head = head;
                HeadSkinning = new DfmSkinning(head);
                HeadConnection = new(BodySkinning, HeadSkinning, "hp_head", "hp_head", "hp_neck");
                AddHardpoints(HeadSkinning, HeadConnection);
            }
            if (leftHand != null)
            {
                LeftHand = leftHand;
                LeftHandSkinning = new DfmSkinning(leftHand);
                LeftHandConnection = new(BodySkinning, LeftHandSkinning, "hp_left b", "hp_left b", "hp_left a");
                AddHardpoints(LeftHandSkinning, LeftHandConnection);
            }
            if (rightHand != null)
            {
                RightHand = rightHand;
                RightHandSkinning = new DfmSkinning(rightHand);
                RightHandConnection = new(BodySkinning, RightHandSkinning, "hp_right b", "hp_right b", "hp_right a");
                AddHardpoints(RightHandSkinning, RightHandConnection);
            }
            UpdateBounds();
        }

        public BoundingBox Bounds;


        void UpdateBounds()
        {
            Bounds = BodySkinning.BoundingBox;
            GetTransforms(Matrix4x4.Identity,
                out var headTr,
                out var lhTr,
                out var rhTr);
            if (HeadSkinning != null)
            {
                Bounds = BoundingBox.CreateMerged(Bounds, BoundingBox.TransformAABB(HeadSkinning.BoundingBox, headTr));
            }
            if (LeftHandSkinning != null)
            {
                Bounds = BoundingBox.CreateMerged(Bounds, BoundingBox.TransformAABB(LeftHandSkinning.BoundingBox, lhTr));
            }
            if (RightHandSkinning != null)
            {
                Bounds = BoundingBox.CreateMerged(Bounds, BoundingBox.TransformAABB(RightHandSkinning.BoundingBox, rhTr));
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
            UpdateBounds();
        }

        public void UploadBoneData(StorageBuffer bonesBuffer, ref int offset, ref int lastSet)
        {
            BodySkinning.SetBoneData(bonesBuffer,  ref offset, ref lastSet);
            HeadSkinning?.SetBoneData(bonesBuffer, ref offset, ref lastSet, HeadConnection.Bone);
            LeftHandSkinning?.SetBoneData(bonesBuffer, ref offset, ref lastSet, LeftHandConnection.Bone);
            RightHandSkinning?.SetBoneData(bonesBuffer, ref offset, ref lastSet, RightHandConnection.Bone);
        }

        public void GetTransforms(Matrix4x4 source, out Matrix4x4 head, out Matrix4x4 leftHand, out Matrix4x4 rightHand)
        {
            if (Head != null)
                head = HeadConnection.Transform.Matrix() * source;
            else
                head = source;
            if (LeftHand != null)
                leftHand = LeftHandConnection.Transform.Matrix() * source;
            else
                leftHand = source;
            if (RightHand != null)
                rightHand = RightHandConnection.Transform.Matrix() * source;
            else
                rightHand = source;
        }

        public void StartScript(Script anmScript, float start_time, float time_scale, float duration, bool loop = false)
        {
            if(anmScript.HasRootHeight) RootHeight = anmScript.RootHeight;
            var inst = new ScriptInstance(start_time);
            inst.Script = anmScript;
            inst.TimeScale = time_scale;
            inst.Duration = duration;
            inst.Loop = loop;
            inst.Parent = this;
            for(int i = 0; i < anmScript.JointMaps.Count; i++)
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
