// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
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
            public bool Position;
            public bool Orientation;
            public bool LookAt;
            Func<Vector3> lookFunc;
            double t = 0;
            public bool Run(Cutscene cs, double delta)
            {
                Vector3 translate = Parent.Translate;
                Matrix4 rotate = Parent.Rotate;
                if (Part != null && (Position || Orientation))
                {
                    var tr = Part.GetTransform();
                    if (Position) translate = tr.ExtractTranslation();
                    if (Orientation) rotate = Matrix4.CreateFromQuaternion(tr.ExtractRotation());
                }
                t += delta;
                if (LookAt)
                {
                    if (lookFunc == null)
                    {
                        if (Part != null) lookFunc = () => Part.Transform.Transform(Vector3.Zero);
                        else lookFunc = () => Parent.Translate;
                    }
                    if (Child.Camera != null) Child.Camera.LookAt = lookFunc;
                }
                else
                if (Child.Camera != null) Child.Camera.LookAt = null;
                
                if (t > Duration)
                    if (LookAt) Child.Camera.LookAt = null;
                if(Offset != Vector3.Zero) { //TODO: This can be optimised
                    var tr = rotate * Matrix4.CreateTranslation(translate) * Matrix4.CreateTranslation(Offset);
                    translate = tr.ExtractTranslation();
                    rotate = Matrix4.CreateFromQuaternion(tr.ExtractRotation());
                }
                if (Position)
                    Child.Translate = translate;
                if (Orientation)
                    Child.Rotate = rotate;
                return true;
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
            Vector3 offset;
            object tmp;
            if (ev.Properties.TryGetValue("flags", out tmp))
                flags = ThnEnum.Check<AttachFlags>(tmp);
            ev.Properties.TryGetVector3("offset", out offset);
            //Attach GameObjects to eachother
            GameObject part = null;
            string tgt_part;
            ev.Properties.TryGetValue("target_part", out tmp);
            tgt_part = (tmp as string);
            if (targetType == TargetTypes.Hardpoint && !string.IsNullOrEmpty(tgt_part))
            {
                part = new GameObject();
                part.Parent = objB.Object;
                part.Attachment = objB.Object.GetHardpoint(ev.Properties["target_part"].ToString());
            }
            if (targetType == TargetTypes.Part && !string.IsNullOrEmpty(tgt_part))
            {
                var hp = new Hardpoint(null, objB.Object.CmpConstructs.Find(ev.Properties["target_part"].ToString())); //Create a dummy hardpoint to attach to
                part = new GameObject();
                part.Parent = objB.Object;
                part.Attachment = hp;
            }
            cs.Coroutines.Add(new AttachRoutine()
            {
                Duration = ev.Duration,
                Child = objA,
                Parent = objB,
                Part = part,
                Position = ((flags & AttachFlags.Position) == AttachFlags.Position),
                Orientation = ((flags & AttachFlags.Orientation) == AttachFlags.Orientation),
                LookAt = ((flags & AttachFlags.LookAt) == AttachFlags.LookAt)
            });
        }
    }
}
