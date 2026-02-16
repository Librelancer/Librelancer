// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Collections.Generic;
using System.Numerics;
using LibreLancer;
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

        struct BoneBlendData
        {
            public Quaternion Rotation;
            public Vector3 Translation;
            public float RotationWeight;
            public float TranslationWeight;
        }

        struct BonePose
        {
            public Quaternion Rotation;
            public Vector3 Translation;
            public BonePose(Quaternion rotation, Vector3 translation)
            {
                Rotation = rotation;
                Translation = translation;
            }
        }

        private static void AccumulateRotation(ref BoneBlendData data, Quaternion rotation, float weight)
        {
            if (weight <= 0f)
                return;

            if (data.RotationWeight <= 0f)
            {
                data.Rotation = rotation;
            }
            else
            {
                var t = weight / (data.RotationWeight + weight);
                data.Rotation = Quaternion.Slerp(data.Rotation, rotation, t);
            }
            data.RotationWeight += weight;
        }

        private static void AccumulateTranslation(ref BoneBlendData data, Vector3 translation, float weight)
        {
            if (weight <= 0f)
                return;

            if (data.TranslationWeight <= 0f)
            {
                data.Translation = translation;
            }
            else
            {              
                var t = weight / (data.TranslationWeight + weight);
                data.Translation = Vector3.Lerp(data.Translation, translation, t);                
            }
            data.TranslationWeight += weight;
        }

        //Used for object maps
        public bool ApplyRootMotion => _rootMotionInstance != null;
        public Vector3 RootTranslation => _rootMotionInstance.RootTranslation;
        public Quaternion RootRotation => _rootMotionInstance.RootRotation;

        public Quaternion RootRotationAccumulator => _rootMotionInstance.RootRotationAccumulator;
        public Vector3 LastTranslation;
        public Quaternion LastRotation;

        private ScriptInstance _rootMotionInstance;
        private float _rootMotionWeight;
        private bool _hasStarted;
        private const float DefaultPoseRotationEpsilon = 0.01f;
        private const float DefaultPoseTranslationEpsilon = 0.01f;

        class ScriptInstance
        {
            public double T;
            public float StartTime;
            public float TimeScale;
            public float Duration;
            public bool Loop;
            public float BlendInDuration;
            public float BlendOutDuration;

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

            float GetBlendWeight()
            {
                float weight = 1f;
                var t = (float)T;

                if (BlendInDuration > 0f && t < BlendInDuration)
                    weight = t / BlendInDuration;

                if (!Loop && Duration > 0f && BlendOutDuration > 0f)
                {
                    float blendOutStart = Duration - BlendOutDuration;
                    if (t > blendOutStart)
                        weight = MathF.Min(weight, (Duration - t) / BlendOutDuration);
                }

                if (weight < 0f) return 0f;
                if (weight > 1f) return 1f;
                return weight;
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

            public bool RunScript(double delta, Dictionary<BoneInstance, BoneBlendData> blendData)
            {
                T += delta;
                var ft = (float)(T * TimeScale) + StartTime;
                var blendWeight = GetBlendWeight();
                bool running = false;

                for (int i = 0; i < Joints.Count; i++)
                {
                    ref var j = ref Joints[i];
                    ref var ch = ref Script.JointMaps[j.JointMapIndex].Channel;
                    var cht = ft;
                    if (Loop && ft > ch.Duration)
                        cht = ft % ch.Duration;

                    if (blendWeight > 0f)
                    {
                        if (!blendData.TryGetValue(j.Bone, out var data))
                            data = default;

                        if (ch.HasOrientation)
                        {
                            var rot = ch.QuaternionAtTime(cht, ref j.JointMapCursor);
                            DfmSkeletonManager.AccumulateRotation(ref data, rot, blendWeight);
                        }

                        if (ch.HasPosition)
                        {
                            var tr = ch.PositionAtTime(cht, ref j.JointMapCursor);
                            DfmSkeletonManager.AccumulateTranslation(ref data, tr, blendWeight);
                        }

                        blendData[j.Bone] = data;
                    }

                    if (ft < ch.Duration || Loop)
                        running = true;
                }

                if (ft - lastFt > 0.001f)
                {
                    if (EvaluateRoot(lastFt, out var rot0, out var tr0) &&
                        EvaluateRoot(ft, out var rot1, out var tr1))
                    {
                        RootTranslation = (tr1 - tr0);
                        RootRotation = (Quaternion.Inverse(rot0)) * rot1;
                        RootRotationAccumulator = rot1;
                        if (blendWeight > Parent._rootMotionWeight)
                        {
                            Parent._rootMotionInstance = this;
                            Parent._rootMotionWeight = blendWeight;
                        }
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
            _rootMotionWeight = 0f;

            if (RunningScripts.Count == 0)
            {
                _hasStarted = false;
                UpdateBounds();
                return;
            }

            var basePose = new Dictionary<BoneInstance, BonePose>();
            var blendData = new Dictionary<BoneInstance, BoneBlendData>();
            var toRemove = new List<ScriptInstance>();

            foreach (var sc in RunningScripts)
            {
                for (int i = 0; i < sc.Joints.Count; i++)
                {
                    var bone = sc.Joints[i].Bone;
                    if (!basePose.ContainsKey(bone))
                        basePose[bone] = new BonePose(bone.Rotation, bone.Translation);
                }
            }

            foreach (var sc in RunningScripts)
            {
                if (!sc.RunScript(delta, blendData))
                    toRemove.Add(sc);
            }

            foreach (var kvp in basePose)
            {
                if (blendData.TryGetValue(kvp.Key, out var data))
                {
                    var rotWeight = MathF.Min(1f, data.RotationWeight);
                    var trWeight = MathF.Min(1f, data.TranslationWeight);

                    var newRot = rotWeight > 0f
                        ? Quaternion.Slerp(kvp.Value.Rotation, data.Rotation, rotWeight)
                        : kvp.Value.Rotation;

                    var newTr = trWeight > 0f
                        ? Vector3.Lerp(kvp.Value.Translation, data.Translation, trWeight)
                        : kvp.Value.Translation;

                    kvp.Key.Rotation = newRot;
                    kvp.Key.Translation = newTr;
                }
                else
                {
                    kvp.Key.Rotation = kvp.Value.Rotation;
                    kvp.Key.Translation = kvp.Value.Translation;
                }
            }

            BodySkinning.UpdateBones();
            HeadSkinning?.UpdateBones();
            LeftHandSkinning?.UpdateBones();
            RightHandSkinning?.UpdateBones();
            HeadConnection?.Update();
            LeftHandConnection?.Update();
            RightHandConnection?.Update();

            foreach (var sc in toRemove) RunningScripts.Remove(sc);
            UpdateBounds();
        }

        public void UploadBoneData(StorageBuffer bonesBuffer, ref int offset, ref int lastSet)
        {
            BodySkinning.SetBoneData(bonesBuffer, ref offset, ref lastSet);
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

        public void StartScript(Script anmScript, float start_time, float time_scale, float duration, bool loop = false, float blendIn = 5f, float blendOut = 5f)
        {
            if (anmScript.HasRootHeight) RootHeight = anmScript.RootHeight;
            var inst = new ScriptInstance(start_time);
            inst.Script = anmScript;
            inst.TimeScale = time_scale;
            inst.Duration = duration;
            inst.Loop = loop;
            var skipBlendIn = IsDefaultPose();
            inst.BlendInDuration = skipBlendIn ? 0f : blendIn;
            inst.BlendOutDuration = blendOut;
            inst.Parent = this;
            for (int i = 0; i < anmScript.JointMaps.Count; i++)
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
            _hasStarted = true;
        }

        private bool IsDefaultPose()
        {
            foreach (var bone in BodySkinning.Bones.Values)
                if (!IsDefaultPose(bone)) return false;

            if (HeadSkinning != null)
                foreach (var bone in HeadSkinning.Bones.Values)
                    if (!IsDefaultPose(bone)) return false;

            if (LeftHandSkinning != null)
                foreach (var bone in LeftHandSkinning.Bones.Values)
                    if (!IsDefaultPose(bone)) return false;

            if (RightHandSkinning != null)
                foreach (var bone in RightHandSkinning.Bones.Values)
                    if (!IsDefaultPose(bone)) return false;

            return true;
        }

        private static bool IsDefaultPose(BoneInstance bone)
        {
            var dot = MathF.Abs(Quaternion.Dot(bone.Rotation, Quaternion.Identity));
            if (dot < 1f - DefaultPoseRotationEpsilon) return false;
            if (bone.Translation.LengthSquared() > DefaultPoseTranslationEpsilon * DefaultPoseTranslationEpsilon) return false;
            return true;
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
