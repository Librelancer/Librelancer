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
using System.IO;
using System.Runtime.InteropServices;
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

		public override bool Draw()
		{
			if (ImGuiExt.BeginDock(Title +"##"+ Unique, ref open, 0))
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
				if(isOpen) 
				{
					int i = 0;
					foreach (var node in Utf.Root.Children)
					{
						DoNode(node,Utf.Root, i++);
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
			else
			{
				ImGui.Text(string.Format("Size: {0}", LibreLancer.DebugDrawing.SizeSuffix(selectedNode.Data.Length)));
				if (ImGui.Button("Hex Editor"))
				{
					hexdata = new byte[selectedNode.Data.Length];
					selectedNode.Data.CopyTo(hexdata, 0);
					mem = new MemoryEditor();
					hexEditor = true;
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

			}
		}

		void Popups()
		{
			//Hex Editor
			if (hexEditor)
			{
				ImGui.OpenPopup("HexEditor");
				hexEditor = false;
			}
			if (ImGui.BeginPopupModal("HexEditor"))
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
			//Rename dialog
			if (doRename)
			{
				ImGui.OpenPopup("Rename");
				doRename = false;
			}
			if (ImGui.BeginPopupModal("Rename", WindowFlags.AlwaysAutoResize))
			{
				ImGui.Text("Name: ");
				ImGui.SameLine();
				ImGui.InputText("", text.Pointer, text.Size, InputTextFlags.Default, text.Callback);
				if (ImGui.Button("Ok"))
				{
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
				ImGui.OpenPopup("Delete");
				doDelete = false;
			}
			if (ImGui.BeginPopupModal("Delete", WindowFlags.AlwaysAutoResize))
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
				ImGui.OpenPopup("Error");
				doError = false;
			}
			if (ImGui.BeginPopupModal("Error", WindowFlags.AlwaysAutoResize))
			{
				ImGui.Text(errorText);
				if (ImGui.Button("Ok")) ImGui.CloseCurrentPopup();
				ImGui.EndPopup();
			}
		}


		bool doRename = false;
		LUtfNode renameNode;

		bool doDelete = false;
		LUtfNode deleteNode;
		LUtfNode deleteParent;
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
				ImGui.Bullet();
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
