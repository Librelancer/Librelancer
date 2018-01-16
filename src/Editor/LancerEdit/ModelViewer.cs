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
using LibreLancer;
using ImGuiNET;
namespace LancerEdit
{
	public class ModelViewer : DockTab
	{
		RenderTarget2D renderTarget;
		int rw = -1, rh = -1;
		int rid = 0;
		bool open = true;
		public string Title;
		IDrawable drawable;
		RenderState rstate;
		CommandBuffer buffer;
		ViewportManager vps;
		public ModelViewer(string title, IDrawable drawable, RenderState rstate, ViewportManager viewports, CommandBuffer commands)
		{
			Title = title;
			this.drawable = drawable;
			this.rstate = rstate;
			this.vps = viewports;
			buffer = commands;
		}

		public override bool Draw()
		{
			if (ImGuiExt.BeginDock(Title + "##" + Unique, ref open, 0))
			{
				var renderWidth = Math.Max(120, (int)ImGui.GetWindowWidth() - 5);
				var renderHeight = Math.Max(120, (int)ImGui.GetWindowHeight() - 40);
				//Generate render target
				if (rh != renderHeight || rw != renderWidth)
				{
					if (renderTarget != null)
					{
						ImGuiHelper.DeregisterTexture(renderTarget);
						renderTarget.Dispose();
					}
					renderTarget = new RenderTarget2D(renderWidth, renderHeight);
					rid = ImGuiHelper.RegisterTexture(renderTarget);
					rw = renderWidth;
					rh = renderHeight;
				}
				DrawGL(renderWidth, renderHeight);
				//Draw Image
				ImGui.Image((IntPtr)rid, new Vector2(renderWidth, renderHeight), Vector2.Zero, Vector2.One, Vector4.One, Vector4.One);
			}
			ImGuiExt.EndDock();
			return open;
		}

		void DrawGL(int renderWidth, int renderHeight)
		{
			//Set state
			renderTarget.BindFramebuffer();
			rstate.Cull = true;
			var cc = rstate.ClearColor;
			rstate.DepthEnabled = true;
			rstate.ClearColor = Color4.CornflowerBlue;
			rstate.ClearAll();
			vps.Push(0, 0, renderWidth, renderHeight);
			//Draw Model
			var cam = new ChaseCamera(new Viewport(0, 0, renderWidth, renderHeight));
			cam.ChasePosition = Vector3.Zero;
			cam.ChaseOrientation = Matrix4.CreateRotationX(MathHelper.Pi);
			cam.DesiredPositionOffset = new Vector3(drawable.GetRadius() * 2, 0, 0);
			cam.OffsetDirection = Vector3.UnitX;
			cam.Reset();
			cam.Update(TimeSpan.FromSeconds(500));
			buffer.StartFrame();
			drawable.Update(cam, TimeSpan.Zero, TimeSpan.Zero);
			drawable.Update(cam, TimeSpan.FromSeconds(0), TimeSpan.FromSeconds(0));
			drawable.DrawBuffer(buffer, Matrix4.Identity, Lighting.Empty);
			buffer.DrawOpaque(rstate);
			rstate.DepthWrite = false;
			buffer.DrawTransparent(rstate);
			rstate.DepthWrite = true;
			//Restore state
			rstate.Cull = false;
			rstate.BlendMode = BlendMode.Normal;
			rstate.DepthEnabled = false;
			rstate.ClearColor = cc;
			RenderTarget2D.ClearBinding();
			vps.Pop();
		}
		public override void Dispose()
		{
			if (renderTarget != null)
			{
				ImGuiHelper.DeregisterTexture(renderTarget);
				renderTarget.Dispose();
			}
		}
	}
}
