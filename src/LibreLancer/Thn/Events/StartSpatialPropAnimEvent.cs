// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Numerics;
using LibreLancer.Thorn;

namespace LibreLancer.Thn
{
    public class StartSpatialPropAnimEvent : ThnEvent
    {
        [Flags]
        public enum AnimVars
        {
            Nothing = 0,
            QOrient = 1 << 1,
            Orient = 1 << 2,
            AxisRot = 1 << 3,
            Pos = 1 << 4
        }

        public AnimVars SetFlags;
        public Quaternion Q_Orient;
        public Matrix4x4 Orient;
        public AxisRotation AxisRot;
        public Vector3 Pos;
        public struct AxisRotation
        {
            public Vector3 Axis;
            public float Degrees;
            public float GetRads(float pct)
            {
                var degs = MathHelper.Lerp(0, Degrees, pct);
                return MathHelper.DegreesToRadians(-degs);
            }
        }

        public StartSpatialPropAnimEvent() { }

        public StartSpatialPropAnimEvent(LuaTable table) : base(table)
        {
            if (!GetProps(table, out var props)) return;
            if (!GetValue(props, "spatialprops", out LuaTable sp)) return;
            if (GetValue(sp, "q_orient", out Q_Orient)) SetFlags |= AnimVars.QOrient;
            if (GetValue(sp, "orient", out Orient)) SetFlags |= AnimVars.Orient;
            if (GetValue(sp, "axisrot", out LuaTable axisrot))
            {
                SetFlags |= AnimVars.AxisRot;
                if (!axisrot.TryGetVector3(1, out AxisRot.Axis)) {
                    FLLog.Error("Thn", "START_SPATIAL_PROP_ANIM axisrot missing axis");
                    AxisRot.Axis = Vector3.UnitY;
                }
                AxisRot.Degrees = (float) axisrot[0];
            }
            if (GetValue(sp, "pos", out Pos)) SetFlags |= AnimVars.Pos;
        }


        public override void Run(ThnScriptInstance instance)
        {
            if (Targets.Length == 0) return;
            bool hasPos = (SetFlags & AnimVars.Pos) == AnimVars.Pos;
            bool hasQuat = (SetFlags & AnimVars.Orient) == AnimVars.Orient ||
                           (SetFlags & AnimVars.QOrient) == AnimVars.QOrient;
            
            Quaternion quat = Q_Orient;
            if ((SetFlags & AnimVars.Orient) == AnimVars.Orient)
                quat = Orient.ExtractRotation();

            ThnObject objA;
            if (!instance.Objects.TryGetValue(Targets[0], out objA))
            {
                FLLog.Error("Thn", $"Object does not exist {Targets[0]}");
                return;
            }

            AxisRotation trAxisRot = AxisRot;
            if ((SetFlags & AnimVars.AxisRot) == AnimVars.AxisRot) {
                trAxisRot.Axis = Vector3.TransformNormal(trAxisRot.Axis, objA.Rotate);
            }

            if (Targets.Length > 1)
            {
                if (!instance.Objects.TryGetValue(Targets[1], out var objB))
                {
                    FLLog.Error("Thn", $"Object does not exist {Targets[1]}");
                    return;
                }
                if(Duration < float.Epsilon)
                {
                    objA.Translate = objB.Translate;
                    objA.Rotate = objB.Rotate;
                }
                else
                {
                    instance.AddProcessor(new FollowSpatialRoutine()
                    {
                        Event = this,
                        HasPos = hasPos,
                        HasQuat = hasQuat,
                        This = objA,
                        Follow = objB,
                        OriginalRotate = objA.Rotate
                    });
                }
            }
            else
            {
                if (Duration < float.Epsilon)
                {
                    if (hasPos) objA.Translate = Pos;
                    if (hasQuat) objA.Rotate = Matrix4x4.CreateFromQuaternion(quat);
                    if ((SetFlags & AnimVars.AxisRot) == AnimVars.AxisRot) {
                        objA.Rotate = objA.Rotate * Matrix4x4.CreateFromAxisAngle(trAxisRot.Axis, trAxisRot.GetRads(1));
                    }
                }
                else
                {
                    instance.AddProcessor(new StaticSpatialRoutine()
                    {
                        Event = this,
                        HasPos = hasPos,
                        HasQuat = hasQuat,
                        EndPos = Pos,
                        EndQuat = quat,
                        This = objA,
                        AxisRot = trAxisRot,
                        OriginalRotate = objA.Rotate
                    });
                }
            }
        }

        abstract class SpatialAnimRoutine : ThnEventProcessor
        {
            public StartSpatialPropAnimEvent Event;
            public bool HasPos;
            public bool HasQuat;
            public AxisRotation AxisRot;
            public Matrix4x4 OriginalRotate;
            public ThnObject This;

            private double time;
            
            public override bool Run(double delta)
            {
                time = MathHelper.Clamp(time + delta, 0, Event.Duration);
                
                if (HasPos) This.Translate = GetPosition(delta);
                if (HasQuat) This.Rotate = Matrix4x4.CreateFromQuaternion(GetOrientation(delta));
                if ((Event.SetFlags & AnimVars.AxisRot) == AnimVars.AxisRot)
                {
                    This.Rotate = OriginalRotate *
                                  Matrix4x4.CreateFromAxisAngle(AxisRot.Axis, AxisRot.GetRads((float) (time / Event.Duration)));
                }
                return time < Event.Duration;
            }

            protected Vector3 GetPosition(double delta)
            {
                var end = PosEnd();
                if (time >= Event.Duration) return end;
                var len = (end - This.Translate).Length();
                if (len <= float.Epsilon) return end;
                var dir = (end - This.Translate).Normalized();
                var pct = (float)(delta / (Event.Duration - time));
                if (pct > 1) pct = 1;
                return This.Translate + (dir * len * pct);
            }
            protected Quaternion GetOrientation(double delta)
            {
                var end = QEnd();
                if (time >= Event.Duration) return end;
                var pct = (float)(delta / (Event.Duration - time));
                if (pct >= 1) return end;
                return Quaternion.Slerp(This.Rotate.ExtractRotation(), end, pct);
            }
            protected abstract Quaternion QEnd();
            protected abstract Vector3 PosEnd();
        }
        class StaticSpatialRoutine : SpatialAnimRoutine
        {
            public Vector3 EndPos;
            public Quaternion EndQuat;
            protected override Quaternion QEnd() => EndQuat;
            protected override Vector3 PosEnd() => EndPos;
        }
        class FollowSpatialRoutine : SpatialAnimRoutine
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