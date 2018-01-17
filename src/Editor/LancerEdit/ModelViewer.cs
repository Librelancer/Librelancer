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
using LibreLancer.Utf.Cmp;
using LibreLancer.Utf.Mat;
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
		ResourceManager res;
		public string Name;
		int viewMode = 0;
		static readonly string[] viewModes = new string[] {
			"Textured",
			"Wireframe",
			"Textured+Wireframe",
			"Flat",
			"Normals"
		};
		const int M_TEXTURED = 0;
		const int M_WIREFRAME = 1;
		const int M_TEXTURE_WIREFRAME = 2;
		const int M_FLAT = 3;
		const int M_NORMALS = 4;

		static readonly Color4[] initialCmpColors = new Color4[] {
			Color4.White,
			Color4.Red,
			Color4.LightGreen,
			Color4.Blue,
			Color4.Yellow,
			Color4.Magenta,
			Color4.DarkGreen,
			Color4.Cyan,
			Color4.Orange
		};
		Material wireframeMaterial3db;
		Material normalsDebugMaterial;
		Dictionary<int, Material> partMaterials = new Dictionary<int, Material>();

		public ModelViewer(string title, string name,IDrawable drawable, RenderState rstate, ViewportManager viewports, CommandBuffer commands, ResourceManager res)
		{
			Title = title;
			Name = name;
			this.drawable = drawable;
			this.rstate = rstate;
			this.vps = viewports;
			this.res = res;
			buffer = commands;
			wireframeMaterial3db = new Material(res);
			wireframeMaterial3db.Dc = Color4.White;
			wireframeMaterial3db.DtName = ResourceManager.WhiteTextureName;
			normalsDebugMaterial = new Material(res);
			normalsDebugMaterial.Type = "NormalDebugMaterial";
		}

		Vector2 rotation = Vector2.Zero;
		public override bool Draw()
		{
			if (ImGuiExt.BeginDock(Title + "##" + Unique, ref open, 0))
			{
				ImGui.Text("View Mode:");
				ImGui.SameLine();
				ImGui.Combo("##modes", ref viewMode, viewModes);
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
				//Draw Image
				//ImGui.Image((IntPtr)rid, new Vector2(renderWidth, renderHeight), Vector2.Zero, Vector2.One, Vector4.One, Vector4.One);
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
				}
			}
			ImGuiExt.EndDock();
			return open;
		}

		public override void DetectResources(List<MissingReference> missing, List<uint> matrefs, List<string> texrefs)
		{
			ResourceDetection.DetectDrawable(Name, drawable, res, missing, matrefs, texrefs);
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
			cam.DesiredPositionOffset = new Vector3(drawable.GetRadius() * 2, 0, 0);
			cam.OffsetDirection = Vector3.UnitX;
			cam.Reset();
			cam.Update(TimeSpan.FromSeconds(500));
			buffer.StartFrame();
			drawable.Update(cam, TimeSpan.Zero, TimeSpan.Zero);
			drawable.Update(cam, TimeSpan.FromSeconds(0), TimeSpan.FromSeconds(0));
			bool is2 = false;
			bool render = true;
			if (viewMode == M_TEXTURE_WIREFRAME)
			{
				is2 = true;
				viewMode = M_TEXTURED;
			}
			while (render)
			{
				if (viewMode == M_WIREFRAME)
				{
					rstate.Wireframe = true;
				}
				if (drawable is CmpFile)
				{
					DrawCmp(cam);
				}
				else
				{
					DrawSimple(cam);
				}
				buffer.DrawOpaque(rstate);
				rstate.DepthWrite = false;
				buffer.DrawTransparent(rstate);
				rstate.DepthWrite = true;
				if (viewMode == M_WIREFRAME)
				{
					rstate.Wireframe = false;
				}
				render = false;
				if (is2 && viewMode == M_TEXTURED)
				{
					viewMode = M_WIREFRAME;
					render = true;
					GL.PolygonOffset(1, 1);
				}
			}
			if (is2)
			{
				GL.PolygonOffset(0f, 0f);
				viewMode = M_TEXTURE_WIREFRAME;
			}
			//Restore state
			rstate.Cull = false;
			rstate.BlendMode = BlendMode.Normal;
			rstate.DepthEnabled = false;
			rstate.ClearColor = cc;
			RenderTarget2D.ClearBinding();
			vps.Pop();
		}

		void DrawSimple(ICamera cam)
		{
			Material mat = null;
			if (viewMode == M_WIREFRAME || viewMode == M_FLAT)
			{
				mat = wireframeMaterial3db;
				mat.Update(cam);
			}
			if (viewMode == M_NORMALS)
			{
				mat = normalsDebugMaterial;
				mat.Update(cam);
			}
			var matrix = Matrix4.CreateRotationX(rotation.Y) * Matrix4.CreateRotationY(rotation.X);
			drawable.DrawBuffer(buffer, matrix, Lighting.Empty, mat);
		}

		int jColors = 0;
		void DrawCmp(ICamera cam)
		{
			var matrix = Matrix4.CreateRotationX(rotation.Y) * Matrix4.CreateRotationY(rotation.X);
			if (viewMode == M_TEXTURED)
			{
				drawable.DrawBuffer(buffer, matrix, Lighting.Empty);
			}
			else if (viewMode == M_WIREFRAME || viewMode == M_FLAT)
			{
				var cmp = (CmpFile)drawable;
				foreach (var part in cmp.Parts)
				{
					Material mat;
					if (!partMaterials.TryGetValue(part.Key, out mat))
					{
						mat = new Material(res);
						mat.DtName = ResourceManager.WhiteTextureName;
						mat.Dc = initialCmpColors[jColors++];
						if (jColors >= initialCmpColors.Length) jColors = 0;
						partMaterials.Add(part.Key, mat);
					}
					mat.Update(cam);
					part.Value.DrawBuffer(buffer, matrix, Lighting.Empty, mat);
				}
			}
			else
			{
				normalsDebugMaterial.Update(cam);
				drawable.DrawBuffer(buffer, matrix, Lighting.Empty, normalsDebugMaterial);
			}
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
