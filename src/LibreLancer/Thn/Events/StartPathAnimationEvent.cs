// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System.Numerics;
using LibreLancer.Thorn;

namespace LibreLancer.Thn.Events
{
    public class StartPathAnimationEvent : ThnEvent
    {
        public float StartPercent;
        public float StopPercent;
        public Vector3 Offset;
        public AttachFlags Flags;

        public StartPathAnimationEvent()
        {
        }

        public StartPathAnimationEvent(ThornTable table) : base(table)
        {
            if (GetProps(table, out var props))
            {
                GetValue(props, "start_percent", out StartPercent);
                GetValue(props, "stop_percent", out StopPercent, 1);
                GetValue(props, "flags", out Flags);
                GetValue(props, "offset", out Offset);
            }
        }

        public override void Run(ThnScriptInstance instance)
        {
            var obj = instance.Objects[Targets[0]];
            var path = instance.Objects[Targets[1]];

            var parent = new ThnPathParent(path);
            parent.Offset = Offset;
            parent.T = StartPercent;
            var attachment = new ThnAttachment(parent);
            if ((Flags & AttachFlags.LookAt) == AttachFlags.LookAt)
            {
                attachment.PathLookAt = true;
                attachment.Orientation = true;
            }
            else if ((Flags & AttachFlags.Orientation) == AttachFlags.Orientation)
            {
                attachment.Orientation = true;
            }
            if ((Flags & AttachFlags.Position) == AttachFlags.Position)
            {
                attachment.Position = true;
            }

            obj.Attachments.Add(attachment);
            instance.AddProcessor(new PathAnimation(obj, attachment, parent, this));
        }

        private class PathAnimation(ThnSceneObject child, ThnAttachment attachment,
            ThnPathParent parent, StartPathAnimationEvent ev) : ThnEventProcessor
        {
            public StartPathAnimationEvent Event = ev;
            public ThnSceneObject Child = child;
            public ThnAttachment Attachment = attachment;
            public ThnPathParent Parent = parent;

            private double time;

            public override bool Run(double delta)
            {
                time += delta;
                float pct = MathHelper.Lerp(Event.StartPercent, Event.StopPercent, Event.GetT((float)time));
                Parent.T = pct;
                if (time >= Event.Duration)
                {
                    Child.Attachments.Remove(Attachment);
                    return false;
                }
                return true;
            }
        }
    }
}
