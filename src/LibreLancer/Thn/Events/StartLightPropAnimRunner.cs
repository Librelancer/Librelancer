// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System.Numerics;
using LibreLancer.Thorn;
using LibreLancer.Utf.Ale;

namespace LibreLancer
{
    [ThnEventRunner(EventTypes.StartLightPropAnim)]
    public class StartLightPropAnimRunner : IThnEventRunner
    {
        public void Process(ThnEvent ev, Cutscene cs)
        {
            if (!cs.Objects.ContainsKey((string) ev.Targets[0]))
            {
                FLLog.Error("Thn", $"Entity {ev.Targets[0]} does not exist");
                return;
            }
            var obj = cs.Objects[(string)ev.Targets[0]];
            if (obj.Light == null)
            {
                FLLog.Error("Thn", $"Entity {ev.Targets[0]} is not a light");
                return;
            }

            object tmp;
            LuaTable lightprops;
            if (ev.Properties.TryGetValue("lightprops", out tmp))
                lightprops = (LuaTable) tmp;
            else
            {
                FLLog.Warning("Thn", "Light prop animation with no properties");
                return;
            }

            Vector3 vtmp;
            Color3f? targetDiffuse = null;
            Color3f? targetAmbient = null;
            
            if (lightprops.TryGetValue("on", out tmp))
                obj.Light.Active = ThnEnum.Check<bool>(tmp);
            if (lightprops.TryGetVector3("diffuse", out vtmp)) {
                targetDiffuse = new Color3f(vtmp);
                if (ev.Duration <= 0) obj.Light.Light.Color = new Color3f(vtmp);
            }
            if (lightprops.TryGetVector3("ambient", out vtmp))
            {
                targetAmbient = new Color3f(vtmp);
                if (ev.Duration <= 0) obj.Light.Light.Ambient = new Color3f(vtmp);
            }
            if (ev.Duration > 0)
            {
                cs.Coroutines.Add(new AnimLightProp()
                {
                    Source = obj.Light.Light,
                    Target = obj.Light,
                    TargetDiffuse = targetDiffuse,
                    TargetAmbient = targetAmbient,
                    Duration = ev.Duration,
                    ParamCurve = ev.ParamCurve
                });
            }
        }

        class AnimLightProp : IThnRoutine
        {
            public RenderLight Source;
            public DynamicLight Target;
            public Color3f? TargetDiffuse;
            public Color3f? TargetAmbient;
            public ParameterCurve ParamCurve;
            public double Duration;
            private double time;
            public bool Run(Cutscene cs, double delta)
            {
                time += delta;
                if (time >= Duration)
                {
                    Target.Light.Color = TargetDiffuse ?? Source.Color;
                    Target.Light.Ambient = TargetAmbient ?? Source.Ambient;
                    return false;
                }
                var pct = (float)(time / Duration);
                if (ParamCurve != null) pct = ParamCurve.GetValue((float) time, (float) Duration);
                if (TargetDiffuse != null)
                {
                    Target.Light.Color = AlchemyEasing.EaseColor(EasingTypes.Linear, pct, 0, 1, Source.Color,
                        TargetDiffuse.Value);
                }
                if (TargetAmbient != null)
                {
                    Target.Light.Ambient = AlchemyEasing.EaseColor(EasingTypes.Linear, pct, 0, 1, Source.Color,
                        TargetAmbient.Value);
                }
                return true;
            }
        }
    }
}