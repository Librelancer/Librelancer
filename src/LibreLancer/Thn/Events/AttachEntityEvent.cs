// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Numerics;
using LibreLancer.Thorn;

namespace LibreLancer.Thn
{
    public class AttachEntityEvent : ThnEvent
    {
        class AttachEntityProcessor : ThnEventProcessor
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
            public override bool Run(double delta)
            {
                Vector3 translate = Parent.Translate;
                Matrix4x4 rotate = Parent.Rotate;
                if (Part != null && (Position || Orientation))
                {
                    Part.SetLocalTransform(Part.LocalTransform); //force update: hacky
                    var tr = Part.WorldTransform;
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
                        //offset does not affect LOOK_AT flags
                        if (Part != null) lookFunc = () => Vector3.Transform(Vector3.Zero, Part.LocalTransform);
                        else lookFunc = () => Parent.Translate;
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
        
        public AttachEntityEvent() { }

        public TargetTypes TargetType;
        public AttachFlags Flags;
        public string TargetPart;
        public Vector3 Offset;
        
        public AttachEntityEvent(LuaTable table) : base(table)
        {
            if (GetProps(table, out var props))
            {
                GetValue(props, "target_type", out TargetType);
                GetValue(props, "flags", out Flags, AttachFlags.Position | AttachFlags.Orientation);
                GetValue(props, "target_part", out TargetPart);
                GetValue(props, "offset", out Offset);
            }
        }

        public override void Run(ThnScriptInstance instance)
        {
           ThnObject objA;
            ThnObject objB;
            if(!instance.Objects.TryGetValue(Targets[0], out objA))
            {
                FLLog.Error("Thn", "Object doesn't exist " + Targets[0]);
                return;
            }
            if(!instance.Objects.TryGetValue(Targets[1], out objB))
            {
                FLLog.Error("Thn", "Object doesn't exist " + Targets[1]);
                return;
            }
          
            //Attach GameObjects to eachother
            GameObject part = null;
            string tgt_part;
            if (TargetType == TargetTypes.Hardpoint && !string.IsNullOrEmpty(TargetPart))
            {
                if (objB.Object == null)
                {
                    FLLog.Error("Thn", "Could not get hardpoints on " + objB.Name);
                }
                else
                {
                    part = new GameObject();
                    part.Parent = objB.Object;
                    part.Attachment = objB.Object.GetHardpoint(TargetPart);
                }
            }
            if (TargetType == TargetTypes.Part && !string.IsNullOrEmpty(TargetPart))
            {
                if (objB.Object == null || objB.Object.RigidModel == null || objB.Object.RigidModel.Parts == null)
                {
                    FLLog.Error("Thn", "Could not get parts on " + objB.Name);
                }
                else
                {
                    if (objB.Object.RigidModel.Parts.TryGetValue(TargetPart, out var tgtpart))
                    {
                        var hp = new Hardpoint(null, tgtpart);
                        part = new GameObject();
                        part.Parent = objB.Object;
                        part.Attachment = hp;
                    }
                }
            }
            Quaternion lastRotate = Quaternion.Identity;
            if ((Flags & AttachFlags.Orientation) == AttachFlags.Orientation &&
                (Flags & AttachFlags.OrientationRelative) == AttachFlags.OrientationRelative)
            {
                if (part != null)
                    lastRotate = part.WorldTransform.ExtractRotation();
                else
                    lastRotate = objB.Rotate.ExtractRotation();
            }
            instance.AddProcessor(new AttachEntityProcessor()
            {
                Duration = Duration,
                Child = objA,
                Parent = objB,
                Part = part,
                Position = ((Flags & AttachFlags.Position) == AttachFlags.Position),
                Orientation = ((Flags & AttachFlags.Orientation) == AttachFlags.Orientation),
                OrientationRelative = ((Flags & AttachFlags.OrientationRelative) == AttachFlags.OrientationRelative),
                EntityRelative = ((Flags & AttachFlags.EntityRelative) == AttachFlags.EntityRelative),
                LookAt = ((Flags & AttachFlags.LookAt) == AttachFlags.LookAt),
                LastRotate = lastRotate,
                Offset = Offset
            });
        }
    }
}