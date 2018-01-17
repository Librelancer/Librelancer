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
using LibreLancer;
using LibreLancer.Fx;
using LibreLancer.Utf.Ale;
using ImGuiNET;
namespace LancerEdit
{
	public class AleViewer : DockTab
	{
		//GL
		RenderTarget2D renderTarget;
		int rid = 0;
		int rw = -1, rh = -1;
		RenderState rstate;
		ViewportManager vps;
		CommandBuffer buffer;
		Billboards billboards;
		PolylineRender polyline;
		PhysicsDebugRenderer debug;
		//Tab
		public string Title;
		string name;
		ParticleLibrary plib;
		int lastEffect = 0;
		int currentEffect = 0;
		bool open = true;

		float sparam = 1;
		string[] effectNames;
		public AleViewer(string title, string name, AleFile ale, MainWindow main)
		{
			plib = new ParticleLibrary(main.Resources, ale);
			effectNames = new string[plib.Effects.Count];
			for (int i = 0; i < effectNames.Length; i++)
				effectNames[i] = string.Format("{0} (0x{1:X})", plib.Effects[i].Name, plib.Effects[i].CRC);
			Title = title;
			this.name = name;
			this.rstate = main.RenderState;
			vps = main.Viewport;
			buffer = main.Commands;
			billboards = main.Billboards;
			polyline = main.Polyline;
			debug = main.DebugRender;
			SetupRender(0);
		}
		ParticleEffectInstance instance;
		void SetupRender(int index)
		{
			instance = new ParticleEffectInstance(plib.Effects[index]);
			instance.Resources = plib.Resources;
		}
		Vector2 rotation = Vector2.Zero;
		float zoom = 200;
		public override bool Draw()
		{
			if (ImGuiExt.BeginDock(Title + "##" + Unique, ref open, 0))
			{
				//Select Fx
				lastEffect = currentEffect;
				ImGui.Text("Effect:");
				ImGui.SameLine();
				ImGui.Combo("##effect", ref currentEffect, effectNames);
				if (currentEffect != lastEffect) SetupRender(currentEffect);
				//Render
				var renderWidth = Math.Max(120, (int)ImGui.GetWindowWidth() - 15);
				var renderHeight = Math.Max(120, (int)ImGui.GetWindowHeight() - 70);
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
				ImGui.ImageButton((IntPtr)rid, new Vector2(renderWidth, renderHeight),
								  Vector2.Zero, Vector2.One,
								  0,
								  Vector4.One, Vector4.One);
				if (ImGui.IsItemHovered(HoveredFlags.Default))
				{
					if (ImGui.IsMouseDragging(0, 1f))
					{
						var delta = (Vector2)ImGui.GetMouseDragDelta(0, 1f);
						rotation -= (delta / 64);
						ImGui.ResetMouseDragDelta(0);
					}
					float wheel = ImGui.GetIO().MouseWheel;
					if (ImGui.GetIO().ShiftPressed)
						zoom -= wheel * 15;
					else
						zoom -= wheel * 45;
				}
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
			rstate.ClearColor = Color4.CornflowerBlue * new Color4(0.3f, 0.3f, 0.3f, 1f);
			rstate.ClearAll();
			vps.Push(0, 0, renderWidth, renderHeight);
			//Draw Model
			var cam = new ChaseCamera(new Viewport(0, 0, renderWidth, renderHeight));
			cam.ChasePosition = Vector3.Zero;
			cam.ChaseOrientation = Matrix4.CreateRotationX(MathHelper.Pi);
			cam.DesiredPositionOffset = new Vector3(zoom, 0, 0);
			cam.OffsetDirection = Vector3.UnitX;
			cam.Reset();
			cam.Update(TimeSpan.FromSeconds(500));
			buffer.StartFrame();
			polyline.SetCamera(cam);
			billboards.Begin(cam, buffer);
			debug.StartFrame(cam, rstate);
			instance.Draw(polyline, billboards, debug, transform, sparam);
			polyline.FrameEnd();
			billboards.End();
			buffer.DrawOpaque(rstate);
			rstate.DepthWrite = false;
			buffer.DrawTransparent(rstate);
			rstate.DepthWrite = true;
			debug.Render();
			//Restore state
			rstate.Cull = false;
			rstate.BlendMode = BlendMode.Normal;
			rstate.DepthEnabled = false;
			rstate.ClearColor = cc;
			RenderTarget2D.ClearBinding();
			vps.Pop();
		}

		public override void DetectResources(List<MissingReference> missing, List<uint> matrefs, List<string> texrefs)
		{
			foreach (var node in instance.Effect.Nodes)
			{
				if (node is FxBasicAppearance)
				{
					var fx = (FxBasicAppearance)node;
					if (fx.Texture != null && !ResourceDetection.HasTexture(texrefs, fx.Texture)) texrefs.Add(fx.Texture);
					if (fx.Texture != null && plib.Resources.FindTexture(fx.Texture) == null)
					{
						var str = "Texture: " + fx.Texture; //TODO: This is wrong - handle properly
						if (!ResourceDetection.HasMissing(missing, str)) missing.Add(new MissingReference(
							str, string.Format("{0}: {1} ({2})", instance.Effect.Name, node.NodeName, node.Name)));
					}
				}
			}
		}

		Matrix4 transform = Matrix4.Identity;
		public override void Update(double elapsed)
		{
			transform = Matrix4.CreateRotationX(rotation.Y) * Matrix4.CreateRotationY(rotation.X);
			instance.Update(TimeSpan.FromSeconds(elapsed), transform, sparam);
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
