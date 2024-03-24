// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using LibreLancer.Thorn;

namespace LibreLancer.Thn.Events
{
    public class StartIKEvent : ThnEvent
    {
        public StartIKEvent() { }

        public StartIKEvent(ThornTable table) : base(table) { }
    }
}