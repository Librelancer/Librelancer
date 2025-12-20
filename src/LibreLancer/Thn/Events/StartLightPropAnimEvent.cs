// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using LibreLancer.Data.GameData;
using LibreLancer.Render;
using LibreLancer.Thorn;

namespace LibreLancer.Thn.Events
{
    public class StartLightPropAnimEvent : ThnEvent
    {
        [Flags]
        public enum AnimVars
        {
            Nothing = 0,
            On = 1 << 1,
            Diffuse = 1 << 2,
            Ambient = 1 << 3,
            Cutoff = 1 << 4,
            Theta = 1 << 5,
            Range = 1 << 6
        }

        public AnimVars SetFlags;
        public bool On;
        public Color3f Diffuse;
        public Color3f Ambient;
        public float Theta;
        public float Cutoff;
        public float Range;
        public StartLightPropAnimEvent() { }

        public StartLightPropAnimEvent(ThornTable table) : base(table)
        {
            //Get Tables
            if (!GetProps(table, out var props)) return;
            if (!GetValue(props, "lightprops", out ThornTable lights)) return;
            //Set Properties
            if (GetValue(lights, "on", out On)) SetFlags |= AnimVars.On;
            if (GetValue(lights, "diffuse", out Diffuse)) SetFlags |= AnimVars.Diffuse;
            if (GetValue(lights, "ambient", out Ambient)) SetFlags |= AnimVars.Ambient;
            if (GetValue(lights, "theta", out Theta)) SetFlags |= AnimVars.Theta;
            if(GetValue(lights, "cutoff", out Cutoff)) SetFlags |= AnimVars.Cutoff;
            if (GetValue(lights, "range", out Range)) SetFlags |= AnimVars.Range;
        }

        public override void Run(ThnScriptInstance instance)
        {
            if (!instance.Objects.TryGetValue(Targets[0], out var obj))
            {
                FLLog.Error("Thn", $"Entity {Targets[0]} does not exist");
                return;
            }
            if (obj.Light == null)
            {
                FLLog.Error("Thn", $"Entity {Targets[0]} is not a light");
                return;
            }
            if ((SetFlags & AnimVars.On) == AnimVars.On) obj.Light.Active = On;
            if (Duration > 0)
            {
                instance.AddProcessor(new LightPropAnim()
                {
                    Orig = obj.Light.Light,
                    Dst = obj.Light,
                    Event = this
                });
            }
            else
            {
                if ((SetFlags & AnimVars.Diffuse) == AnimVars.Diffuse) obj.Light.Light.Color = Diffuse;
                if ((SetFlags & AnimVars.Ambient) == AnimVars.Ambient) obj.Light.Light.Ambient = Ambient;
                if ((SetFlags & AnimVars.Theta) == AnimVars.Theta) obj.Light.Light.Theta = Theta;
                if ((SetFlags & AnimVars.Cutoff) == AnimVars.Cutoff) obj.Light.Light.Phi = Cutoff;
                if ((SetFlags & AnimVars.Range) == AnimVars.Range) obj.Light.Light.Range = Range;
            }
        }

        class LightPropAnim : ThnEventProcessor
        {
            public RenderLight Orig;
            public DynamicLight Dst;
            public StartLightPropAnimEvent Event;
            private double time;
            public override bool Run(double delta)
            {
                time += delta;
                var t = Event.GetT((float) time);
                if ((Event.SetFlags & AnimVars.Diffuse) == AnimVars.Diffuse)
                {
                    Dst.Light.Color = Easing.EaseColorRGB(EasingTypes.Linear, t, 0, 1, Orig.Color, Event.Diffuse);
                }
                if ((Event.SetFlags & AnimVars.Ambient) == AnimVars.Ambient)
                {
                    Dst.Light.Ambient = Easing.EaseColorRGB(EasingTypes.Linear, t, 0, 1, Orig.Ambient, Event.Ambient);
                }
                if ((Event.SetFlags & AnimVars.Theta) == AnimVars.Theta)
                {
                    Dst.Light.Theta= MathHelper.Lerp(Orig.Theta, Event.Theta, t);
                }
                if ((Event.SetFlags & AnimVars.Cutoff) == AnimVars.Cutoff)
                {
                    Dst.Light.Phi = MathHelper.Lerp(Orig.Phi, Event.Cutoff, t);
                }
                if ((Event.SetFlags & AnimVars.Range) == AnimVars.Range)
                {
                    Dst.Light.Range = MathHelper.Lerp(Orig.Range, Event.Range, t);
                }
                return time < Event.Duration;
            }
        }
    }
}
