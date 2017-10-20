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
 * Portions created by the Initial Developer are Copyright (C) 2013-2017
 * the Initial Developer. All Rights Reserved.
 */
using System;
using Xwt;
using LibreLancer;
namespace LancerEdit
{
	public class GLWidget : Canvas
	{
		public event Action GLDraw;
		AppInstance app;
		public GLWidget(AppInstance app)
		{
			this.app = app;
		}
		RenderTarget2D renderTarget;
		protected override void OnDraw(Xwt.Drawing.Context ctx, Xwt.Rectangle dirtyRect)
		{
			if (GLDraw == null)
				return;
			int w = (int)Bounds.Width, h = (int)Bounds.Height;
			if (renderTarget == null ||
			   renderTarget.Width != w ||
			   renderTarget.Height != h)
			{
				if (renderTarget != null) renderTarget.Dispose();
				renderTarget = new RenderTarget2D(w, h);
			}
			renderTarget.BindFramebuffer();
			app.RenderState.SetViewport(0, 0, w, h);
			GLDraw();
			RenderTarget2D.ClearBinding();
			var data = new byte[w * h * 4];
			renderTarget.GetData(data);
			using (var image = app.MainWindow.GetImage(data, w, h))
			{
				ctx.DrawImage(image, Xwt.Point.Zero);
			}
		}
	}

}
