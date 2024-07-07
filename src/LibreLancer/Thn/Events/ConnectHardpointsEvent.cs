// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System.Numerics;
using BepuPhysics.Collidables;
using LibreLancer.Render;
using LibreLancer.Thorn;
using LibreLancer.World;

namespace LibreLancer.Thn.Events
{
    public class ConnectHardpointsEvent : ThnEvent
    {
        public string Hardpoint;
        public string ParentHardpoint;
        public ConnectHardpointsEvent() { }




        public ConnectHardpointsEvent(ThornTable table) : base(table)
        {
            if (!GetProps(table, out var props)) return;
            if (props.TryGetValue("hardpoint", out var hp))
                Hardpoint = hp.ToString();
            if (props.TryGetValue("parent_hardpoint", out var php))
                ParentHardpoint = php.ToString();
        }

        private double time = 0;
        public override void Run(ThnScriptInstance instance)
        {
            if (Targets.Length < 2) {
                FLLog.Error("Thn","CONNECT_HARDPOINTS requires 2 targets");
            }
            if (!instance.Objects.TryGetValue(Targets[0], out var child))
            {
                FLLog.Error("Thn", $"Entity {Targets[0]} does not exist");
                if (child.Object == null){
                    FLLog.Error("Thn", $"Entity {Targets[0]} has no hardpoints");
                    return;
                }
                return;
            }
            if (!instance.Objects.TryGetValue(Targets[1], out var parent))
            {
                FLLog.Error("Thn", $"Entity {Targets[0]} does not exist");
                if (parent.Object == null) {
                    FLLog.Error("Thn", $"Entity {Targets[1]} has no hardpoints");
                    return;
                }
                return;
            }

            IRenderHardpoint childHp;
            IRenderHardpoint parentHp;
            if (string.IsNullOrEmpty(Hardpoint) || (childHp = GetHardpoint(child.Object, Hardpoint)) == null)
            {
                FLLog.Error("Thn", $"Could not find hardpoint on {Targets[0]}");
                return;
            }
            if (string.IsNullOrEmpty(Hardpoint) || (parentHp = GetHardpoint(parent.Object, ParentHardpoint)) == null)
            {
                FLLog.Error("Thn", $"Could not find hardpoint on {Targets[0]}");
                return;
            }
            instance.AddProcessor(new ProcessConnection()
            {
                ParentHardpoint = parentHp,
                ChildHardpoint = childHp,
                Parent = parent,
                Child = child,
                Duration = Duration
            });
        }

        class ProcessConnection : ThnEventProcessor
        {
            public ThnObject Parent;
            public ThnObject Child;
            public IRenderHardpoint ParentHardpoint;
            public IRenderHardpoint ChildHardpoint;
            double time = 0;
            public double Duration;

            public override bool Run(double delta)
            {
                time += delta;
                var tr = ChildHardpoint.Transform.Inverse() * ParentHardpoint.Transform *
                        new Transform3D(Parent.Translate, Parent.Rotate);
                Child.Translate = tr.Position;
                Child.Rotate = tr.Orientation;
                return time < Duration;
            }
        }
    }
}
