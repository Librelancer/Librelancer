// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
namespace LibreLancer
{
    [ThnEventRunner(EventTypes.AttachEntity)]
    public class AttachEntityRunner : IThnEventRunner
    {
        class AttachCameraToObject : IThnRoutine
        {
            public float Duration;
            public ThnCameraTransform Camera;
            public Vector3 Offset;
            public GameObject Object;
            public GameObject Part;
            public bool Position;
            public bool Orientation;
            public bool LookAt;
            double t = 0;
            public bool Run(Cutscene cs, double delta)
            {
                Matrix4 transform;
                if (Part != null)
                    transform = Part.GetTransform();
                else
                    transform = Object.GetTransform();
                t += delta;
                if (t > Duration)
                {
                    if (LookAt)
                        Camera.LookAt = null;
                    return false;
                }
                if (Position)
                    Camera.Position = transform.ExtractTranslation();
                if (Orientation)
                    Camera.Orientation = Matrix4.CreateFromQuaternion(transform.ExtractRotation());
                return true;
            }
        }

        class DetachObject : IThnRoutine
        {
            public float Duration;
            public GameObject Object;
            double t = 0;
            public bool Run(Cutscene cs, double delta)
            {
                t += delta;
                if (t > Duration)
                    return false;

                return true;
            }
        }

        public void Process(ThnEvent ev, Cutscene cs)
        {
            object tmp;
            if (!cs.Objects.ContainsKey((string)ev.Targets[0]))
            {
                FLLog.Error("Thn", "Object doesn't exist " + (string)ev.Targets[0]);
                return;
            }
            var objA = cs.Objects[(string)ev.Targets[0]];
            var objB = cs.Objects[(string)ev.Targets[1]];
            var targetType = ThnEnum.Check<TargetTypes>(ev.Properties["target_type"]);
            var flags = AttachFlags.Position | AttachFlags.Orientation;
            Vector3 offset;

            if (ev.Properties.TryGetValue("flags", out tmp))
                flags = ThnEnum.Check<AttachFlags>(tmp);
            ev.Properties.TryGetVector3("offset", out offset);
            //Attach GameObjects to eachother
            if (objA.Object != null && objB.Object != null)
            {
                if (targetType == TargetTypes.Hardpoint)
                {
                    var targetHp = ev.Properties["target_part"].ToString();
                    if (!objB.Object.HardpointExists(targetHp))
                    {
                        FLLog.Error("Thn", "object " + objB.Name + " does not have hardpoint " + targetHp);
                        return;
                    }
                    var hp = objB.Object.GetHardpoint(targetHp);
                    objA.Object.Attachment = hp;
                    objA.Object.Parent = objB.Object;
                    objA.Object.Transform = Matrix4.CreateTranslation(offset);
                }
                else if (targetType == TargetTypes.Root)
                {
                    objA.Object.Transform = Matrix4.CreateTranslation(offset);
                    objA.Object.Parent = objB.Object;
                }

            }
            //Attach GameObjects and Cameras to eachother
            if (objA.Object != null && objB.Camera != null)
            {

            }
            if (objA.Camera != null && objB.Object != null)
            {
                if ((flags & AttachFlags.LookAt) == AttachFlags.LookAt)
                {
                    objA.Camera.LookAt = objB.Object;
                }
                GameObject part = null;
                if (targetType == TargetTypes.Hardpoint)
                {
                    part = new GameObject();
                    part.Parent = objB.Object;
                    part.Attachment = objB.Object.GetHardpoint(ev.Properties["target_part"].ToString());
                }
                if (targetType == TargetTypes.Part)
                {
                    var hp = new Hardpoint(null, part.CmpConstructs.Find(ev.Properties["target_part"].ToString())); //Create a dummy hardpoint to attach to
                    part = new GameObject();
                    part.Parent = objB.Object;
                    part.Attachment = hp;
                }
                cs.Coroutines.Add(new AttachCameraToObject()
                {
                    Duration = ev.Duration,
                    Camera = objA.Camera,
                    Object = objB.Object,
                    Part = part,
                    Position = ((flags & AttachFlags.Position) == AttachFlags.Position),
                    Orientation = ((flags & AttachFlags.Orientation) == AttachFlags.Orientation),
                    LookAt = ((flags & AttachFlags.LookAt) == AttachFlags.LookAt)
                });
            }
        }
    }
}
