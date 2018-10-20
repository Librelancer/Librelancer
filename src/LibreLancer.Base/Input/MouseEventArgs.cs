// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;

namespace LibreLancer
{
	public class MouseEventArgs
	{
		public int X { get; private set; }
		public int Y { get; private set; }
		public MouseButtons Buttons { get; private set; }
		public MouseEventArgs (int x, int y, MouseButtons buttons)
		{
			X = x;
			Y = y;
			Buttons = buttons;
		}
	}
}

