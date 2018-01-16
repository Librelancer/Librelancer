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
using System.IO;
using ImGuiNET;
using LibreLancer;
namespace LancerEdit
{
	public class UtfTab : DockTab
	{
		public string Title;
		bool open = true;
		public EditableUtf Utf;
		LUtfNode selectedNode = null;
		MainWindow main;
		public UtfTab(MainWindow main, EditableUtf utf, string title)
		{
			this.main = main;
			Utf = utf;
			Title = title;
			text = new TextBuffer();
		}
		MemoryEditor mem;
		byte[] hexdata;
		bool hexEditor = false;
		TreeNodeFlags tflags = TreeNodeFlags.OpenOnArrow | TreeNodeFlags.OpenOnDoubleClick;
		TextBuffer text;

		public override void Dispose()
		{
			text.Dispose();
		}

		bool HasChild(LUtfNode node, string name)
		{
			if (node.Children == null) return false;
			foreach (var child in node.Children)
				if (child.Name == name) return true;
			return false;
		}

		public override bool Draw()
		{
			if (ImGuiExt.BeginDock(Title + "##" + Unique, ref open, 0))
			{
				//Layout
				if (selectedNode != null)
				{
					ImGui.Columns(2, "NodeColumns", true);
				}
				//Headers
				ImGui.Separator();
				ImGui.Text("Nodes");
				if (selectedNode != null)
				{
					ImGui.NextColumn();
					ImGui.Text("Node Information");
					ImGui.NextColumn();
				}
				ImGui.Separator();
				//Tree
				ImGui.BeginChild("##scroll", false, 0);
				var flags = selectedNode == Utf.Root ? TreeNodeFlags.Selected | tflags : tflags;
				var isOpen = ImGui.TreeNodeEx("/", flags);
				if (ImGuiNative.igIsItemClicked(0))
				{
					selectedNode = Utf.Root;
				}
				ImGui.PushID("/");
				DoNodeMenu("/", Utf.Root, null);
				ImGui.PopID();
				if (isOpen)
				{
					int i = 0;
					foreach (var node in Utf.Root.Children)
					{
						DoNode(node, Utf.Root, i++);
					}
					ImGui.TreePop();
				}
				ImGui.EndChild();
				//End Tree
				if (selectedNode != null)
				{
					//Node preview
					ImGui.NextColumn();
					NodeInformation();
				}

			}
			ImGuiExt.EndDock();
			Popups();
			return open;
		}

		bool doError = false;
		string errorText;
		void ErrorPopup(string error)
		{
			errorText = error;
			doError = true;
		}

