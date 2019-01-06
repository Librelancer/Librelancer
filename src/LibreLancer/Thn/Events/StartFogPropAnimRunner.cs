// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using LibreLancer.Thorn;
namespace LibreLancer
{
    [ThnEventRunner(EventTypes.StartFogPropAnim)]
    public class StartFogPropAnimRunner : IThnEventRunner
    {
        class FogPropAnimRoutine : IThnRoutine
        {
            public ThnEvent Event;
            public Vector3? FogColor;
            public float? FogStart;
            public float? FogEnd;
            public float? FogDensity;

            public Color4 OrigFogColor;
            public float OrigFogStart;
            public float OrigFogEnd;
            public float OrigFogDensity;

            double t = 0;
            public bool Run(Cutscene cs, double delta)
            {
                t += delta;
                var amount = MathHelper.Clamp((float)(t / Event.Duration), 0, 1);
                if (t > Event.Duration)
                    return false;
                if (FogColor != null)
                {
                    var f = FogColor.Value;
                    var c2 = new Color3f(f.X / 255f, f.Y / 255f, f.Z / 255f);
                    var c1 = new Color3f(OrigFogColor.R, OrigFogColor.G, OrigFogColor.B);
                    var cend = Utf.Ale.AlchemyEasing.EaseColorRGB(Utf.Ale.EasingTypes.Linear, amount, 0, 1, c1, c2);
                    cs.Renderer.SystemLighting.FogColor = new Color4(cend, 1);
                }
                if (FogStart != null)
                {
                    var f = FogStart.Value;
                    cs.Renderer.SystemLighting.FogRange.X =
                        MathHelper.Lerp(OrigFogStart, f, amount);
                }
                if(FogEnd != null)
                {
                    var f = FogEnd.Value;
                    cs.Renderer.SystemLighting.FogRange.Y =
                        MathHelper.Lerp(OrigFogEnd, f, amount);
                }
                if(FogDensity != null)
                {
                    var f = FogDensity.Value;
                    cs.Renderer.SystemLighting.FogDensity =
                        MathHelper.Lerp(OrigFogDensity, f, amount);
                }
               
                return true;
            }
        }

        public void Process(ThnEvent ev, Cutscene cs)
        {
            //fogmode is ignored.
            //fogdensity is ignored.
            var fogprops = (LuaTable)ev.Properties["fogprops"];

            object tmp;
            Vector3 tmp2;

            //Nullable since we are animating
            bool? fogon = null;
            Vector3? fogColor = null;
            float? fogstart = null;
            float? fogend = null;
            float? fogDensity = null;
            FogModes fogMode = FogModes.Linear;
            //Get values
            if (fogprops.TryGetValue("fogon", out tmp))
                fogon = ThnEnum.Check<bool>(tmp);
            if (fogprops.TryGetValue("fogmode", out tmp))
                fogMode = ThnEnum.Check<FogModes>(tmp);
            if (fogprops.TryGetValue("fogdensity", out tmp))
                fogDensity = (float)tmp;
            if (fogprops.TryGetVector3("fogcolor", out tmp2))
                fogColor = tmp2;
            if (fogprops.TryGetValue("fogstart", out tmp))
                fogstart = (float)tmp;
            if (fogprops.TryGetValue("fogend", out tmp))
                fogend = (float)tmp;

            if (fogon.HasValue) //i'm pretty sure this can't be animated
                cs.Renderer.SystemLighting.FogMode = fogon.Value ? fogMode : FogModes.None;

            //Set fog
            if (Math.Abs(ev.Duration) < float.Epsilon) //just set it
            {
                if (fogColor.HasValue)
                {
                    var v = fogColor.Value;
                    v *= (1 / 255f);
                    cs.Renderer.SystemLighting.FogColor = new Color4(v.X, v.Y, v.Z, 1);
                }
                if (fogstart.HasValue)
                    cs.Renderer.SystemLighting.FogRange.X = fogstart.Value;
                if (fogend.HasValue)
                    cs.Renderer.SystemLighting.FogRange.Y = fogend.Value;
                if (fogDensity.HasValue)
                    cs.Renderer.SystemLighting.FogDensity = fogDensity.Value;
            }
            else
                cs.Coroutines.Add(new FogPropAnimRoutine() //animate it!
                {
                    Event = ev,
                    FogDensity = fogDensity,
                    FogColor = fogColor,
                    FogStart = fogstart,
                    FogEnd = fogend,
                    OrigFogColor = cs.Renderer.SystemLighting.FogColor,
                    OrigFogStart = cs.Renderer.SystemLighting.FogRange.X,
                    OrigFogEnd = cs.Renderer.SystemLighting.FogRange.Y,
                    OrigFogDensity = cs.Renderer.SystemLighting.FogDensity
                });
        }
    }
}
