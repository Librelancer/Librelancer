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
		public Billboards Billboards;
		public PolylineRender Polyline;
		public PhysicsDebugRenderer DebugRender;
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
			Billboards = new Billboards();
			Polyline = new PolylineRender(Commands);
			DebugRender = new PhysicsDebugRenderer();
			Viewport.Push(0, 0, 800, 600);
		}

		bool openAbout = false;
		public List<DockTab> tabs = new List<DockTab>();
		public List<MissingReference> MissingResources = new List<MissingReference>();
		public List<uint> ReferencedMaterials = new List<uint>();
		public List<string> ReferencedTextures = new List<string>();
		public bool ClipboardCopy = true;
		public LUtfNode Clipboard;
		List<DockTab> toAdd = new List<DockTab>();
		public UtfTab ActiveTab;
		double frequency = 0;
		int updateTime = 10;
		public void AddTab(DockTab tab)
		{
			toAdd.Add(tab);
		}
		protected override void Update(double elapsed)
		{
			foreach (var tab in tabs)
				tab.Update(elapsed);
		}
        DockTab selected;
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
				if (ImGui.MenuItem("New", "Ctrl-N", false, true))
				{
					var t = new UtfTab(this, new EditableUtf(), "Untitled");
					ActiveTab = t;
                    AddTab(t);
				}
				if (ImGui.MenuItem("Open", "Ctrl-O", false, true))
				{
					var f = FileDialog.Open();
					if (f != null && DetectFileType.Detect(f) == FileType.Utf)
					{
						var t = new UtfTab(this, new EditableUtf(f), System.IO.Path.GetFileName(f));
						ActiveTab = t;
                        AddTab(t);
					}
				}
				if (ActiveTab == null)
				{
					ImGui.MenuItem("Save", "Ctrl-S", false, false);
				}
				else
				{
					if (ImGui.MenuItem(string.Format("Save '{0}'", ActiveTab.Title), "Ctrl-S", false, true))
					{
						var f = FileDialog.Save();
						if (f != null)
						{
							ActiveTab.Title = System.IO.Path.GetFileName(f);
							ActiveTab.Utf.Save(f);
						}
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
					tabs.Add(new ResourcesTab(Resources, MissingResources, ReferencedMaterials, ReferencedTextures));
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
			if (ImGui.BeginPopupModal("About", WindowFlags.AlwaysAutoResize))
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
			MissingResources.Clear();
			ReferencedMaterials.Clear();
			ReferencedTextures.Clear();
			foreach (var tab in tabs)
			{
				tab.DetectResources(MissingResources, ReferencedMaterials, ReferencedTextures);
			}
            ImGui.SetNextWindowSize(new Vector2(size.X, size.Y - 25), Condition.Always);
            ImGui.SetNextWindowPos(new Vector2(0, menu_height), Condition.Always, Vector2.Zero);
            bool childopened = true;
            ImGui.BeginWindow("tabwindow", ref childopened,
                              WindowFlags.NoTitleBar |
                              WindowFlags.NoSavedSettings |
                              WindowFlags.NoBringToFrontOnFocus |
                              WindowFlags.NoMove |
                              WindowFlags.NoResize);
            TabHandler.TabLabels(tabs, ref selected);
            ImGui.BeginChild("###tabcontent");
            if (selected != null)
                selected.Draw();
            ImGui.EndChild();
            TabHandler.DrawTabDrag(tabs);
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
			string activename = ActiveTab == null ? "None" : ActiveTab.Title;
			string utfpath = ActiveTab == null ? "None" : ActiveTab.GetUtfPath();
			ImGui.Text(string.Format("FPS: {0} | {1} Materials | {2} Textures | Active: {3} - {4}",
									 (int)Math.Round(frequency),
									 Resources.MaterialDictionary.Count,
									 Resources.TextureDictionary.Count,
									 activename,
									 utfpath));
			ImGui.EndWindow();
			ImGui.PopFont();
			guiHelper.Render(RenderState);
            foreach (var tab in toAdd)
            {
                tabs.Add(tab);
                selected = tab;
            }
			toAdd.Clear();
		}

		protected override void Cleanup()
		{
			Audio.Dispose();
		}
	}
}
