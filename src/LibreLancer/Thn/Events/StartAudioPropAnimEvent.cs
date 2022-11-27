// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using LibreLancer.Thorn;

namespace LibreLancer.Thn.Events
{
    public class StartAudioPropAnimEvent : ThnEvent
    {
        public StartAudioPropAnimEvent() { }

        public StartAudioPropAnimEvent(LuaTable table) : base(table)
        {
        }
    }
}