// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Numerics;
using LibreLancer.Render;
using LibreLancer.Thorn;
using LibreLancer.World;

namespace LibreLancer.Thn.Events
{
    /*
     * NOTES:
     * Offset does not affect LOOK_AT flags (?)
     * LOOK_AT rotates directly, object and camera
     * no need for LookAtFunc
     *
     * Attached position is kept after event end, just stops updating
     */
    public class AttachEntityEvent : ThnEvent
    {
        class AttachEntityProcessor : ThnEventProcessor
        {
            public float Duration;
            public ThnObject Child;
            public Vector3 Offset;
            public EntityTarget Parent;
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
                var (translate, rotate) = Parent.GetTransform();

                if (Orientation && OrientationRelative)
                {
                    var qCurrent = rotate.ExtractRotation();
                    var diff = qCurrent * Quaternion.Inverse(LastRotate);
                    var qChild = Child.Rotate.ExtractRotation();
                    rotate = Matrix4x4.CreateFromQuaternion(qChild * diff);
                    LastRotate = qCurrent;
                }
                t += delta;

                if (Position)
                {
                    if (Offset != Vector3.Zero)
                    {
                        //TODO: This can be optimised
                        var off = Offset;
                        if (EntityRelative)
                        {
                            off = Vector3.Transform(Offset, rotate.ExtractRotation());
                        }

                        var tr = rotate * Matrix4x4.CreateTranslation(translate) * Matrix4x4.CreateTranslation(off);
                        Child.Translate = tr.Translation;
                    }
                    else
                    {
                        Child.Translate = translate;
                    }
                }
                if (Orientation)
                {
                    Child.Rotate = rotate;
                }
                if (LookAt)
                {
                    Child.Rotate = Matrix4x4.CreateFromQuaternion(QuaternionEx.LookRotation(Vector3.Normalize(Child.Translate - translate), Vector3.UnitY));
                }
                return (t <= Duration);
            }
        }

        class EntityTarget(ThnObject obj, IRenderHardpoint hardpoint, RigidModelPart part)
        {
            public (Vector3 Translate, Matrix4x4 Rotate) GetTransform()
            {
                if (part != null)
                {
                    var tr = part.LocalTransform * obj.Object.LocalTransform;
                    return (Vector3.Transform(Vector3.Zero, tr), Matrix4x4.CreateFromQuaternion(tr.ExtractRotation()));
                }
                if (hardpoint != null)
                {
                    var tr = hardpoint.Transform * obj.Object.LocalTransform;
                    return (Vector3.Transform(Vector3.Zero, tr), Matrix4x4.CreateFromQuaternion(tr.ExtractRotation()));
                }

                return (obj.Translate, obj.Rotate);
            }
        }

        public AttachEntityEvent() { }

        public TargetTypes TargetType;
        public AttachFlags Flags;
        public string TargetPart;
        public Vector3 Offset;

        public AttachEntityEvent(ThornTable table) : base(table)
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
            IRenderHardpoint hardpoint = null;
            RigidModelPart part = null;
            if (TargetType == TargetTypes.Hardpoint && !string.IsNullOrEmpty(TargetPart))
            {
                if (objB.Object == null)
                {
                    FLLog.Error("Thn", "Could not get hardpoints on " + objB.Name);
                }
                else
                {
                    hardpoint = GetHardpoint(objB.Object, TargetPart);
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
                    if (!objB.Object.RigidModel.Parts.TryGetValue(TargetPart, out part))
                    {
                        FLLog.Error("Thn", $"Could not find part {TargetPart} on " + objB.Name);
                    }
                }
            }

            var tgt = new EntityTarget(objB, hardpoint, part);
            Quaternion lastRotate = Quaternion.Identity;
            if ((Flags & AttachFlags.Orientation) == AttachFlags.Orientation &&
                (Flags & AttachFlags.OrientationRelative) == AttachFlags.OrientationRelative)
            {
                var (_, tr) = tgt.GetTransform();
                lastRotate = tr.ExtractRotation();
            }
            instance.AddProcessor(new AttachEntityProcessor()
            {
                Duration = Duration,
                Child = objA,
                Parent = tgt,
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
