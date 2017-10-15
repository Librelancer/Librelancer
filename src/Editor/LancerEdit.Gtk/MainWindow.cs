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
using Gtk;
using LibreLancer;

using Pinta.Docking;
using Pinta.Docking.DockNotebook;
using Pinta.Docking.Gui;

namespace LancerEdit.Gtk
{
	public class MainWindow : Window, IMainWindow
	{
		DockFrame dock;
		DockNotebookContainer notebookContainer;
		VBox mainBox;
		AppInstance app;

		public MainWindow() : base(WindowType.Toplevel)
		{
			DefaultWidth = 600;
			DefaultHeight = 400;

			app = new AppInstance(Xwt.Toolkit.CurrentEngine.WrapWindow(this), this);
			mainBox = new VBox();
			var menu = new Xwt.Menu();
			app.ConstructMenu(menu);
			var backend = (Xwt.GtkBackend.MenuBackend)Xwt.Toolkit.GetBackend(menu);
			mainBox.PackStart(backend.MenuBar, false, false, 0);
			Add(mainBox);
			CreateDock();
			app.AddTabs();
			mainBox.ShowAll();

			DockNotebookManager.TabClosed += DockNotebookManager_TabClosed;
			DeleteEvent += OnDeleteEvent;
		}

		void CreateDock()
		{
			dock = new DockFrame();
			dock.CompactGuiLevel = 5;

			var style = new DockVisualStyle();
			style.PadTitleLabelColor = Styles.PadLabelColor;
			style.PadBackgroundColor = Styles.PadBackground;
			style.InactivePadBackgroundColor = Styles.InactivePadBackground;
			style.TabStyle = DockTabStyle.Normal;
			style.ShowPadTitleIcon = false;
			dock.DefaultVisualStyle = style;

			var documents = new DocumentsPad(dock);
			notebookContainer = documents.NotebookContainer;

			mainBox.PackStart(dock, true, true, 0);
			if (!dock.HasLayout("Default"))
				dock.CreateLayout("Default", false);
			dock.CurrentLayout = "Default";
		}

		void DockNotebookManager_TabClosed(object sender, TabClosedEventArgs e)
		{
			if (e.Tab == null || e.Tab.Content == null)
				return;
			var sdi = (SdiWorkspaceWindow)e.Tab.Content;
			if (((LancerContent)sdi.ViewContent).Page.CloseRequest())
				notebookContainer.CloseTab(e.Tab);
			else
				e.Cancel = true;
		}

		protected void OnDeleteEvent(object sender, DeleteEventArgs a)
		{
			app.OnQuit();
			Application.Quit();
			a.RetVal = true;
		}

		public void AddTab(LTabPage page)
		{
			notebookContainer.TabControl.InsertTab(
				new LancerContent(page),
				notebookContainer.TabControl.CurrentTabIndex + 1
			);
		}

		public void RemoveTab(LTabPage page)
		{
			throw new NotImplementedException();
		}

		public LTabPage GetCurrentTab()
		{
			var sdi = (SdiWorkspaceWindow)notebookContainer.TabControl.CurrentTab.Content;
			return ((LancerContent)sdi.ViewContent).Page;
		}

		public Xwt.Drawing.Image GetImage(byte[] data, int width, int height)
		{
			var pixbuf = new Gdk.Pixbuf(data, true, 8, width, height, width * 4);
			return Xwt.Toolkit.CurrentEngine.WrapImage(pixbuf);
		}

		public void Quit()
		{
			app.OnQuit();
			Application.Quit();
		}

		public void EnsureUIThread(System.Action work)
		{
			Application.Invoke((sender, e) => work());
		}

		public void QueueUIThread(System.Action work)
		{
			Application.Invoke((sender, e) => work());
		}
	}
}