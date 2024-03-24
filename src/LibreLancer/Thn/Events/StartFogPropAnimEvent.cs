// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Numerics;
using LibreLancer.Render;
using LibreLancer.Thorn;

namespace LibreLancer.Thn.Events
{
    public class StartFogPropAnimEvent : ThnEvent
    {
        [Flags]
        public enum AnimVars
        {
            Nothing = 0,
            FogOn = 1 << 1,
            FogMode = 1 << 2,
            FogDensity = 1 << 3,
            FogColor = 1 << 4,
            FogStart = 1 << 5,
            FogEnd = 1 << 6
        }

        public AnimVars SetFlags;
        public bool FogOn;
        public FogModes FogMode;
        public Vector3 FogColor;
        public float FogDensity;
        public float FogStart;
        public float FogEnd;
        
        public StartFogPropAnimEvent() { }

        public StartFogPropAnimEvent(ThornTable table) : base(table)
        {
            //Get Tables
            if (!GetProps(table, out var props)) return;
            if (!GetValue(props, "fogprops", out ThornTable fog)) return;
            //Set Properties
            if (GetValue(fog, "fogon", out FogOn)) SetFlags |= AnimVars.FogOn;
            if (GetValue(fog, "fogmode", out FogMode)) SetFlags |= AnimVars.FogMode;
            if (GetValue(fog, "fogcolor", out FogColor))
            {
                SetFlags |= AnimVars.FogColor;
                FogColor *= (1 / 255f);
            }
            if (GetValue(fog, "fogdensity", out FogDensity)) SetFlags |= AnimVars.FogDensity;
            if (GetValue(fog, "fogstart", out FogStart)) SetFlags |= AnimVars.FogStart;
            if (GetValue(fog, "fogend", out FogEnd)) SetFlags |= AnimVars.FogEnd;
        }

        public override void Run(ThnScriptInstance instance)
        {
            if (SetFlags == AnimVars.Nothing) return; //Nothing to change
            var light = instance.Cutscene.Renderer.SystemLighting;
            //mode and fog on/off
            if ((SetFlags & AnimVars.FogOn) == AnimVars.FogOn)
            {
                if ((SetFlags & AnimVars.FogMode) == AnimVars.FogMode)
                    light.FogMode = FogOn ? FogMode : FogModes.None;
                else
                    if (!FogOn) light.FogMode = FogModes.None;
            }
            if (SetFlags == (AnimVars.FogMode | AnimVars.FogOn) ||
                SetFlags == AnimVars.FogOn ||
                SetFlags == AnimVars.FogMode)
                return; //Nothing to animate
            //anim
            if (Duration < float.Epsilon)
            {
                if ((SetFlags & AnimVars.FogColor) == AnimVars.FogColor)
                    light.FogColor = new Color4(FogColor.X, FogColor.Y, FogColor.Z, 1);
                if ((SetFlags & AnimVars.FogDensity) == AnimVars.FogDensity)
                    light.FogDensity = FogDensity;
                if ((SetFlags & AnimVars.FogStart) == AnimVars.FogStart)
                    light.FogRange.X = FogStart;
                if ((SetFlags & AnimVars.FogEnd) == AnimVars.FogEnd)
                    light.FogRange.Y = FogEnd;
            }
            else
            {
                instance.AddProcessor(new FogPropAnim()
                {
                    Event = this,
                    Lights = light,
                    OrigFogColor = light.FogColor,
                    OrigFogStart = light.FogRange.X,
                    OrigFogEnd = light.FogRange.Y,
                    OrigFogDensity = light.FogDensity
                });
            }
        }

        class FogPropAnim : ThnEventProcessor
        {
            public StartFogPropAnimEvent Event;
            public SystemLighting Lights;
            public Color4 OrigFogColor;
            public float OrigFogStart;
            public float OrigFogEnd;
            public float OrigFogDensity;
            private double time;
            public override bool Run(double delta)
            {
                time += delta;
                var t = Event.GetT((float) time);
                if ((Event.SetFlags & AnimVars.FogColor) == AnimVars.FogColor)
                {
                    Lights.FogColor = new Color4(
                        MathHelper.Lerp(OrigFogColor.R, Event.FogColor.X, t),
                        MathHelper.Lerp(OrigFogColor.G, Event.FogColor.Y, t),
                        MathHelper.Lerp(OrigFogColor.B, Event.FogColor.Z, t),
                        1
                    );
                }
                if ((Event.SetFlags & AnimVars.FogDensity) == AnimVars.FogDensity)
                    Lights.FogDensity = MathHelper.Lerp(OrigFogDensity, Event.FogDensity, t);
                if ((Event.SetFlags & AnimVars.FogStart) == AnimVars.FogStart)
                    Lights.FogRange.X = MathHelper.Lerp(OrigFogStart, Event.FogStart, t);
                if ((Event.SetFlags & AnimVars.FogEnd) == AnimVars.FogEnd)
                    Lights.FogRange.Y = MathHelper.Lerp(OrigFogEnd, Event.FogEnd, t);
                return time < Event.Duration;
            }
        }
    }
}