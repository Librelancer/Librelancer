// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
namespace LibreLancer
{
    [ThnEventRunner(EventTypes.StartPathAnimation)]
    public class StartPathAnimationRunner : IThnEventRunner
    {
        static void OrthoNormalize(ref Vector3 normal, ref Vector3 tangent)
        {
            normal.Normalize();
            var proj = normal * Vector3.Dot(tangent, normal);
            tangent = tangent - proj;
            tangent.Normalize();
        }

        static Quaternion LookRotation(Vector3 direction, Vector3 up)
        {
            var forward = direction.Normalized();
            OrthoNormalize(ref up, ref forward);
            var right = Vector3.Cross(up, forward);
            var ret = new Quaternion();
            ret.W = (float)Math.Sqrt(1 + right.X + up.Y + forward.Z) * 0.5f;
            float w4_recip = 1f / (4f * ret.W);
            ret.X = (up.Z - forward.Y) * w4_recip;
            ret.Y = (forward.X - right.Z) * w4_recip;
            ret.Z = (right.Y - up.X) * w4_recip;
            return ret;
        }
        abstract class PathAnimationBase : IThnRoutine
        {
            public float Duration;
            public float StartPercent;
            public float StopPercent;
            public AttachFlags Flags;
            public ParameterCurve Curve;
            public ThnObject Path;


            double time = 0;

            public bool Run(Cutscene cs, double delta)
            {
                time += delta;
                if (time > Duration)
                {
                    if (Curve != null)
                        Process(Curve.GetValue(Duration, Duration));
                    else
                        Process(1);
                    return false;
                }
                if (Curve != null)
                    Process(Curve.GetValue((float)time, Duration));
                else
                    Process((float)time / Duration);
                return true;
            }

            void Process(float t)
            {
                float pct = MathHelper.Lerp(StartPercent, StopPercent, t);
                var path = Path.Entity.Path;
                var pos = path.GetPosition(pct);
                if ((Flags & AttachFlags.LookAt) == AttachFlags.LookAt)
                {
                    var orient = Matrix4.CreateFromQuaternion(LookRotation(path.GetDirection(pct, StartPercent > StopPercent), Vector3.UnitY));
                    if ((Flags & AttachFlags.Position) == AttachFlags.Position)
                        SetPositionOrientation(pos + Path.Translate, orient);
                    else
                        SetOrientation(orient);
                }
                else if ((Flags & AttachFlags.Orientation) == AttachFlags.Orientation)
                {
                    if ((Flags & AttachFlags.Position) == AttachFlags.Position)
                        SetPosition(pos + Path.Translate);
                }
                else if ((Flags & AttachFlags.Position) == AttachFlags.Position)
                {
                    SetPosition(pos + Path.Translate);
                }
            }

            protected abstract void SetPosition(Vector3 pos);
            protected abstract void SetPositionOrientation(Vector3 pos, Matrix4 orient);
            protected abstract void SetOrientation(Matrix4 orient);
        }

        class ObjectPathAnimation : PathAnimationBase
        {
            public ThnObject Object;

            protected override void SetPosition(Vector3 pos)
            {
                Object.Translate = pos;
            }
            protected override void SetPositionOrientation(Vector3 pos, Matrix4 orient)
            {
                Object.Translate = pos;
                Object.Rotate = orient;
            }
            protected override void SetOrientation(Matrix4 orient)
            {
                Object.Rotate = orient;
            }
        }


        public void Process(ThnEvent ev, Cutscene cs)
        {
            var obj = cs.Objects[(string)ev.Targets[0]];
            var path = cs.Objects[(string)ev.Targets[1]];
            var start = (float)ev.Properties["start_percent"];
            var stop = (float)ev.Properties["stop_percent"];
            var flags = ThnEnum.Check<AttachFlags>(ev.Properties["flags"]);
            cs.Coroutines.Add(new ObjectPathAnimation()
            {
                Duration = ev.Duration,
                StartPercent = start,
                StopPercent = stop,
                Flags = flags,
                Curve = ev.ParamCurve,
                Path = path,
                Object = obj
            });
        }
    }
}
