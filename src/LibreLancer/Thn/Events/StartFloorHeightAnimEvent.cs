// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using LibreLancer.Thorn;

namespace LibreLancer.Thn
{
    public class StartFloorHeightAnimEvent : ThnEvent
    {
        public StartFloorHeightAnimEvent() {}

        public StartFloorHeightAnimEvent(LuaTable table) : base(table)
        {
        }
    }
}