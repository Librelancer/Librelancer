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
        private class AttachEntityProcessor(float duration, ThnSceneObject child, ThnAttachment attachment) : ThnEventProcessor
        {
            public float Duration = duration;
            public ThnSceneObject Child = child;
            public ThnAttachment Attachment = attachment;

            private double t;

            public override bool Run(double delta)
            {
                t += delta;
                if (t > Duration)
                {
                    Child.Attachments.Remove(Attachment);
                    return false;
                }
                return true;
            }
        }


        public AttachEntityEvent() { }

        public TargetTypes TargetType;
        public AttachFlags Flags;
        public string? TargetPart;
        public Vector3 Offset;

        public AttachEntityEvent(ThornTable table) : base(table)
        {
            if (!GetProps(table, out var props))
            {
                return;
            }

            GetValue(props, "target_type", out TargetType);
            GetValue(props, "flags", out Flags, AttachFlags.Position | AttachFlags.Orientation);
            GetValue(props, "target_part", out TargetPart);
            GetValue(props, "offset", out Offset);
        }

        public override void Run(ThnScriptInstance instance)
        {
            if(!instance.Objects.TryGetValue(Targets[0], out var objA))
            {
                FLLog.Error("Thn", "Object doesn't exist " + Targets[0]);
                return;
            }

            if(!instance.Objects.TryGetValue(Targets[1], out var objB))
            {
                FLLog.Error("Thn", "Object doesn't exist " + Targets[1]);
                return;
            }

            // Attach GameObjects to eachother
            IRenderHardpoint? hardpoint = null;
            RigidModelPart? part = null;
            switch (TargetType)
            {
                case TargetTypes.Hardpoint when !string.IsNullOrEmpty(TargetPart):
                {
                    if (objB.Object == null)
                    {
                        FLLog.Error("Thn", "Could not get hardpoints on " + objB.Name);
                    }
                    else
                    {
                        hardpoint = GetHardpoint(objB.Object, TargetPart);
                    }

                    break;
                }

                case TargetTypes.Part when !string.IsNullOrEmpty(TargetPart):
                {
                    if (objB.Object?.Model?.RigidModel.Parts == null)
                    {
                        FLLog.Error("Thn", "Could not get parts on " + objB.Name);
                    }
                    else
                    {
                        if (!objB.Object.Model.RigidModel.Parts.TryGetPart(TargetPart, out part))
                        {
                            FLLog.Error("Thn", $"Could not find part {TargetPart} on " + objB.Name);
                        }
                    }

                    break;
                }
            }

            var tgt = new ThnObjectParent(objB, hardpoint, part);
            Quaternion lastRotate = Quaternion.Identity;
            if ((Flags & AttachFlags.Orientation) == AttachFlags.Orientation &&
                (Flags & AttachFlags.OrientationRelative) == AttachFlags.OrientationRelative)
            {
                var (_, tr) = tgt.GetTransform(false);
                lastRotate = tr;
            }
            var attachment = new ThnAttachment(tgt)
            {
                Position = ((Flags & AttachFlags.Position) == AttachFlags.Position),
                Orientation = ((Flags & AttachFlags.Orientation) == AttachFlags.Orientation),
                OrientationRelative = ((Flags & AttachFlags.OrientationRelative) == AttachFlags.OrientationRelative),
                EntityRelative = ((Flags & AttachFlags.EntityRelative) == AttachFlags.EntityRelative),
                LookAt = ((Flags & AttachFlags.LookAt) == AttachFlags.LookAt),
                LastRotate = lastRotate,
                Offset = Offset,
            };
            objA.Attachments.Add(attachment);
            instance.AddProcessor(new AttachEntityProcessor(Duration, objA, attachment));
        }
    }
}
