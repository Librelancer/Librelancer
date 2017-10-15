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
using XwtPlus.TextEditor;
using LibreLancer;
using LibreLancer.Media;
namespace LancerEdit
{
	public class AppInstance
	{
		WindowFrame window;
		public IMainWindow MainWindow;
		public RenderState RenderState;
		public Renderer2D Render2D;
		public AudioManager Audio;
		public AppInstance(WindowFrame window, IMainWindow main)
		{
			this.window = window;
			this.MainWindow = main;
			window.Title = "LancerEdit";
			RenderState = new RenderState();
			Render2D = new Renderer2D(RenderState);
			Audio = new AudioManager(main);
			TextEditorOptions.CutText = Txt._("Cut");
			TextEditorOptions.CopyText = Txt._("Copy");
			TextEditorOptions.PasteText = Txt._("Paste");
			TextEditorOptions.SelectAllText = Txt._("Select All");
		}

		public void ConstructMenu(Menu menu)
		{
			var fileMenu = new MenuItem(Txt._("File"));
			fileMenu.SubMenu = new Menu();
			var newItem = new MenuItem(Txt._("New"));
			newItem.SubMenu = new Menu();
			var newUtf = new MenuItem(Txt._("Utf File"));
			newUtf.Clicked += NewUtf_Clicked;
			newItem.SubMenu.Items.Add(newUtf);
			fileMenu.SubMenu.Items.Add(newItem);
			fileMenu.SubMenu.Items.Add(new SeparatorMenuItem());
			var openItem = new MenuItem(Txt._("Open"));
			openItem.Clicked += OpenItem_Clicked;
			fileMenu.SubMenu.Items.Add(openItem);
			var saveItem = new MenuItem(Txt._("Save"));
			saveItem.Clicked += SaveItem_Clicked;
			fileMenu.SubMenu.Items.Add(saveItem);
			fileMenu.SubMenu.Items.Add(new SeparatorMenuItem());
			var quitItem = new MenuItem(Txt._("Quit"));
			quitItem.Clicked += QuitItem_Clicked;
			fileMenu.SubMenu.Items.Add(quitItem);
			menu.Items.Add(fileMenu);
			var helpMenu = new MenuItem(Txt._("Help"));
			helpMenu.SubMenu = new Menu();
			var aboutItem = new MenuItem(Txt._("About"));
			aboutItem.Clicked += AboutItem_Clicked;
			helpMenu.SubMenu.Items.Add(aboutItem);
			menu.Items.Add(helpMenu);
		}

		public void AddTabs()
		{

		}

		void AboutItem_Clicked(object sender, EventArgs e)
		{
			using (var dialog = new AboutDialog())
			{
				dialog.Run(window);
			}
		}

		void QuitItem_Clicked(object sender, EventArgs e)
		{
			MainWindow.Quit();
		}

		void NewUtf_Clicked(object sender, EventArgs e)
		{
			var page = new UtfEditorPage(window, this, Txt._("Untitled"));
			page.NewFile();
			MainWindow.AddTab(page);
		}

		void SaveItem_Clicked(object sender, EventArgs e)
		{
			MainWindow.GetCurrentTab()?.DoSave();
		}

		void OpenItem_Clicked(object sender, EventArgs e)
		{
			using (var dialog = new OpenFileDialog(Txt._("Open")))
			{
				dialog.Filters.Add(new FileDialogFilter("Ini Files", "*.ini"));
				dialog.Filters.Add(new FileDialogFilter("Utf Files", "*.utf", "*.cmp", "*.3db", "*.mat", "*.txm"));
				dialog.Filters.Add(new FileDialogFilter("All Files", "*.*"));
				if (dialog.Run(window))
				{
					var safeName = System.IO.Path.GetFileName(dialog.FileName);
					if (DetectFileType.Detect(dialog.FileName) == FileType.Ini)
					{
						var page = new TextEditorPage(safeName);
						page.Load(dialog.FileName);
						MainWindow.AddTab(page);
					}
					else
					{
						var page = new UtfEditorPage(window, this, safeName);
						page.Load(dialog.FileName);
						MainWindow.AddTab(page);
					}
				}
			}
		}

		public void OnQuit()
		{
			Audio.Dispose();
		}
	}
}
