// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using LibreLancer.Render;
using LibreLancer.Thorn;

namespace LibreLancer.Thn.Events
{
    public class StartFloorHeightAnimEvent : ThnEvent
    {
        //Todo: Second target (marker?)

        public float FloorHeight;
        public string TargetPart;
        public TargetTypes TargetType;

        public StartFloorHeightAnimEvent() {}

        public StartFloorHeightAnimEvent(ThornTable table) : base(table)
        {
            if (GetProps(table, out var props))
            {
                GetValue(props, "floor_height", out FloorHeight);
                GetValue(props, "target_part", out TargetPart);
                GetValue(props, "target_type", out TargetType);
            }
        }

        public override void Run(ThnScriptInstance instance)
        {
            if (!instance.Objects.TryGetValue(Targets[0], out var obj))
            {
                FLLog.Error("Thn", $"Entity {Targets[0]} does not exist");
                return;
            }
            if (obj.Object == null || obj.Object.RenderComponent is not CharacterRenderer)
            {
                FLLog.Error("Thn", $"FloorHeightAnim: Entity {Targets[0]} is not deformable");
                return;
            }

            if (TargetType != TargetTypes.Root)
            {
                FLLog.Error("Thn", $"({Time}) Unknown FlrHeightAnim target {TargetType}, ignoring");
            }

            if (Targets.Length > 1)
            {
                FLLog.Error("Thn", $"({Time}) FlrHeightAnim Unknown second target ({Targets[1]}), ignoring");
            }


            if (Duration <= 0)
            {
                FLLog.Debug("Info", $"FlrHeightAnim {Targets[0]} {1.00} = {FloorHeight}");
                ((CharacterRenderer)obj.Object.RenderComponent).Skeleton.FloorHeight = FloorHeight;
            }
            else
            {
                var skel = ((CharacterRenderer)obj.Object.RenderComponent).Skeleton;
                instance.AddProcessor(new FloorHeightAnimator()
                {
                    Event = this,
                    OrigFloorHeight = skel.FloorHeight,
                    Skeleton = skel
                });
            }
        }

        class FloorHeightAnimator : ThnEventProcessor
        {
            public StartFloorHeightAnimEvent Event;
            public float OrigFloorHeight;
            public DfmSkeletonManager Skeleton;

            double time = 0;
            public override bool Run(double delta)
            {
                time += delta;
                var t = Event.GetT((float) time);
                Skeleton.FloorHeight = MathHelper.Lerp(OrigFloorHeight, Event.FloorHeight, t);
                FLLog.Debug("Info", $"FlrHeightAnim {Event.Targets[0]} {t} = {Skeleton.FloorHeight}");
                return time < Event.Duration;
            }
        }
    }
}
