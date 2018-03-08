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
			selectedReference = null;
			instance = new ParticleEffectInstance(plib.Effects[index]);
			instance.Resources = plib.Resources;
		}
		Vector2 rotation = Vector2.Zero;
		float zoom = 200;
		public override bool Draw()
		{
			if (ImGuiExt.BeginDock(Title + "###" + Unique, ref open, 0))
			{
				//Fx management
				lastEffect = currentEffect;
				ImGui.Text("Effect:");
				ImGui.SameLine();
				ImGui.Combo("##effect", ref currentEffect, effectNames);
				if (currentEffect != lastEffect) SetupRender(currentEffect);
				ImGui.SameLine();
				ImGui.Button("+");
				ImGui.SameLine();
				ImGui.Button("-");
				ImGui.Separator();
				//Layout
				ImGui.Columns(2, "##alecolumns", true);
				ImGui.Text("Viewport");
				ImGui.NextColumn();
				ImGui.Text("Hierachy");
				ImGui.Separator();
				ImGui.NextColumn();
				ImGui.BeginChild("##renderchild");
				//Viewport Rendering
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
				//Display + Camera controls
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
				//Action Bar
				if (ImGui.Button("Actions"))
					ImGui.OpenPopup("actions");
				if (ImGui.BeginPopup("actions"))
				{
					if (ImGui.MenuItem("Open Node Library"))
					{
					}
					ImGui.EndPopup();
				}
				ImGui.SameLine();
				if (ImGui.Button("Reset"))
				{
					instance.Reset();
				}
				ImGui.SameLine();
				ImGui.Text(string.Format("T: {0:0.000}", instance.GlobalTime));
				//Node Hierachy Tab
				ImGui.EndChild();
				ImGui.NextColumn();
				ImGui.BeginChild("##nodesdisplay", false);
				if (selectedReference != null)
				{
					NodeOptions();
					ImGui.Separator();
				}
				ImGui.BeginChild("##nodescroll", false);
				NodeHierachy();
				ImGui.EndChild();
				ImGui.EndChild();
			}
			ImGuiExt.EndDock();
			return open;
		}

		NodeReference selectedReference = null;
		void NodeHierachy()
		{
			var enabledColor = (Vector4)ImGui.GetStyle().GetColor(ColorTarget.Text);
			var disabledColor = (Vector4)ImGui.GetStyle().GetColor(ColorTarget.TextDisabled);

			foreach (var reference in instance.Effect.References)
			{
				int j = 0;
				if (reference.Parent == null)
					DoNode(reference, j++, enabledColor, disabledColor);
			}
		}

		void NodeOptions()
		{
			ImGui.Text(string.Format("Selected Node: {0} ({1})", selectedReference.Node.NodeName, selectedReference.Node.Name));
			//Node enabled
			var enabled = instance.NodeEnabled(selectedReference);
			var wasEnabled = enabled;
			ImGui.Checkbox("Enabled", ref enabled);
			if (enabled != wasEnabled) instance.EnableStates[selectedReference] = enabled;
			//Normals?

			//Textures?

			//Debug volumes?
		}

		void DoNode(NodeReference reference, int idx, Vector4 enabled, Vector4 disabled)
		{
			var col = instance.NodeEnabled(reference) ? enabled : disabled;
			string label = null;
			if (reference.IsAttachmentNode)
				label = string.Format("Attachment##{0}", idx);
			else
				label = string.Format("{0} ({1})##{2}", reference.Node.NodeName, reference.Node.Name, idx);
			ImGui.PushStyleColor(ColorTarget.Text, col);
			if (reference.Children.Count > 0)
			{
				if (ImGui.TreeNodeEx(label, TreeNodeFlags.OpenOnDoubleClick | TreeNodeFlags.OpenOnArrow))
				{
					int j = 0;
					foreach (var child in reference.Children)
						DoNode(child, j++, enabled, disabled);
					ImGui.TreePop();
				}
			}
			else
			{
				ImGui.Bullet();
				ImGui.SameLine();
				if (ImGui.Selectable(label, selectedReference == reference)) {
					selectedReference = reference;
				}
			}
			ImGui.PopStyleColor();
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
            var cam = new LookAtCamera();
            cam.Update(renderWidth, renderHeight, new Vector3(zoom, 0, 0), Vector3.Zero);
			buffer.StartFrame(rstate);
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
			foreach (var reference in instance.Effect.References)
			{
				if (reference.Node is FxBasicAppearance)
				{
					var node = reference.Node;
					var fx = (FxBasicAppearance)reference.Node;
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
