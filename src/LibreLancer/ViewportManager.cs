using System;
using System.Collections.Generic;
using OpenTK.Graphics.OpenGL;
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
		public ViewportManager ()
		{
			_vpm = this;
		}
		public void Push(int x, int y, int width, int height)
		{
			var vp = new Viewport (x, y, width, height);
			viewports.Push (vp);
			GL.Viewport (x, y, width, height);
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
			GL.Viewport (vp.X, vp.Y, vp.Width, vp.Height);
		}
	}
}

