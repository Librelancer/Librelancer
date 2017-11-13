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
using System.Collections.Generic;
using System.Text;
using System.IO;
using Xwt;

namespace LancerEdit
{
	public class UtfEditorPage : LTabPage
	{
		EditableUtf utf;
		TreeView utfTree;
		TreeStore store;
		DataField<string> nodename = new DataField<string>();
		DataField<LUtfNode> utfd = new DataField<LUtfNode>();
		Menu contextMenu;

		MenuItem renameItem;
		MenuItem copyItem;
		MenuItem pasteItem;
		MenuItem pasteAsSibling;
		MenuItem deleteItem;
		WindowFrame window;
		AppInstance app;

		Frame previewFrame;
		VBox intermediatePreviewBox;
		Label intermediatePreview;
		Label dataSizeLabel;
		VBox dataPreview;
		TextEntry asciiPreview;
		TextEntry hexPreview;
		public UtfEditorPage(WindowFrame window, AppInstance app, string name) : base(name)
		{
			this.app = app;
			this.window = window;
			HBox paned = new HBox();

			utfTree = new TreeView();
			store = new TreeStore(nodename, utfd);
			utfTree.DataSource = store;
			utfTree.Columns.Add("", nodename);
			utfTree.SelectionChanged += UtfTree_SelectionChanged;
			utfTree.ButtonPressed += UtfTree_ButtonPressed;
			paned.PackStart(utfTree, true,true);

			previewFrame = new Frame();
			previewFrame.Label = Txt._("Node Information");
			intermediatePreview = new Label();
			paned.PackStart(previewFrame);
			contextMenu = new Menu();
			renameItem = new MenuItem(Txt._("Rename"));
			renameItem.Clicked += RenameItem_Clicked;
			contextMenu.Items.Add(renameItem);

			copyItem = new MenuItem(Txt._("Copy"));
			copyItem.Clicked += CopyItem_Clicked;
			contextMenu.Items.Add(copyItem);

			pasteItem = new MenuItem(Txt._("Paste"));
			pasteItem.SubMenu = new Menu();
			var pasteAsChild = new MenuItem(Txt._("As Child"));
			pasteAsChild.Clicked += PasteAsChild_Clicked;
			pasteItem.SubMenu.Items.Add(pasteAsChild);
			pasteAsSibling = new MenuItem(Txt._("As Sibling"));
			pasteAsSibling.Clicked += PasteAsSibling_Clicked;
			pasteItem.SubMenu.Items.Add(pasteAsSibling);
			contextMenu.Items.Add(pasteItem);

			deleteItem = new MenuItem(Txt._("Delete"));
			deleteItem.Clicked += DeleteItem_Clicked;
			contextMenu.Items.Add(deleteItem);


			PackStart(paned, true, true);

			dataPreview = new VBox();
			var playAudio = new Button() { Label = Txt._("Play Audio") };
			playAudio.Clicked += PlayAudio_Clicked;
			var viewTexture = new Button() { Label = Txt._("View Texture") };
			viewTexture.Clicked += ViewTexture_Clicked;
			var editASCII = new Button() { Label = Txt._("Edit ASCII") };
			editASCII.Clicked += EditASCII_Clicked;

			var previewTable = new Table();
			previewTable.Add(new Label() { Text = "ASCII:" }, 0, 0);
			asciiPreview = new TextEntry() { Sensitive = false };
			hexPreview = new TextEntry() { Sensitive = false };
			previewTable.Add(asciiPreview, 1, 0);
			previewTable.Add(new Label() { Text = "Hex:" }, 0, 1);
			previewTable.Add(hexPreview, 1, 1);
			dataSizeLabel = new Label();
			dataPreview.PackStart(dataSizeLabel);
			dataPreview.PackStart(previewTable);

			dataPreview.PackStart(playAudio);
			dataPreview.PackStart(viewTexture);
			dataPreview.PackStart(editASCII);
			this.window = window;
		}

		public void Load(string filename)
		{
			utf = new EditableUtf(filename);
			var nav = store.AddNode().SetValue(nodename, "/").SetValue(utfd, utf.Root);
			foreach (var child in utf.Root.Children) {
				PopulateStore(nav.AddChild(), child);
				nav.MoveToParent();
			}
		}

		public override void DoSave()
		{
			using (var save = new SaveFileDialog())
			{
				if (save.Run(ParentWindow))
				{
					utf.Save(save.FileName);
				}
			}
		}

