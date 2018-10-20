// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and confiditons defined in
// LICENSE, which is part of this source code package

using System;
using LibreLancer.Thorn;
namespace LibreLancer
{
	public class ThnEvent
	{
		public float Duration;
		public double Time;
		public EventFlags Flags;
		public EventTypes Type;
		public LuaTable Targets;
		public LuaTable Properties;
		public ParameterCurve ParamCurve;
		public override string ToString()
		{
			return string.Format("[{0}: {1}]", Time, Type);
		}
	}
}