		void NodeInformation()
		{
			ImGui.Text("Name: " + selectedNode.Name);
			if (selectedNode.Children != null)
			{
				ImGui.Text(selectedNode.Children.Count + " children");
			}
			else if (selectedNode.Data != null)
			{
				ImGui.Text(string.Format("Size: {0}", LibreLancer.DebugDrawing.SizeSuffix(selectedNode.Data.Length)));
				if (ImGui.Button("Hex Editor"))
				{
					hexdata = new byte[selectedNode.Data.Length];
					selectedNode.Data.CopyTo(hexdata, 0);
					mem = new MemoryEditor();
					hexEditor = true;
				}
				if (ImGui.Button("Float Editor"))
				{
					floats = new float[selectedNode.Data.Length / 4];
					for (int i = 0; i < selectedNode.Data.Length / 4; i++)
					{
						floats[i] = BitConverter.ToSingle(selectedNode.Data, i * 4);
					}
					floatEditor = true;
				}
				if (ImGui.Button("Int Editor"))
				{
					ints = new int[selectedNode.Data.Length / 4];
					for (int i = 0; i < selectedNode.Data.Length / 4; i++)
					{
						ints[i] = BitConverter.ToInt32(selectedNode.Data, i * 4);
					}
					intEditor = true;
				}
				if (ImGui.Button("Color Picker"))
				{
					var len = selectedNode.Data.Length / 4;
					if (len < 3)
					{
						pickcolor4 = true;
						color4 = new System.Numerics.Vector4(0, 0, 0, 1);
					}
					else if (len == 3)
					{
						pickcolor4 = false;
						color3 = new System.Numerics.Vector3(
							BitConverter.ToSingle(selectedNode.Data, 0),
							BitConverter.ToSingle(selectedNode.Data, 4),
							BitConverter.ToSingle(selectedNode.Data, 8));
					}
					else if (len > 3)
					{
						pickcolor4 = true;
						color4 = new System.Numerics.Vector4(
							BitConverter.ToSingle(selectedNode.Data, 0),
							BitConverter.ToSingle(selectedNode.Data, 4),
							BitConverter.ToSingle(selectedNode.Data, 8),
							BitConverter.ToSingle(selectedNode.Data, 12));
					}
					colorPicker = true;
				}
				if (ImGui.Button("Texture Viewer"))
				{
					Texture2D tex = null;
					try
					{
						using (var stream = new MemoryStream(selectedNode.Data))
						{
							tex = LibreLancer.ImageLib.Generic.FromStream(stream);
						}
						var title = string.Format("{0} ({1})", selectedNode.Name, Title);
						var tab = new TextureViewer(title, tex);
						main.AddTab(tab);
					}
					catch (Exception)
					{
						ErrorPopup("Node data couldn't be opened as texture");
					}
				}
				if (ImGui.Button("Play Audio"))
				{
					var data = main.Audio.AllocateData();
					using (var stream = new MemoryStream(selectedNode.Data))
					{
						main.Audio.PlaySound(stream);
					}
				}
				if (ImGui.Button("Import Data"))
				{
					string path;
					if ((path = FileDialog.Open()) != null)
					{
						selectedNode.Data = File.ReadAllBytes(path);
					}
				}
				if (ImGui.Button("Export Data"))
				{
					string path;
					if ((path = FileDialog.Save()) != null)
					{
						File.WriteAllBytes(path, selectedNode.Data);
					}
				}
				if (ImGui.Button("View Model"))
				{
					IDrawable drawable = null;
					try
					{
						drawable = LibreLancer.Utf.UtfLoader.GetDrawable(Utf.Export(), main.Resources);
						drawable.Initialize(main.Resources);
					}
					catch (Exception) { ErrorPopup("Could not open as model"); drawable = null; }
					if (drawable != null)
					{
						main.AddTab(new ModelViewer("Model Viewer", drawable, main.RenderState, main.Viewport, main.Commands));
					}
				}
			}
			else
			{
				ImGui.Text("Empty");
			}
		}

		bool floatEditor = false;
		float[] floats;
		bool intEditor = false;
		int[] ints;
		bool intHex = false;
		bool colorPicker = false;
		bool pickcolor4 = false;
		System.Numerics.Vector4 color4;
		System.Numerics.Vector3 color3;

