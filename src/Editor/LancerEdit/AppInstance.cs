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
		public CommandBuffer CmdBuf;
		public RenderState RenderState;
		public Renderer2D Render2D;
		public AudioManager Audio;
		public ResourceManager Resources;

		public AppInstance(WindowFrame window, IMainWindow main)
		{
			this.window = window;
			this.MainWindow = main;
			new LibreLancer.MaterialMap(); //HACK: Init this properly (prompt for Freelancer.ini ?)
			window.Title = "LancerEdit";
			RenderState = new RenderState();
			Render2D = new Renderer2D(RenderState);
			Audio = new AudioManager(main);
			CmdBuf = new CommandBuffer();
			Resources = new ResourceManager();
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

			var resMenu = new MenuItem(Txt._("Resources"));
			resMenu.SubMenu = new Menu();
			var loadResFile = new MenuItem(Txt._("Load File"));
			loadResFile.Clicked += LoadResFile_Clicked;
			resMenu.SubMenu.Items.Add(loadResFile);
			var modelView = new MenuItem(Txt._("View Model"));
			modelView.Clicked += ModelView_Clicked;
			var iniExplorer = new MenuItem(Txt._("Ini Explorer"));
			iniExplorer.Clicked += IniExplorer_Clicked;
			resMenu.SubMenu.Items.Add(iniExplorer);
			resMenu.SubMenu.Items.Add(modelView);
			var infocardExport = new MenuItem(Txt._("Infocard Export"));
			infocardExport.Clicked += (a,b) => {
				new InfocardExportDialog().Show();
			};
			resMenu.SubMenu.Items.Add(infocardExport);
			menu.Items.Add(resMenu);

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

		void IniExplorer_Clicked(object sender, EventArgs e)
		{
			MainWindow.AddTab(new DataExplorerPage(MainWindow));
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
				dialog.Filters.Add(new FileDialogFilter("Utf Files", "*.utf", "*.cmp", "*.3db", "*.mat", "*.txm", "*.sph"));
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

		void ModelView_Clicked(object sender, EventArgs e)
		{
			MainWindow.GetCurrentTab()?.DoModelView();
		}

		void LoadResFile_Clicked(object sender, EventArgs e)
		{
			using (var dialog = new OpenFileDialog(Txt._("Open")))
			{
				dialog.Filters.Add(new FileDialogFilter("Resource Files","*.cmp", "*.3db", "*.mat", "*.txm"));
				dialog.Filters.Add(new FileDialogFilter("All Files", "*.*"));
				if (dialog.Run(window))
				{
					var n = dialog.FileName.ToLowerInvariant();
					if (n.EndsWith(".mat"))
						Resources.LoadMat(dialog.FileName);
					else if (n.EndsWith(".txm"))
						Resources.LoadTxm(dialog.FileName);
					else if (n.EndsWith(".3db") || n.EndsWith(".cmp"))
						Resources.GetDrawable(dialog.FileName);
				}
			}
		}

		public void OnQuit()
		{
			Audio.Dispose();
		}
	}
}
