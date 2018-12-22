// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using LibreLancer.Thorn;
namespace LibreLancer
{
    [ThnEventRunner(EventTypes.StartSpatialPropAnim)]
    public class StartSpatialPropAnimRunner : IThnEventRunner
    {
        public void Process(ThnEvent ev, Cutscene cs)
        {
            for (int i = 0; i < ev.Targets.Capacity; i++)
            {
                ThnObject obj;
                if (!cs.Objects.TryGetValue((string)ev.Targets[i], out obj))
                {
                    FLLog.Error("Thn", "Object does not exist " + (string)ev.Targets[0]);
                    return;
                }

                var props = (LuaTable)ev.Properties["spatialprops"];
                Quaternion? q_orient = null;
                Vector3 pos;
                object tmp;
                if (ev.Properties.TryGetValue("q_orient", out tmp))
                {
                    var tb = (LuaTable)tmp;
                    q_orient = new Quaternion((float)tb[0], (float)tb[1], (float)tb[2], (float)tb[3]);
                }
                if (ev.Properties.TryGetValue("orient", out tmp))
                {
                    var orient = ThnScript.GetMatrix((LuaTable)tmp);
                    q_orient = orient.ExtractRotation();
                }
                bool hasPos = ev.Properties.TryGetVector3("pos", out pos);
                if (obj.Camera != null)
                {
                    if (ev.Duration > float.Epsilon)
                    {
                        cs.Coroutines.Add(new CameraSpatialRoutine()
                        {
                            Camera = obj.Camera,
                            Duration = ev.Duration,
                            Start = obj.Camera.Position,
                            End = pos,
                            HasPos = hasPos,
                            HasQuat = q_orient != null,
                            QStart = obj.Camera.Orientation.ExtractRotation(),
                            QEnd = q_orient ?? Quaternion.Identity
                        });
                    }
                    else
                    {
                        if (hasPos) obj.Camera.Position = pos;
                        if (q_orient != null) obj.Camera.Orientation = Matrix4.CreateFromQuaternion(q_orient.Value);
                    }


                }
                else if (obj.Object != null)
                {
                    if (ev.Duration > float.Epsilon)
                    {
                        cs.Coroutines.Add(new ObjectSpatialRoutine()
                        {
                            Object = obj.Object,
                            Duration = ev.Duration,
                            Start = obj.Object.Transform.Transform(Vector3.Zero),
                            End = pos,
                            HasPos = hasPos,
                            HasQuat = q_orient != null,
                            QStart = obj.Object.Transform.ExtractRotation(),
                            QEnd = q_orient ?? Quaternion.Identity
                        });
                    }
                    else
                    {
                        if (hasPos && q_orient != null)
                            obj.Object.Transform = Matrix4.CreateFromQuaternion(q_orient.Value) * Matrix4.CreateTranslation(pos);
                        else if (hasPos)
                        {
                            var rot = obj.Object.Transform.ExtractRotation();
                            obj.Object.Transform = Matrix4.CreateFromQuaternion(rot) * Matrix4.CreateTranslation(pos);
                        }
                        else if (q_orient != null)
                        {
                            var translation = obj.Object.Transform.ExtractTranslation();
                            obj.Object.Transform = Matrix4.CreateFromQuaternion(q_orient.Value) * Matrix4.CreateTranslation(translation);
                        }
                    }
                }
                else if (obj.Light != null)
                {
                    if (hasPos)
                    {
                        if (ev.Duration > float.Epsilon)
                        {

                        }
                        else
                        {
                            obj.Light.Light.Position = pos;
                        }
                    }
                }
            }
        }
        class SpatialAnimRoutine
        {
            public double Duration;
            public double Time;
            public Vector3 Start;
            public Vector3 End;
            public Quaternion QStart;
            public Quaternion QEnd;
            public bool HasPos;
            public bool HasQuat;
            protected Vector3 GetPosition()
            {
                if (Time == Duration) return End;
                var len = (End - Start).Length;
                var dir = (End - Start).Normalized();
                return Start + (dir * len * (float)(Time / Duration));
            }
            protected Quaternion GetOrientation()
            {
                if (Time == Duration) return QEnd;
                return Quaternion.Slerp(QStart, QEnd, (float)(Time / Duration));
            }
        }
        class CameraSpatialRoutine : SpatialAnimRoutine, IThnRoutine
        {
            public ThnCameraTransform Camera;

            public bool Run(Cutscene cs, double delta)
            {
                Time += delta;
                Time = MathHelper.Clamp(Time, 0, Duration);
                if(HasPos) Camera.Position = GetPosition();
                if (HasQuat) Camera.Orientation = Matrix4.CreateFromQuaternion(GetOrientation());
                return Time != Duration;
            }
        }
        class ObjectSpatialRoutine : SpatialAnimRoutine, IThnRoutine
        {
            public GameObject Object;

            public bool Run(Cutscene cs, double delta)
            {
                Time += delta;
                Time = MathHelper.Clamp(Time, 0, Duration);
                if (HasPos && HasQuat)
                {
                    Object.Transform = Matrix4.CreateFromQuaternion(GetOrientation()) * 
                    Matrix4.CreateTranslation(GetPosition());
                } else if (HasPos)
                {
                    var pos = GetPosition();
                    var rot = Object.Transform.ExtractRotation();
                    Object.Transform = Matrix4.CreateFromQuaternion(rot) * Matrix4.CreateTranslation(pos);
                }
                else if(HasQuat)
                {
                    var translation = Object.Transform.ExtractTranslation();
                    Object.Transform = Matrix4.CreateFromQuaternion(GetOrientation()) * Matrix4.CreateTranslation(translation);
                }
                return Time != Duration;
            }
        }
     
    }
}
