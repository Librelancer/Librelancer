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
			"Flat",
			"Normals"
		};
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
			if (drawable is CmpFile)
			{
				var cmp = (CmpFile)drawable;
				foreach (var part in cmp.Parts) DetectResourcesModel(part.Value.Model, Name + ", " + part.Value.Model.Path, missing, matrefs, texrefs);
			}
			if (drawable is ModelFile)
			{
				DetectResourcesModel((ModelFile)drawable, Name, missing, matrefs, texrefs);
			}
			if (drawable is SphFile)
			{
				var sph = (SphFile)drawable;
				for (int i = 0; i < sph.SideMaterials.Length; i++)
				{
					if (sph.SideMaterials[i] == null)
					{
						var str = "Material: " + sph.SideMaterialNames[i];
						if (!HasMissing(missing, str)) missing.Add(new MissingReference(str, string.Format("{0} M{1}", Name, i)));
					}
					else
					{
						var crc = CrcTool.FLModelCrc(sph.SideMaterialNames[i]);
						if (!matrefs.Contains(crc)) matrefs.Add(crc);
						DoMaterialRefs(sph.SideMaterials[i], missing, texrefs, string.Format(" - {0} M{1}", Name, i));
					}
				}
			}
		}
		void DetectResourcesModel(ModelFile mdl, string mdlname, List<MissingReference> missing, List<uint> matrefs, List<string> texrefs)
		{
			var lvl = mdl.Levels[0];
			for (int i = lvl.StartMesh; i < (lvl.StartMesh + lvl.MeshCount); i++)
			{
				if (lvl.Mesh.Meshes[i].Material == null)
				{
					var str = "Material: 0x" + lvl.Mesh.Meshes[i].MaterialCrc.ToString("X");
					if (!HasMissing(missing, str)) missing.Add(new MissingReference(str, string.Format("{0}, VMesh(0x{1:X}) #{2}", mdlname, lvl.MeshCrc, i)));
				}
				else
				{
					if (!matrefs.Contains(lvl.Mesh.Meshes[i].MaterialCrc)) matrefs.Add(lvl.Mesh.Meshes[i].MaterialCrc);
					var m = lvl.Mesh.Meshes[i].Material;
					DoMaterialRefs(m, missing, texrefs, string.Format(" - {0} VMesh(0x{1:X}) #{2}", mdlname, lvl.MeshCrc, i));
				}
			}
		}

		void DoMaterialRefs(Material m, List<MissingReference> missing, List<string> texrefs, string refstr)
		{
			RefTex(m.DtName, missing, texrefs, m.Name, refstr);
			if (m.Render is NomadMaterial)
			{
				var nt = m.NtName ?? "NomadRGB_NomadAlpha1";
				RefTex(nt, missing, texrefs, m.Name, refstr);
			}
			RefTex(m.EtName, missing, texrefs, m.Name, refstr);
			RefTex(m.DmName, missing, texrefs, m.Name, refstr);
			RefTex(m.Dm1Name, missing, texrefs, m.Name, refstr);
		}

		void RefTex(string tex, List<MissingReference> missing, List<string> texrefs, string mName, string refstr)
		{
			if (tex != null)
			{
				if (!HasTexture(texrefs, tex)) texrefs.Add(tex);
				if (res.FindTexture(tex) == null)
				{
					var str = "Texture: " + tex;
					if (!HasMissing(missing, str)) missing.Add(new MissingReference(str, mName + refstr));
				}
			}
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
			if (viewMode == 1)
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
			if (viewMode == 1)
			{
				rstate.Wireframe = false;
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
			if (viewMode == 1 || viewMode == 2)
			{
				mat = wireframeMaterial3db;
				mat.Update(cam);
			}
			if (viewMode == 3)
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
			if (viewMode == 0)
			{
				drawable.DrawBuffer(buffer, matrix, Lighting.Empty);
			}
			else if (viewMode == 1 || viewMode == 2)
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
