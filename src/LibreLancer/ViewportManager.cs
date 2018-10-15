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
 * Portions created by the Initial Developer are Copyright (C) 2013-2018
 * the Initial Developer. All Rights Reserved.
 */
using System;
using System.Collections.Generic;
namespace LibreLancer
{
	public class ViewportManager
	{
		public static ViewportManager Instance {
			get {
				return _vpm;
			}
		}
		static ViewportManager _vpm;
		Stack<Viewport> viewports = new Stack<Viewport>();
		RenderState render;
		public ViewportManager (RenderState rs)
		{
			_vpm = this;
			render = rs;
		}
		public void Push(int x, int y, int width, int height)
		{
			var vp = new Viewport (x, y, width, height);
			viewports.Push (vp);
			render.SetViewport (x, y, width, height);
		}
		public void Replace(int x, int y, int width, int height)
		{
			viewports.Pop ();
			Push (x, y, width, height);
		}
		public void CheckViewports()
		{
			if (viewports.Count != 1)
				throw new Exception ("viewports.Count != 1 at end of frame");
		}
		public void Pop()
		{
			viewports.Pop ();
			var vp = viewports.Peek ();
			render.SetViewport (vp.X, vp.Y, vp.Width, vp.Height);
		}
	}
}

