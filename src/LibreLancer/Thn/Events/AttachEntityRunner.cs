// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Numerics;
using LibreLancer.Thorn;

namespace LibreLancer
{
    [ThnEventRunner(EventTypes.AttachEntity)]
    public class AttachEntityRunner : IThnEventRunner
    {
        class AttachRoutine : IThnRoutine
        {
            public float Duration;
            public ThnObject Child;
            public Vector3 Offset;
            public ThnObject Parent;
            public GameObject Part;
            public Quaternion LastRotate;
            public bool Position;
            public bool Orientation;
            public bool OrientationRelative;
            public bool EntityRelative;
            public bool LookAt;
            Func<Vector3> lookFunc;
            double t = 0;
            public bool Run(Cutscene cs, double delta)
            {
                Vector3 translate = Parent.Translate;
                Matrix4x4 rotate = Parent.Rotate;
                if (Part != null && (Position || Orientation))
                {
                    var tr = Part.GetTransform();
                    if (Position) translate = tr.Translation;
                    if (Orientation) rotate = Matrix4x4.CreateFromQuaternion(tr.ExtractRotation());
                }
                if (Orientation && OrientationRelative)
                {
                    var qCurrent = rotate.ExtractRotation();
                    var diff = qCurrent * Quaternion.Inverse(LastRotate);
                    var qChild = Child.Rotate.ExtractRotation();
                    rotate = Matrix4x4.CreateFromQuaternion(qChild * diff);
                    LastRotate = qCurrent;
                }
                t += delta;
                if (LookAt)
                {
                    if (lookFunc == null)
                    {
                        if (Part != null) lookFunc = () => Vector3.Transform(Vector3.Zero, Part.Transform) + Offset;
                        else lookFunc = () => Parent.Translate + Offset;
                    }
                    if (Child.Camera != null) Child.Camera.LookAt = lookFunc;
                }

                if (t > Duration)
                    if (LookAt && Child.Camera != null) Child.Camera.LookAt = null;
                if(Offset != Vector3.Zero) { //TODO: This can be optimised
                    var off = Offset;
                    if (EntityRelative)
                    {
                        off = Vector3.Transform(Offset, rotate.ExtractRotation());
                    }
                    var tr = rotate * Matrix4x4.CreateTranslation(translate) * Matrix4x4.CreateTranslation(off);
                    translate = tr.Translation;
                    rotate = Matrix4x4.CreateFromQuaternion(tr.ExtractRotation());
                }
                if (Position)
                    Child.Translate = translate;
                if (Orientation)
                    Child.Rotate = rotate;
                return (t <= Duration);
            }
        }
        
        public void Process(ThnEvent ev, Cutscene cs)
        {
            ThnObject objA;
            ThnObject objB;
            if(!cs.Objects.TryGetValue((string)ev.Targets[0], out objA))
            {
                FLLog.Error("Thn", "Object doesn't exist " + (string)ev.Targets[0]);
                return;
            }
            if(!cs.Objects.TryGetValue((string)ev.Targets[1], out objB))
            {
                FLLog.Error("Thn", "Object doesn't exist " + (string)ev.Targets[1]);
                return;
            }
            var targetType = ThnEnum.Check<TargetTypes>(ev.Properties["target_type"]);
            var flags = AttachFlags.Position | AttachFlags.Orientation;
            object tmp;
            if (ev.Properties.TryGetValue("flags", out tmp))
                flags = ThnEnum.Check<AttachFlags>(tmp);
            //Attach GameObjects to eachother
            GameObject part = null;
            string tgt_part;
            ev.Properties.TryGetValue("target_part", out tmp);
            tgt_part = (tmp as string);
            if (targetType == TargetTypes.Hardpoint && !string.IsNullOrEmpty(tgt_part))
            {
                if (objB.Object == null)
                {
                    FLLog.Error("Thn", "Could not get hardpoints on " + objB.Name);
                }
                else
                {
                    part = new GameObject();
                    part.Parent = objB.Object;
                    part.Attachment = objB.Object.GetHardpoint(ev.Properties["target_part"].ToString());
                }
            }
            if (targetType == TargetTypes.Part && !string.IsNullOrEmpty(tgt_part))
            {
                if (objB.Object == null || objB.Object.RigidModel == null || objB.Object.RigidModel.Parts == null)
                {
                    FLLog.Error("Thn", "Could not get parts on " + objB.Name);
                }
                else
                {
                    if (objB.Object.RigidModel.Parts.TryGetValue((string)ev.Properties["target_part"], out var tgtpart))
                    {
                        var hp = new Hardpoint(null, tgtpart);
                        part = new GameObject();
                        part.Parent = objB.Object;
                        part.Attachment = hp;
                    }
                }
            }
            Vector3 offset = Vector3.Zero;
            if (ev.Properties.TryGetValue("offset", out tmp))
                offset = ((LuaTable) tmp).ToVector3();
            Quaternion lastRotate = Quaternion.Identity;
            if ((flags & AttachFlags.Orientation) == AttachFlags.Orientation &&
                (flags & AttachFlags.OrientationRelative) == AttachFlags.OrientationRelative)
            {
                if (part != null)
                    lastRotate = part.GetTransform().ExtractRotation();
                else
                    lastRotate = objB.Rotate.ExtractRotation();
            }
            cs.Coroutines.Add(new AttachRoutine()
            {
                Duration = ev.Duration,
                Child = objA,
                Parent = objB,
                Part = part,
                Position = ((flags & AttachFlags.Position) == AttachFlags.Position),
                Orientation = ((flags & AttachFlags.Orientation) == AttachFlags.Orientation),
                OrientationRelative = ((flags & AttachFlags.OrientationRelative) == AttachFlags.OrientationRelative),
                EntityRelative = ((flags & AttachFlags.EntityRelative) == AttachFlags.EntityRelative),
                LookAt = ((flags & AttachFlags.LookAt) == AttachFlags.LookAt),
                LastRotate = lastRotate,
                Offset = offset
            });
        }
    }
}
