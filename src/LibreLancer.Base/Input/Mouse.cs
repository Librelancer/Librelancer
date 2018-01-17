/* The contents of this file are subject to the Mozilla Public License
 * Version 1.1 (the "License"); you may not use this file except in
 * compliance with the License. You may obtain a copy of the License at
 * http://www.mozilla.org/MPL/
 * 
 * Software distributed under the License is distributed on an "AS IS"
 * basis, WITHOUT WARRANTY OF ANY KIND, either express or implied. See the
 * License for the specific language governing rights and limitations
 * under the License.
 * 
 * 
 * The Initial Developer of the Original Code is Callum McGing (mailto:callum.mcging@gmail.com).
 * Portions created by the Initial Developer are Copyright (C) 2013-2016
 * the Initial Developer. All Rights Reserved.
 */
using System;

namespace LibreLancer
{
	public delegate void MouseWheelEventHandler(int amount);
	public delegate void MouseEventHandler(MouseEventArgs e);
	public class Mouse
	{
		public int X { get; internal set; }
		public int Y { get; internal set; }
		public MouseButtons Buttons { get; internal set; }

		public event MouseWheelEventHandler MouseWheel;
		public event MouseEventHandler MouseMove;
		public event MouseEventHandler MouseDown;
		public event MouseEventHandler MouseUp;

		public int MouseDelta = 0;

		internal Mouse ()
		{
		}

		public bool IsButtonDown(MouseButtons b)
		{
			return (Buttons & b) == b;
		}
		internal void OnMouseMove()
		{
			if (MouseMove != null)
				MouseMove (new MouseEventArgs (X, Y, Buttons));
		}

		internal void OnMouseDown(MouseButtons b)
		{
			if (MouseDown != null)
				MouseDown (new MouseEventArgs (X, Y, b));
		}

		internal void OnMouseUp (MouseButtons b)
		{
			if (MouseUp != null)
				MouseUp (new MouseEventArgs (X, Y, b));
		}

		internal void OnMouseWheel(int amount)
		{
			MouseDelta += amount;
			if (MouseWheel != null)
				MouseWheel (amount);
		}
	}
}

