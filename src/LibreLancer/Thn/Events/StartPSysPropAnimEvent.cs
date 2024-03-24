// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using LibreLancer.Render;
using LibreLancer.Thorn;

namespace LibreLancer.Thn.Events
{
    public class StartPSysPropAnimEvent : ThnEvent
    {
        public StartPSysPropAnimEvent() { }

        public float SParam;
        public bool Set;
        public StartPSysPropAnimEvent(ThornTable table) : base(table)
        {
            if (GetProps(table, out var props))
            {
                if (!GetValue(props, "psysprops", out ThornTable psys))
                    return;
                Set = GetValue(psys, "sparam", out SParam);
            }
        }
        
        public override void Run(ThnScriptInstance instance)
        {
            if (!Set) return;
            if (!instance.Objects.TryGetValue(Targets[0], out var obj))
            {
                FLLog.Error("Thn", $"Entity {Targets[0]} does not exist");
                return;
            }
            if (Duration <= float.Epsilon) {
                if (obj.Engine != null) {
                    obj.Engine.Speed = SParam;
                }
                else {
                    var ren = ((ParticleEffectRenderer)obj.Object.RenderComponent);
                    ren.SParam = SParam;
                }
            }
            else
            {
                float startValue;
                if (obj.Engine != null)
                {
                    startValue = obj.Engine.Speed;
                }
                else
                {
                    var ren = ((ParticleEffectRenderer)obj.Object.RenderComponent);
                    startValue = ren.SParam;
                }
                instance.AddProcessor(new SParamAnimation()
                {
                    Object = obj,
                    Event = this,
                    StartValue = startValue
                });
            }
        }

        class SParamAnimation : ThnEventProcessor
        {
            public ThnObject Object;
            public StartPSysPropAnimEvent Event;
            public float StartValue;

            private double time;
            public override bool Run(double delta)
            {
                time += delta;
                var value = MathHelper.Lerp(
                    StartValue, 
                    Event.SParam, 
                    Event.GetT((float) time)
                    );
                if (Object.Engine != null)
                {
                    Object.Engine.Speed = value;
                }
                else
                {
                    var ren = ((ParticleEffectRenderer)Object.Object.RenderComponent);
                    ren.SParam = value;
                }
                return time < Event.Duration;
            }
        }
    }
}