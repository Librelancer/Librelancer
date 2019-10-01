// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
namespace LibreLancer
{
    [ThnEventRunner(EventTypes.StartPathAnimation)]
    public class StartPathAnimationRunner : IThnEventRunner
    {
        abstract class PathAnimationBase : IThnRoutine
        {
            public float Duration;
            public float StartPercent;
            public float StopPercent;
            public Vector3 Offset;
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
                var mat = Path.Rotate * Matrix4.CreateTranslation(Path.Translate);
                var pos = mat.Transform(path.GetPosition(pct) + Offset);
                if ((Flags & AttachFlags.LookAt) == AttachFlags.LookAt)
                {
                    var orient = Matrix4.CreateFromQuaternion(Quaternion.LookRotation(path.GetDirection(pct, StartPercent > StopPercent), Vector3.UnitY)) * Path.Rotate;
                    if ((Flags & AttachFlags.Position) == AttachFlags.Position)
                        SetPositionOrientation(pos, orient);
                    else
                        SetOrientation(orient);
                }
                else if ((Flags & AttachFlags.Orientation) == AttachFlags.Orientation)
                {
                    var orient = Path.Rotate * Matrix4.CreateFromQuaternion(path.GetOrientation(pct));
                    SetOrientation(orient);
                    if ((Flags & AttachFlags.Position) == AttachFlags.Position)
                        SetPosition(pos);
                }
                else if ((Flags & AttachFlags.Position) == AttachFlags.Position)
                {
                    SetPosition(pos);
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
            var offset = Vector3.Zero;
            ev.Properties.TryGetVector3("offset", out offset);
            cs.Coroutines.Add(new ObjectPathAnimation()
            {
                Duration = ev.Duration,
                StartPercent = start,
                StopPercent = stop,
                Flags = flags,
                Curve = ev.ParamCurve,
                Path = path,
                Offset = offset,
                Object = obj
            });
        }
    }
}
