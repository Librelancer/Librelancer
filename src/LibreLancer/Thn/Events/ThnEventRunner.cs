// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
namespace LibreLancer
{
    public interface IThnEventRunner
    {
        void Process(ThnEvent ev, Cutscene cs);
    }
    public class ThnEventRunnerAttribute : Attribute
    {
        public EventTypes Event;
        public ThnEventRunnerAttribute(EventTypes ev)
        {
            Event = ev;
        }
    }
}