		unsafe void Popups()
		{
			//Hex Editor
			if (hexEditor)
			{
				ImGui.OpenPopup("HexEditor##" +Unique);
				hexEditor = false;
			}
			if (ImGui.BeginPopupModal("HexEditor##" +Unique))
			{
				ImGui.PushFont(ImGuiHelper.Default);
				int res;
				if ((res = mem.Draw("Hex", hexdata, hexdata.Length, 0)) != 0)
				{
					if (res == 1) selectedNode.Data = hexdata;
					ImGui.CloseCurrentPopup();
				}
				ImGui.PopFont();
				ImGui.EndPopup();
			}
			//Color Picker
			if (colorPicker)
			{
				ImGui.OpenPopup("Color Picker##" +Unique);
				colorPicker = false;
			}
			if (ImGui.BeginPopupModal("Color Picker##" +Unique, WindowFlags.AlwaysAutoResize))
			{
				bool old4 = pickcolor4;
				ImGui.Checkbox("Alpha?", ref pickcolor4);
				if (old4 != pickcolor4)
				{
					if (old4 == false) color4 = new System.Numerics.Vector4(color3.X, color3.Y, color3.Z, 1);
					if (old4 == true) color3 = new System.Numerics.Vector3(color4.X, color4.Y, color4.Z);
				}
				ImGui.Separator();
				if (pickcolor4)
					ImGui.ColorPicker4("Color", ref color4, ColorEditFlags.AlphaPreview  | ColorEditFlags.AlphaBar);
				else
					ImGui.ColorPicker3("Color", ref color3);
				ImGui.Separator();
				if (ImGui.Button("Ok"))
				{
					ImGui.CloseCurrentPopup();
					if (pickcolor4)
					{
						var bytes = new byte[16];
						fixed (byte* ptr = bytes)
						{
							var f = (System.Numerics.Vector4*)ptr;
							f[0] = color4;
						}
						selectedNode.Data = bytes;
					}
					else
					{
						var bytes = new byte[12];
						fixed(byte* ptr = bytes)
						{
							var f = (System.Numerics.Vector3*)ptr;
							f[0] = color3;
						}
						selectedNode.Data = bytes;
					}
				}
				ImGui.SameLine();
				if (ImGui.Button("Cancel"))
				{
					ImGui.CloseCurrentPopup();
				}
				ImGui.EndPopup();
			}
			//Float Editor
			if (floatEditor)
			{
				ImGui.OpenPopup("Float Editor##" +Unique);
				floatEditor = false;
			}
			DataEditors.FloatEditor("Float Editor##" +Unique, ref floats, selectedNode);
			if (intEditor)
			{
				ImGui.OpenPopup("Int Editor##" +Unique);
				intEditor = false;
			}
			DataEditors.IntEditor("Int Editor##" +Unique, ref ints, ref intHex, selectedNode);
			//Rename dialog
			if (doRename)
			{
				ImGui.OpenPopup("Rename##" +Unique);
				doRename = false;
			}
			if (ImGui.BeginPopupModal("Rename##" +Unique, WindowFlags.AlwaysAutoResize))
			{
				ImGui.Text("Name: ");
				ImGui.SameLine();
				ImGui.InputText("", text.Pointer, text.Size, InputTextFlags.Default, text.Callback);
				if (ImGui.Button("Ok"))
				{
					var n = text.GetText().Trim();
					if (n.Length == 0)
						ErrorPopup("Node name cannot be empty");
					else
						renameNode.Name = text.GetText();
					ImGui.CloseCurrentPopup();
				}
				ImGui.SameLine();
				if (ImGui.Button("Cancel"))
				{
					ImGui.CloseCurrentPopup();
				}
				ImGui.EndPopup();
			}
			//Delete dialog
			if (doDelete)
			{
				ImGui.OpenPopup("Delete##" +Unique);
				doDelete = false;
			}
			if (ImGui.BeginPopupModal("Delete##" +Unique, WindowFlags.AlwaysAutoResize))
			{
				ImGui.Text("Are you sure you want to delete: '" + deleteNode.Name + "'?");
				if (ImGui.Button("Ok"))
				{
					deleteParent.Children.Remove(deleteNode);
					ImGui.CloseCurrentPopup();
				}
				ImGui.SameLine();
				if (ImGui.Button("Cancel"))
				{
					ImGui.CloseCurrentPopup();
				}
				ImGui.EndPopup();
			}
			//Error
			if (doError)
			{
				ImGui.OpenPopup("Error##" +Unique);
				doError = false;
			}
			if (ImGui.BeginPopupModal("Error##" +Unique, WindowFlags.AlwaysAutoResize))
			{
				ImGui.Text(errorText);
				if (ImGui.Button("Ok")) ImGui.CloseCurrentPopup();
				ImGui.EndPopup();
			}
			//Add
			if (doAddConfirm)
			{
				ImGui.OpenPopup("Confirm Add##" + Unique);
				doAddConfirm = false;
			}
			if (ImGui.BeginPopupModal("Confirm Add##" + Unique, WindowFlags.AlwaysAutoResize))
			{
				ImGui.Text("Adding children will clear this node's data. Continue?");
				if (ImGui.Button("Yes"))
				{
					doAdd = true;
					ImGui.CloseCurrentPopup();
				}
				ImGui.SameLine();
				if (ImGui.Button("No")) ImGui.CloseCurrentPopup();
				ImGui.EndPopup();
			}
			if (doAdd)
			{
				ImGui.OpenPopup("New Node##" + Unique);
				doAdd = false;
			}
			if (ImGui.BeginPopupModal("New Node##" + Unique, WindowFlags.AlwaysAutoResize))
			{
				ImGui.Text("Name: ");
				ImGui.SameLine();
				ImGui.InputText("", text.Pointer, text.Size, InputTextFlags.Default, text.Callback);
				if (ImGui.Button("Ok"))
				{
					var node = new LUtfNode() { Name = text.GetText().Trim() };
					if (node.Name.Length == 0)
					{
						ErrorPopup("Node name cannot be empty");
					}
					else
					{
						if (addParent != null)
							addParent.Children.Insert(addParent.Children.IndexOf(addNode) + 1, node);
						else
						{
							addNode.Data = null;
							if (addNode.Children == null) addNode.Children = new List<LUtfNode>();
							addNode.Children.Add(node);
						}
					}
					ImGui.CloseCurrentPopup();
				}
				ImGui.SameLine();
				if (ImGui.Button("Cancel"))
				{
					ImGui.CloseCurrentPopup();
				}
				ImGui.EndPopup();
			}
		}


