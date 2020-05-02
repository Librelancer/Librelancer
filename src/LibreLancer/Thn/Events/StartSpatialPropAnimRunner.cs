// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Numerics;
using LibreLancer.Thorn;
namespace LibreLancer
{
    [ThnEventRunner(EventTypes.StartSpatialPropAnim)]
    public class StartSpatialPropAnimRunner : IThnEventRunner
    {
        class AxisRotation
        {
            public Matrix4x4 OriginalRotate;
            public Vector3 Axis;
            public float Degrees;

            public float GetRads(float pct)
            {
                var degs = MathHelper.Lerp(0, Degrees, pct);
                return MathHelper.DegreesToRadians(-degs);
            }
        }
        public void Process(ThnEvent ev, Cutscene cs)
        {
            if (ev.Targets.Capacity == 0) return;
            ThnObject objA;
            
            if (!cs.Objects.TryGetValue((string)ev.Targets[0], out objA))
            {
                FLLog.Error("Thn", "Object does not exist " + (string)ev.Targets[0]);
                return;
            }

            bool hasPos = false;
            Quaternion? q_orient = null;
            if (ev.Targets.Capacity >= 1)
            {

                var props = (LuaTable) ev.Properties["spatialprops"];
                Vector3 pos;
                object tmp;
                if (props.TryGetValue("q_orient", out tmp))
                {
                    var tb = (LuaTable) tmp;
                    q_orient = new Quaternion((float) tb[1], (float) tb[2], (float) tb[3], (float) tb[0]);
                }

                if (props.TryGetValue("orient", out tmp))
                {
                    var orient = ThnScript.GetMatrix((LuaTable) tmp);
                    q_orient = orient.ExtractRotation();
                }

                AxisRotation axisRotation = null;
                if (props.TryGetValue("axisrot", out tmp))
                {
                    var axisRot_Table = (LuaTable) tmp;
                    axisRotation = new AxisRotation();
                    if (!axisRot_Table.TryGetVector3(1, out axisRotation.Axis)) {
                        FLLog.Error("Thn", "invalid axisrot");
                        return;
                    }
                    axisRotation.Axis = Vector3.TransformNormal(axisRotation.Axis, objA.Rotate);
                    axisRotation.Degrees = (float) axisRot_Table[0];
                    axisRotation.OriginalRotate = objA.Rotate;
                }
                
                hasPos = props.TryGetVector3("pos", out pos);
                if(ev.Targets.Capacity > 1)
                {
                    ThnObject objB;
                    if (!cs.Objects.TryGetValue((string)ev.Targets[1], out objB))
                    {
                        FLLog.Error("Thn", "Object does not exist " + (string)ev.Targets[1]);
                        return;
                    }
                    if(ev.Duration < float.Epsilon)
                    {
                        objA.Translate = objB.Translate;
                        objA.Rotate = objB.Rotate;
                    }
                    else
                    {
                        cs.Coroutines.Add(new FollowSpatialRoutine()
                        {
                            Duration = ev.Duration,
                            HasPos = hasPos,
                            HasQuat = q_orient != null,
                            This = objA,
                            Follow = objB
                        });
                    }
                }
                else
                {
                    if (ev.Duration < float.Epsilon)
                    {
                        if (hasPos) objA.Translate = pos;
                        if (q_orient != null) objA.Rotate = Matrix4x4.CreateFromQuaternion(q_orient.Value);
                    }
                    else
                    {
                        cs.Coroutines.Add(new StaticSpatialRoutine()
                        {
                            Duration = ev.Duration,
                            HasPos = hasPos,
                            HasQuat = q_orient != null,
                            EndPos = pos,
                            EndQuat = q_orient ?? Quaternion.Identity,
                            This = objA,
                            AxisRot = axisRotation
                        });
                    }
                }
            }

        }
        abstract class SpatialAnimRoutine : IThnRoutine
        {
            public double Duration;
            public double Time;
            public bool HasPos;
            public bool HasQuat;
            public AxisRotation AxisRot;
            public ThnObject This;

            public bool Run(Cutscene cs, double delta)
            {
                Time += delta;
                Time = MathHelper.Clamp(Time, 0, Duration);
                
                if (HasPos) This.Translate = GetPosition(delta);
                if (HasQuat) This.Rotate = Matrix4x4.CreateFromQuaternion(GetOrientation(delta));
                if (AxisRot != null)
                {
                    This.Rotate = AxisRot.OriginalRotate *
                                  Matrix4x4.CreateFromAxisAngle(AxisRot.Axis, AxisRot.GetRads((float) (Time / Duration)));
                }
                return Time != Duration;
            }

            protected Vector3 GetPosition(double delta)
            {
                var end = PosEnd();
                if (Time == Duration) return end;
                var len = (end - This.Translate).Length();
                if (len <= float.Epsilon) return end;
                var dir = (end - This.Translate).Normalized();
                var pct = (float)(delta / (Duration - Time));
                if (pct > 1) pct = 1;
                return This.Translate + (dir * len * pct);
            }
            protected Quaternion GetOrientation(double delta)
            {
                var end = QEnd();
                if (Time == Duration) return end;
                var pct = (float)(delta / (Duration - Time));
                if (pct >= 1) return end;
                return Quaternion.Slerp(This.Rotate.ExtractRotation(), end, pct);
            }
            protected abstract Quaternion QEnd();
            protected abstract Vector3 PosEnd();
        }
        class StaticSpatialRoutine : SpatialAnimRoutine, IThnRoutine
        {
            public Vector3 EndPos;
            public Quaternion EndQuat;
            protected override Quaternion QEnd() => EndQuat;
            protected override Vector3 PosEnd() => EndPos;
        }
        class FollowSpatialRoutine : SpatialAnimRoutine, IThnRoutine
        {
            public ThnObject Follow;
            protected override Quaternion QEnd()
            {
                return Follow.Rotate.ExtractRotation();
            }
            protected override Vector3 PosEnd()
            {
                return Follow.Translate;
            }
        }
     
    }
}
