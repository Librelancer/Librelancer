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
using OpenTK;
namespace LibreLancer
{
	public abstract class UIAnimation
	{
		public double Start;
		public double Duration;
		double time;
		public Vector2 CurrentPosition = Vector2.Zero;
		public Vector2? CurrentScale;
		public bool Running = false;

		protected UIAnimation (double start, double time)
		{
		}

		public void Update(double delta)
		{
			time += delta;
			if (time >= (Start + Duration)) {
				Running = false;
				return;
			}
			if (time >= Start)
				Run (time - Start);
		}

		protected abstract void Run (double currentTime);

		public virtual void Begin()
		{
			time = 0;
			Running = true;
		}
	}
}