		public override void DoModelView()
		{
			var cmps = utf.Root.Children.FindAll((n) => n.Name.ToLowerInvariant() == "cmpnd");
			var spheres = utf.Root.Children.FindAll((n) => n.Name.ToLowerInvariant() == "sphere");
			if (cmps.Count > 1)
			{
				MessageDialog.ShowError("More than one cmpnd node");
				return;
			}
			if (spheres.Count > 1)
			{
				MessageDialog.ShowError("More than one sphere node");
				return;
			}
			if (cmps.Count > 0 && spheres.Count > 0)
			{
				MessageDialog.ShowError("Ambiguous between cmp and sph");
				return;
			}

			LibreLancer.IDrawable drawable = null;
			if (cmps.Count > 0)
			{
				try
				{
					drawable = new LibreLancer.Utf.Cmp.CmpFile(utf.Export(), app.Resources);
				}
				catch (Exception ex)
				{
					MessageDialog.ShowError("Failed to open as cmp: \n" + ex.Message);
					return;
				}
			}
			else if (spheres.Count > 0)
			{
				try
				{
					drawable = new LibreLancer.Utf.Mat.SphFile(utf.Export(), app.Resources);
				}
				catch (Exception ex)
				{
					MessageDialog.ShowError("Failed to open as sph: \n" + ex.Message);
					return;
				}
			}
			else
			{
				try
				{
					drawable = new LibreLancer.Utf.Cmp.ModelFile(utf.Export(), app.Resources);
				}
				catch (Exception ex)
				{
					MessageDialog.ShowError("Failed to open as 3db: \n" + ex.Message + "\n" + ex.StackTrace);
					return;
				}
			}
			new ModelViewer(app, drawable).Show();
		}

		public void NewFile()
		{
			utf = new EditableUtf();
			store.AddNode().SetValue(nodename, "/").SetValue(utfd, utf.Root);
		}

		void PopulateStore(TreeNavigator nav, LUtfNode node)
		{
			nav = nav.SetValue(nodename, node.Name).SetValue(utfd, node);
			if (node.Children != null)
			{
				foreach (var child in node.Children)
				{
					PopulateStore(nav.AddChild(), child);
					nav.MoveToParent();
				}
			}
		}

		void UtfTree_SelectionChanged(object sender, EventArgs e)
		{
			var row = utfTree.SelectedRow;
            LUtfNode utfn = null;
            try
            {
                utfn = store.GetNavigatorAt(row).GetValue(utfd);
            }
            catch (Exception)
            {
                return;
            }
			
			if (utfn.Children != null)
			{
				DoIntermediatePreview(utfn);
			}
			else if (utfn.Data != null)
			{
				DoDataPreview(utfn);
			}
			else
			{
				previewFrame.Content = null;
			}
		}

		void DoIntermediatePreview(LUtfNode node)
		{
			if (node.Children.Count == 1)
				intermediatePreview.Text = Txt._("1 child");
			else
				intermediatePreview.Text = string.Format(Txt._("{0} children"), node.Children.Count);
			previewFrame.Content = intermediatePreview;
		}

		void DoDataPreview(LUtfNode node)
		{
			dataSizeLabel.Text = string.Format(Txt._("Size: {0}"), BytesToString(node.Data.Length));
			asciiPreview.Text = Encoding.ASCII.GetString(node.Data, 0, Math.Min(node.Data.Length, 16)).TrimEnd('\0');
			if (node.Data.Length > 16) asciiPreview.Text += " ...";
			var builder = new StringBuilder();
			for (int i = 0; i < 7 && i < node.Data.Length; i++) {
				builder.Append(node.Data[i].ToString("X2")).Append(' ');
			}
			if (node.Data.Length > 7) builder.Append("...");
			hexPreview.Text = builder.ToString();
			previewFrame.Content = dataPreview;
		}

		static string BytesToString(long byteCount)
		{
			string[] suf = { " bytes", " KB", " MB", " GB", " TB", " PB", " EB" }; //Longs run out around EB
			if (byteCount == 0)
				return "0" + suf[0];
			long bytes = Math.Abs(byteCount);
			int place = Convert.ToInt32(Math.Floor(Math.Log(bytes, 1024)));
			double num = Math.Round(bytes / Math.Pow(1024, place), 1);
			return (Math.Sign(byteCount) * num).ToString() + suf[place];
		}

