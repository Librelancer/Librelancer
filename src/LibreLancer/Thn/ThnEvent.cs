// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using LibreLancer.Thorn;
namespace LibreLancer
{
	public class ThnEvent
	{
		public float Duration;
		public double EventTime;
        public double TimeOffset;
        public double Time => EventTime + TimeOffset;

        public SoundFlags Flags;
		public EventTypes Type;
		public LuaTable Targets;
		public LuaTable Properties;
		public ParameterCurve ParamCurve;
		public override string ToString()
		{
			return string.Format("[{0}: {1}]", EventTime, Type);
		}
	}
}