		bool doRename = false;
		LUtfNode renameNode;

		bool doDelete = false;
		LUtfNode deleteNode;
		LUtfNode deleteParent;

		bool doAdd = false;
		bool doAddConfirm = false;
		LUtfNode addNode;
		LUtfNode addParent;
		void DoNodeMenu(string id, LUtfNode node, LUtfNode parent)
		{
			if (ImGui.BeginPopupContextItem(id))
			{
				ImGui.MenuItem(node.Name, false);
				ImGui.Separator();
				if (ImGui.MenuItem("Rename", node != Utf.Root))
				{
					text.SetText(node.Name);
					renameNode = node;
					doRename = true;
				}
				if (ImGui.MenuItem("Delete", node != Utf.Root))
				{
					deleteParent = parent;
					deleteNode = node;
					doDelete = true;
				}
				if (ImGui.BeginMenu("Add"))
				{
					if (ImGui.MenuItem("Child"))
					{
						text.SetText("");
						addParent = null;
						addNode = node;
						if (selectedNode.Data != null)
							doAddConfirm = true;
						else
							doAdd = true;
					}
					if (ImGui.MenuItem("Sibling", node != Utf.Root))
					{
						text.SetText("");
						addParent = parent;
						addNode = node;
						doAdd = true;
					}
					ImGui.EndMenu();
				}
				ImGui.EndPopup();
			}
		}

		void DoNode(LUtfNode node, LUtfNode parent, int idx)
		{
			string id = node.Name + "##" + idx;
			if (node.Children != null)
			{
				var flags = selectedNode == node ? TreeNodeFlags.Selected | tflags : tflags;
				var isOpen = ImGui.TreeNodeEx(id, flags);
				if (ImGuiNative.igIsItemClicked(0))
				{
					selectedNode = node;
				}
				ImGui.PushID(id);
				DoNodeMenu(id, node, parent);
				ImGui.PopID();
				int i = 0;
				if (isOpen)
				{
					foreach (var c in node.Children)
						DoNode(c, node, i++);
					ImGui.TreePop();
				}
			}
			else
			{
				if (node.Data != null)
				{
					ImGui.Bullet();
				}
				else
				{
					ImGui.Image((IntPtr)ImGuiHelper.CircleId, new Vector2(15, 19), Vector2.Zero, Vector2.One, Vector4.One, Vector4.Zero);
					ImGui.SameLine();
				}
				bool selected = selectedNode == node;
				if (ImGui.SelectableEx(id, ref selected))
				{
					selectedNode = node;
				}
				DoNodeMenu(id, node, parent);
			}

		}
	}
}
