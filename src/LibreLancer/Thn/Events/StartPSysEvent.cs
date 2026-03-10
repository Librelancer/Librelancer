// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using LibreLancer.Client.Components;
using LibreLancer.Render;
using LibreLancer.Thorn;

namespace LibreLancer.Thn.Events
{
    public class StartPSysEvent : ThnEvent
    {
        public StartPSysEvent() { }
        public StartPSysEvent(ThornTable table) : base(table) { }

        public override void Run(ThnScriptInstance instance)
        {
            if (!instance.Objects.TryGetValue(Targets[0], out var obj))
            {
                FLLog.Error("Thn", "Entity " + Targets[0] + " does not exist");
                return;
            }

            if (obj.Engine != null)
            {
                obj.Engine.Active = true;
                instance.AddProcessor(new StopEngine(obj.Engine, Duration));
                return;
            }

            if (obj.Object == null)
            {
                FLLog.Error("Thn", "Entity " + Targets[0] + " null renderer");
                return;
            }
            
            var r = (ParticleEffectRenderer)obj.Object.RenderComponent!;
            r.Active = true;
            instance.AddProcessor(new StopPSys(r, Duration));
        }

        private class StopEngine : ThnEventProcessor
        {
            private double time;
            public double Duration;
            public CEngineComponent Fx;

            public StopEngine(CEngineComponent fx, double duration)
            {
                Fx = fx;
                Duration = duration;
            }

            public override bool Run(double delta)
            {
                time += delta;

                if(time >= Duration)
                {
                    Fx.Active = false;
                    return false;
                }

                return true;
            }
        }

        private class StopPSys : ThnEventProcessor
        {
            private double time;
            public double Duration;
            public ParticleEffectRenderer Fx;

            public StopPSys(ParticleEffectRenderer fx, double duration)
            {
                Fx = fx;
                Duration = duration;
            }

            public override bool Run(double delta)
            {
                time += delta;
                if(time >= Duration)
                {
                    Fx.Active = false;
                    return false;
                }
                return true;
            }

        }
    }
}
