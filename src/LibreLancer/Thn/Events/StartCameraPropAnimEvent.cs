// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using LibreLancer.Thorn;

namespace LibreLancer.Thn.Events
{
    public class StartCameraPropAnimEvent : ThnEvent
    {
        [Flags]
        public enum AnimFlags
        {
            Nothing = 0,
            FovH = 1 << 0,
            HVAspect = 1 << 1,
            NearPlane = 1 << 2,
            FarPlane = 1 << 3
        }
        public StartCameraPropAnimEvent() { }

        public AnimFlags SetFlags;
        public float FovH;
        public float HVAspect;
        public float NearPlane;
        public float FarPlane;

        public StartCameraPropAnimEvent(ThornTable table) : base(table)
        {
            if (GetProps(table, out var props))
            {
                if (!GetValue(props, "cameraprops", out ThornTable cameraprops))
                    return;
                if (GetValue(cameraprops, "fovh", out FovH)) SetFlags |= AnimFlags.FovH;
                if (GetValue(cameraprops, "hvaspect", out HVAspect)) SetFlags |= AnimFlags.HVAspect;
                if (GetValue(cameraprops, "nearplane", out NearPlane)) SetFlags |= AnimFlags.NearPlane;
                if (GetValue(cameraprops, "farplane", out FarPlane)) SetFlags |= AnimFlags.FarPlane;
            }
        }

        public override void Run(ThnScriptInstance instance)
        {
            if (SetFlags == AnimFlags.Nothing) return;
            if (!instance.Objects.TryGetValue(Targets[0], out var obj))
            {
                FLLog.Error("Thn", $"Entity {Targets[0]} does not exist");
                return;
            }
            if (obj.Camera == null)
            {
                FLLog.Error("Thn", $"Entity {Targets[0]} is not a camera");
                return;
            }
            if (Duration > 0)
            {
                instance.AddProcessor(new CameraPropAnim()
                {
                    Event = this,
                    Camera = obj.Camera,
                    OrigFovH = obj.Camera.FovH,
                    OrigHVAspect = obj.Camera.AspectRatio,
                    OrigNearPlane = obj.Camera.Znear,
                    OrigFarPlane = obj.Camera.Zfar
                });
            }
            else
            {
                if ((SetFlags & AnimFlags.FovH) == AnimFlags.FovH) obj.Camera.FovH = FovH;
                if ((SetFlags & AnimFlags.HVAspect) == AnimFlags.HVAspect) obj.Camera.AspectRatio = HVAspect;
                if ((SetFlags & AnimFlags.NearPlane) == AnimFlags.NearPlane) obj.Camera.Znear = NearPlane;
                if ((SetFlags & AnimFlags.FarPlane) == AnimFlags.FarPlane) obj.Camera.Zfar = FarPlane;
            }
        }

        class CameraPropAnim : ThnEventProcessor
        {
            public StartCameraPropAnimEvent Event;
            public ThnCameraProps Camera;
            public float OrigFovH;
            public float OrigHVAspect;
            public float OrigNearPlane;
            public float OrigFarPlane;

            private double time;
            public override bool Run(double delta)
            {
                time += delta;
                float t = Event.GetT((float) time);
                if ((Event.SetFlags & AnimFlags.FovH) == AnimFlags.FovH)
                    Camera.FovH = MathHelper.Lerp(OrigFovH, Event.FovH, t);
                if ((Event.SetFlags & AnimFlags.HVAspect) == AnimFlags.HVAspect)
                    Camera.AspectRatio = MathHelper.Lerp(OrigHVAspect, Event.HVAspect, t);
                if ((Event.SetFlags & AnimFlags.NearPlane) == AnimFlags.NearPlane)
                    Camera.Znear = MathHelper.Lerp(OrigNearPlane, Event.NearPlane, t);
                if ((Event.SetFlags & AnimFlags.FarPlane) == AnimFlags.FarPlane)
                    Camera.Zfar = MathHelper.Lerp(OrigFarPlane, Event.FarPlane, t);
                return time < Event.Duration;
            }
        }
    }
}