// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using LibreLancer.Thorn;

namespace LibreLancer.Thn.Events
{
    public class ConnectHardpointsEvent : ThnEvent
    {
        public ConnectHardpointsEvent() { }

        public ConnectHardpointsEvent(LuaTable table) : base(table) { }
    }
}