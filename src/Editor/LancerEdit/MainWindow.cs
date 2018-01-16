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
using LibreLancer.Media;
using ImGuiNET;
namespace LancerEdit
{
	public class MainWindow : Game
	{
		ImGuiHelper guiHelper;
		public AudioManager Audio;
		public ResourceManager Resources;
		public ViewportManager Viewport;
		public CommandBuffer Commands; //This is a huge object - only have one
		public MaterialMap MaterialMap;
		public MainWindow() : base(800,600,false,false)
		{
			MaterialMap = new MaterialMap();
			MaterialMap.AddRegex(new LibreLancer.Ini.StringKeyValue("^nomad.*$", "NomadMaterialNoBendy"));
			MaterialMap.AddRegex(new LibreLancer.Ini.StringKeyValue("^n-texture.*$", "NomadMaterialNoBendy"));
		}

		protected override void Load()
		{
			Title = "LancerEdit";
			guiHelper = new ImGuiHelper(this);
			Audio = new AudioManager(this);
            FileDialog.RegisterParent(this);
			Viewport = new ViewportManager(RenderState);
			Resources = new ResourceManager(this);
			Commands = new CommandBuffer();
			Viewport.Push(0, 0, 800, 600);
		}

		bool openAbout = false;
		public List<DockTab> tabs = new List<DockTab>();
		List<DockTab> toAdd = new List<DockTab>();
		double frequency = 0;
		int updateTime = 10;
		public void AddTab(DockTab tab)
		{
			toAdd.Add(tab);
		}
		protected override void Draw(double elapsed)
		{
			EnableTextInput();
			Viewport.Replace(0, 0, Width, Height);
			RenderState.ClearColor = new Color4(0.2f, 0.2f, 0.2f, 1f);
			RenderState.ClearAll();
			guiHelper.NewFrame(elapsed);
			ImGui.PushFont(ImGuiHelper.Noto);
			ImGui.BeginMainMenuBar();
			if (ImGui.BeginMenu("File"))
			{
				if (ImGui.MenuItem("Open", "Ctrl-O", false, true))
				{
					var f = FileDialog.Open();
					if (f != null && DetectFileType.Detect(f) == FileType.Utf)
					{
						tabs.Add(new UtfTab(this, new EditableUtf(f), System.IO.Path.GetFileName(f)));
					}
				}
				if (ImGui.MenuItem("Quit", "Ctrl-Q", false, true))
				{
					Exit();
				}
				ImGui.EndMenu();
			}
			if (ImGui.BeginMenu("Tools"))
			{
				if (ImGui.MenuItem("Resources"))
				{
					tabs.Add(new ResourcesTab(Resources));
				}
				ImGui.EndMenu();
			}
			if (ImGui.BeginMenu("Help"))
			{
				if (ImGui.MenuItem("About"))
				{
					openAbout = true;
				}
				ImGui.EndMenu();
			}
			if (openAbout)
			{
				ImGui.OpenPopup("About");
				openAbout = false;
			}
			if (ImGui.BeginPopupModal("About"))
			{
				ImGui.Text("LancerEdit");
				ImGui.Text("Callum McGing 2018");
				if (ImGui.Button("OK")) ImGui.CloseCurrentPopup();
				ImGui.EndPopup();
			}
			var menu_height = ImGui.GetWindowSize().Y;
			ImGui.EndMainMenuBar();
			var size = (Vector2)ImGui.GetIO().DisplaySize;
			size.Y -= menu_height;
			//Window
			ImGui.SetNextWindowPos(new Vector2(0, menu_height), Condition.Always, Vector2.Zero);
			ImGui.SetNextWindowSize(new Vector2(size.X, size.Y - 25), Condition.Always);
			ImGui.BeginWindow("##mainwin", WindowFlags.NoTitleBar | WindowFlags.NoMove | WindowFlags.NoResize | WindowFlags.NoBringToFrontOnFocus);
			ImGuiExt.BeginDockspace();
			for (int i = 0; i < tabs.Count; i++)
			{
				if (!tabs[i].Draw()) { //No longer open
					tabs[i].Dispose();
					tabs.RemoveAt(i);
					i--;
				}
			}
			ImGuiExt.EndDockspace();
			ImGui.EndWindow();
			//Status bar
			ImGui.SetNextWindowSize(new Vector2(size.X, 25f), Condition.Always);
			ImGui.SetNextWindowPos(new Vector2(0, size.Y - 6f), Condition.Always, Vector2.Zero);
			bool sbopened = true;
			ImGui.BeginWindow("statusbar", ref sbopened, 
			                  WindowFlags.NoTitleBar | 
			                  WindowFlags.NoSavedSettings | 
			                  WindowFlags.NoBringToFrontOnFocus | 
			                  WindowFlags.NoMove | 
			                  WindowFlags.NoResize);
			if (updateTime > 9)
			{
				updateTime = 0;
				frequency = RenderFrequency;
			}
			else { updateTime++; }
			ImGui.Text(string.Format("FPS: {0} | {1} Materials | {2} Textures", (int)Math.Round(frequency), Resources.MaterialDictionary.Count, Resources.TextureDictionary.Count));
			ImGui.EndWindow();
			ImGui.PopFont();
			guiHelper.Render(RenderState);
			foreach (var tab in toAdd)
				tabs.Add(tab);
			toAdd.Clear();
		}

		protected override void Cleanup()
		{
			Audio.Dispose();
		}
	}
}
