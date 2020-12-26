// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Numerics;
using LibreLancer.Thorn;

namespace LibreLancer.Thn
{
    public class StartLightPropAnimEvent : ThnEvent
    {
        [Flags]
        public enum AnimVars
        {
            Nothing = 0,
            On = 1 << 1,
            Diffuse = 1 << 2,
            Ambient = 1 << 3
        }

        public AnimVars SetFlags;
        public bool On;
        public Color3f Diffuse;
        public Color3f Ambient;
        public StartLightPropAnimEvent() { }

        public StartLightPropAnimEvent(LuaTable table) : base(table)
        {
            //Get Tables
            if (!GetProps(table, out var props)) return;
            if (!GetValue(props, "lightprops", out LuaTable lights)) return;
            //Set Properties
            if (GetValue(lights, "on", out On)) SetFlags |= AnimVars.On;
            if (GetValue(lights, "diffuse", out Diffuse)) SetFlags |= AnimVars.Diffuse;
            if (GetValue(lights, "ambient", out Ambient)) SetFlags |= AnimVars.Ambient;
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
                return time < Event.Duration;
            }
        }
    }
}