		void UtfTree_ButtonPressed(object sender, ButtonEventArgs e)
		{
			if (e.Button == PointerButton.Right)
			{
				var row = utfTree.SelectedRow;
				if (row != null)
				{
					var utfn = store.GetNavigatorAt(row).GetValue(utfd);
					renameItem.Sensitive = utfn != utf.Root;
					copyItem.Sensitive = utfn != utf.Root;
					pasteItem.Sensitive = clipboard != null;
					pasteAsSibling.Sensitive = utfn != utf.Root;
					deleteItem.Sensitive = utfn != utf.Root;
					contextMenu.Popup(utfTree, e.X, e.Y);
				}
			}
		}

		LUtfNode clipboard;

		void CopyItem_Clicked(object sender, EventArgs e)
		{
			var row = utfTree.SelectedRow;
			var utfn = store.GetNavigatorAt(row).GetValue(utfd);
			clipboard = utfn.MakeCopy();
		}

		void RenameItem_Clicked(object sender, EventArgs e)
		{
			var row = utfTree.SelectedRow;
			var nav = store.GetNavigatorAt(row);
			var utfn = nav.GetValue(utfd);
			using (var dlg = new RenameNodeDialog(utfn.Name))
			{
				var cmd = dlg.Run(window);
				if (cmd == RenameNodeDialog.AcceptCommand)
				{
					nav.SetValue(nodename, dlg.NodeName);
					utfn.Name = dlg.NodeName;
				}
			}
		}

		void PasteAsChild_Clicked(object sender, EventArgs e)
		{
			var row = utfTree.SelectedRow;
			var nav = store.GetNavigatorAt(row);
			var utfn = nav.GetValue(utfd);
			if (utfn.Data != null)
			{
				if (!XwtUtils.YesNoDialog(window, Txt._("Adding a child to this node will erase its current data. Continue?")))
					return;
			}
			utfn.Data = null;
			if (utfn.Children == null) utfn.Children = new List<LUtfNode>();
			utfn.Children.Add(clipboard);
			var c = nav.AddChild();
			PopulateStore(c, clipboard);
		}

		void PasteAsSibling_Clicked(object sender, EventArgs e)
		{
			var row = utfTree.SelectedRow;
			var nav = store.GetNavigatorAt(row);
			var sibling = nav.GetValue(utfd);
			nav.MoveToParent();
			var parent = nav.GetValue(utfd);
			parent.Children.Insert(parent.Children.IndexOf(sibling), clipboard);
			nav = store.GetNavigatorAt(row);
			var c = nav.InsertAfter();
			PopulateStore(c, clipboard);
		}

		void DeleteItem_Clicked(object sender, EventArgs e)
		{
			if (!XwtUtils.YesNoDialog(window, Txt._("Are you sure you want to delete this node?")))
				return;
			var row = utfTree.SelectedRow;
			var nav = store.GetNavigatorAt(row);
			var utfn = nav.GetValue(utfd);
			nav.MoveToParent();
			var parent = nav.GetValue(utfd);
			nav = store.GetNavigatorAt(row);
			nav.Remove();
			parent.Children.Remove(utfn);
		}

		void PlayAudio_Clicked(object sender, EventArgs e)
		{
			var row = utfTree.SelectedRow;
			var nav = store.GetNavigatorAt(row);
			var utfn = nav.GetValue(utfd);

			var data = app.Audio.AllocateData();
			using (var stream = new MemoryStream(utfn.Data))
			{
				app.Audio.PlaySound(stream);
			}
		}

		void ViewTexture_Clicked(object sender, EventArgs e)
		{
			var row = utfTree.SelectedRow;
			var nav = store.GetNavigatorAt(row);
			var utfn = nav.GetValue(utfd);
			var viewer = new TextureViewer(app, utfn.Data);
			if (viewer.Success) viewer.Show();
		}

		void EditASCII_Clicked(object sender, EventArgs e)
		{
			var row = utfTree.SelectedRow;
			var nav = store.GetNavigatorAt(row);
			var utfn = nav.GetValue(utfd);
			var data = Encoding.ASCII.GetString(utfn.Data).TrimEnd('\0');
			using (var editor = new ASCIIEditor(data))
			{
				if (editor.Run() == ASCIIEditor.AcceptCommand)
				{
					utfn.Data = Encoding.ASCII.GetBytes(editor.Data + "\0");
					DoDataPreview(utfn);
				}
			}
		}
	}
}
