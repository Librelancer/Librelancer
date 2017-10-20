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
	public class ModelViewer : Window
	{
		IDrawable drawable;
		AppInstance app;

		GLWidget gl;
		Color4 background = Color4.CornflowerBlue;
		ColorPicker colorButton;
		float modelRadius;
		public ModelViewer(AppInstance app, IDrawable drawable)
		{
			this.app = app;
			this.drawable = drawable;
			modelRadius = drawable.GetRadius();
			drawable.Initialize(app.Resources);
			Title = "Model Viewer";
			Width = 300;
			Height = 200;

			var vbox = new VBox();
			gl = new GLWidget(app);
			gl.GLDraw += Gl_GLDraw;
			vbox.PackStart(gl, true, true);
			var hbox = new HBox();
			colorButton = new ColorPicker();
			colorButton.Color = Xwt.Drawing.Colors.CornflowerBlue;
			colorButton.SupportsAlpha = false;
			colorButton.ColorChanged += ColorButton_ColorChanged;
			hbox.PackStart(new Label("Background: "));
			hbox.PackStart(colorButton);
			vbox.PackStart(hbox);
			this.Content = vbox;
		}

		void ColorButton_ColorChanged(object sender, EventArgs e)
		{
			background = new Color4(
				(float)colorButton.Color.Red,
				(float)colorButton.Color.Green,
				(float)colorButton.Color.Blue,
				1f
			);
		}

		void Gl_GLDraw()
		{
			app.RenderState.ClearColor = background;
			app.RenderState.ClearAll();
			app.CmdBuf.StartFrame();
			var cam = new ChaseCamera(new Viewport(0, 0, (int)gl.Bounds.Width, (int)gl.Bounds.Height));
			cam.ChasePosition = Vector3.Zero;
			cam.ChaseOrientation = Matrix4.CreateRotationX(MathHelper.Pi);
			cam.DesiredPositionOffset = new Vector3(modelRadius * 2, 0, 0);
			cam.OffsetDirection = Vector3.UnitX;

			cam.Reset();
			cam.Update(TimeSpan.FromSeconds(500));
			drawable.Update(cam, TimeSpan.FromSeconds(0), TimeSpan.FromSeconds(0));
			drawable.DrawBuffer(app.CmdBuf, Matrix4.Identity, Lighting.Empty);
			app.RenderState.DepthEnabled = true;
			app.CmdBuf.DrawOpaque(app.RenderState);
			app.RenderState.DepthWrite = false;
			app.CmdBuf.DrawTransparent(app.RenderState);
			app.RenderState.DepthWrite = true;
		}
	}
}
