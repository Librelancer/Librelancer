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
using LibreLancer.Vertices;
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
		Lighting lighting;
		IDrawable drawable;
		RenderState rstate;
		CommandBuffer buffer;
		ViewportManager vps;
		ResourceManager res;
		public string Name;
		int viewMode = 0;
		static readonly string[] viewModes = new string[] {
			"Textured",
			"Lit",
			"Flat",
			"Normals",
            "None"
		};
        bool doWireframe = false;
		const int M_TEXTURED = 0;
		const int M_LIT = 1;
		const int M_FLAT = 2;
		const int M_NORMALS = 3;
        const int M_NONE = 4;

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
			lighting = Lighting.Create ();
			lighting.Enabled = true;
			lighting.Ambient = Color4.Black;
			lighting.Lights.Add (new RenderLight () {
				Kind = LightKind.Directional,
				Direction = new Vector3(0,-1,0),
				Color = Color4.White
			});
			lighting.Lights.Add (new RenderLight () {
				Kind = LightKind.Directional,
				Direction = new Vector3(0,0,1),
				Color = Color4.White
			});
			lighting.NumberOfTilesX = -1;
            GizmoRender.Init(res);
		}

		Vector2 rotation = Vector2.Zero;
		public override bool Draw()
		{
			if (ImGuiExt.BeginDock(Title + "###" + Unique, ref open, 0))
			{
				ImGui.Text("View Mode:");
				ImGui.SameLine();
				ImGui.Combo("##modes", ref viewMode, viewModes);
                ImGui.SameLine();
                ImGui.Checkbox("Wireframe", ref doWireframe);
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
        Random rand = new Random();
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
            cam.UpdateFrameNumber(rand.Next()); //Stop bad matrix caching
			drawable.Update(cam, TimeSpan.Zero, TimeSpan.Zero);
            if (viewMode != M_NONE)
            {
                buffer.StartFrame(rstate);
                if (drawable is CmpFile)
                    DrawCmp(cam, false);
                else
                    DrawSimple(cam, false);
                buffer.DrawOpaque(rstate);
                rstate.DepthWrite = false;
                buffer.DrawTransparent(rstate);
                rstate.DepthWrite = true;
            }
            if (doWireframe)
            {
                buffer.StartFrame(rstate);
                GL.PolygonOffset(1, 1);
                rstate.Wireframe = true;
                if (drawable is CmpFile)
                    DrawCmp(cam, true);
                else
                    DrawSimple(cam, false);
                GL.PolygonOffset(0, 0);
                buffer.DrawOpaque(rstate);
                rstate.Wireframe = false;
            }
            //Draw hardpoints
            DrawHardpoints(cam);
			//Restore state
            rstate.Cull = false;
			rstate.BlendMode = BlendMode.Normal;
			rstate.DepthEnabled = false;
			rstate.ClearColor = cc;
			RenderTarget2D.ClearBinding();
			vps.Pop();
		}

        void DrawHardpoints(ICamera cam)
        {
            var matrix = Matrix4.CreateRotationX(rotation.Y) * Matrix4.CreateRotationY(rotation.X);
            List<Matrix4> hardpoints = new List<Matrix4>();
            if (drawable is CmpFile)
            {
                foreach (var p in ((CmpFile)drawable).Parts) {
                    var parentHp = p.Value.Construct != null ? p.Value.Construct.Transform : Matrix4.Identity;
                    parentHp *= matrix;
                    foreach(var hp in p.Value.Model.Hardpoints) {
                        hardpoints.Add(hp.Transform * parentHp);
                    }
                }
            } 
            else if (drawable is ModelFile)
            {
                foreach(var hp in ((ModelFile)drawable).Hardpoints) {
                    hardpoints.Add(hp.Transform * matrix);
                }
            }
            if (hardpoints.Count == 0) return;
            GizmoRender.Begin();
            foreach(var tr in hardpoints)
            {
                //X
                GizmoRender.AddGizmo(tr);
            }
            GizmoRender.RenderGizmos(cam, rstate);
        }

        void DrawSimple(ICamera cam, bool wireFrame)
		{
			Material mat = null;
			if (wireFrame || viewMode == M_FLAT)
			{
				mat = wireframeMaterial3db;
				mat.Update(cam);
			}
			else if (viewMode == M_NORMALS)
			{
				mat = normalsDebugMaterial;
				mat.Update(cam);
			}
			var matrix = Matrix4.CreateRotationX(rotation.Y) * Matrix4.CreateRotationY(rotation.X);
            if(viewMode == M_LIT)
			    drawable.DrawBuffer(buffer, matrix, ref lighting, mat);
            else
                drawable.DrawBuffer(buffer, matrix, ref Lighting.Empty, mat);
		}

		int jColors = 0;
        void DrawCmp(ICamera cam, bool wireFrame)
		{
			var matrix = Matrix4.CreateRotationX(rotation.Y) * Matrix4.CreateRotationY(rotation.X);
            if (wireFrame || viewMode == M_FLAT)
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
                    part.Value.DrawBuffer(buffer, matrix, ref Lighting.Empty, mat);
                }
            }
			else if (viewMode == M_TEXTURED || viewMode == M_LIT)
			{
                if(viewMode == M_LIT)
				    drawable.DrawBuffer(buffer, matrix, ref lighting);
                else
                    drawable.DrawBuffer(buffer, matrix, ref Lighting.Empty);
			}
			else
			{
				normalsDebugMaterial.Update(cam);
				drawable.DrawBuffer(buffer, matrix, ref Lighting.Empty, normalsDebugMaterial);
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
