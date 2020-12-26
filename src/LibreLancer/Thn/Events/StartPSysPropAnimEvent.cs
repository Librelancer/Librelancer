// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using LibreLancer.Thorn;

namespace LibreLancer.Thn
{
    public class StartPSysPropAnimEvent : ThnEvent
    {
        public StartPSysPropAnimEvent() { }

        public float SParam;
        public bool Set;
        public StartPSysPropAnimEvent(LuaTable table) : base(table)
        {
            if (GetProps(table, out var props))
            {
                if (!GetValue(props, "psysprops", out LuaTable psys))
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
            var ren = ((ParticleEffectRenderer)obj.Object.RenderComponent);
            if (Duration <= float.Epsilon) {
                ren.SParam = SParam;
            }
            else
            {
                instance.AddProcessor(new SParamAnimation()
                {
                    Renderer = ren,
                    Event = this,
                    StartValue = ren.SParam
                });
            }
        }

        class SParamAnimation : ThnEventProcessor
        {
            public ParticleEffectRenderer Renderer;
            public StartPSysPropAnimEvent Event;
            public float StartValue;

            private double time;
            public override bool Run(double delta)
            {
                time += delta;
                Renderer.SParam = MathHelper.Lerp(
                    StartValue, 
                    Event.SParam, 
                    Event.GetT((float) time)
                    );
                return time < Event.Duration;
            }
        }
    }
}