// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

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

