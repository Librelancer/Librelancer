// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Text;
using LibreLancer;
using LibreLancer.ImUI;
using ImGuiNET;
using LibreLancer.Data;
using LibreLancer.Dialogs;
using LibreLancer.Interface;

namespace InterfaceEdit
{
    public class MainWindow : Game
    {
        ImGuiHelper guiHelper;
        public ViewportManager Viewport;
        public Renderer2D Renderer2D;
        public MainWindow() : base(950,600,false)
        {
        }

        protected override void Load()
        {
            Title = "InterfaceEdit";
            guiHelper = new ImGuiHelper(this);
            FileDialog.RegisterParent(this);
            Renderer2D = new Renderer2D(RenderState);
            Services.Add(Renderer2D);
            Viewport = new ViewportManager(RenderState);
            Viewport.Push(0,0,Width,Height);
            new MaterialMap();
            Fonts = new FontManager();
            LibreLancer.Shaders.AllShaders.Compile();
        }

        List<DockTab> tabs = new List<DockTab>();
        private DockTab selected = null;
        private ResourceWindow resourceEditor;
        private ProjectWindow projectWindow;
        public FontManager Fonts;
        public TestingApi TestApi = new TestingApi();

        public Project Project;

        public void OpenXml(string path)
        {
            var tab = new DesignerTab(File.ReadAllText(path), path, this);
            tabs.Add(tab);
        }

        public void OpenLua(string path)
        {
            tabs.Add(new ScriptEditor(path));
        }

        void OpenGui(string path)
        {
            Project = new Project(this);
            Project.Open(path);
            resourceEditor = new ResourceWindow(this, Project.UiData);
            resourceEditor.IsOpen = true;
            projectWindow = new ProjectWindow(Project.XmlFolder, this);
            projectWindow.IsOpen = true;
            tabs.Add(new StylesheetEditor(Project.XmlFolder, Project.XmlLoader, Project.UiData));
        }

        private FileDialogFilters projectFilters =
            new FileDialogFilters(new FileFilter("Project Files", "librelancer-uiproj"));
        protected override void Draw(double elapsed)
        {
            Viewport.Replace(0, 0, Width, Height);
            RenderState.ClearColor = new Color4(0.2f, 0.2f, 0.2f, 1f);
            RenderState.ClearAll();
            guiHelper.NewFrame(elapsed);
            ImGui.PushFont(ImGuiHelper.Noto);
            ImGui.BeginMainMenuBar();
            if (ImGui.BeginMenu("File"))
            {
                if (Theme.IconMenuItem("New", "new", Color4.White, true))
                {
                    string folder;
                    string outpath;
                    if ((folder = FileDialog.ChooseFolder()) != null)
                    {
                        if ((outpath = FileDialog.Save(projectFilters)) != null)
                        {
                            var proj = new Project(this);
                            proj.Create(folder, outpath);
                            OpenGui(outpath);
                        }
                    }
                }

                if (Theme.IconMenuItem("Open", "open", Color4.White, true))
                {
                    string f;
                    if ((f = FileDialog.Open(projectFilters)) != null)
                    {
                        OpenGui(f);
                    }
                }
                if (selected is SaveableTab saveable)
                {
                    if (Theme.IconMenuItem($"Save '{saveable.Title}'", "save", Color4.White, true))
                    {
                        saveable.Save();   
                    }
                }
                else
                {
                    Theme.IconMenuItem("Save", "save", Color4.LightGray, false);
                }

                if (ImGui.MenuItem("Compile"))
                {
                    try
                    {
                        Compiler.Compile(Project.XmlFolder, Project.XmlLoader);
                    }
                    catch (Exception e)
                    {
                        var detail = new StringBuilder();
                        detail.AppendLine(e.Message);
                        detail.AppendLine(e.StackTrace);
                        Exception e2 = e.InnerException;
                        while (e2 != null)
                        {
                            detail.AppendLine("---");
                            detail.AppendLine(e2.Message);
                            detail.AppendLine(e2.StackTrace);
                            e2 = e2.InnerException;
                        }
                        CrashWindow.Run("Interface Edit", "Compile Error", detail.ToString());
                    }
                }
                if (Theme.IconMenuItem("Quit", "quit", Color4.White, true))
                {
                    Exit();
                }
                ImGui.EndMenu();
            }

            if (ImGui.BeginMenu("Lua"))
            {
                if (ImGui.BeginMenu("Base Icons"))
                {
                    ImGui.MenuItem("Bar", "", ref TestApi.HasBar);
                    ImGui.MenuItem("Trader", "", ref TestApi.HasTrader);
                    ImGui.MenuItem("Equipment", "", ref TestApi.HasEquip);
                    ImGui.MenuItem("Ship Dealer", "", ref TestApi.HasShipDealer);
                    ImGui.EndMenu();
                }
                if (ImGui.BeginMenu("Active Room"))
                {
                    var rooms = TestApi.GetNavbarButtons();
                    for (int i = 0; i < rooms.Length; i++)
                    {
                        if (ImGui.MenuItem(rooms[i].IconName + "##" + i, "", TestApi.ActiveHotspotIndex == i))
                            TestApi.ActiveHotspotIndex = i;
                    }
                    ImGui.EndMenu();
                }
                
                if (ImGui.BeginMenu("Room Actions"))
                {
                    ImGui.MenuItem("Launch", "", ref TestApi.HasLaunchAction) ;
                    ImGui.MenuItem("Repair", "", ref TestApi.HasRepairAction);
                    ImGui.MenuItem("Missions", "", ref TestApi.HasMissionVendor);
                    ImGui.MenuItem("News", "", ref TestApi.HasNewsAction);
                    ImGui.EndMenu();
                }
                ImGui.EndMenu();
            }
            if (Project != null && ImGui.BeginMenu("View"))
            {
                ImGui.MenuItem("Project", "", ref projectWindow.IsOpen);
                ImGui.MenuItem("Resources", "", ref resourceEditor.IsOpen);
                ImGui.EndMenu();
            }
            var menu_height = ImGui.GetWindowSize().Y;
            ImGui.EndMainMenuBar();
            var size = (Vector2)ImGui.GetIO().DisplaySize;
            size.Y -= menu_height;
            ImGui.SetNextWindowSize(new Vector2(size.X, size.Y - 25), ImGuiCond.Always);
            ImGui.SetNextWindowPos(new Vector2(0, menu_height), ImGuiCond.Always, Vector2.Zero);
            bool childopened = true;
            ImGui.Begin("tabwindow", ref childopened,
                ImGuiWindowFlags.NoTitleBar |
                ImGuiWindowFlags.NoSavedSettings |
                ImGuiWindowFlags.NoBringToFrontOnFocus |
                ImGuiWindowFlags.NoMove |
                ImGuiWindowFlags.NoResize);
            var prevSel = selected;
            TabHandler.TabLabels(tabs, ref selected);
            ImGui.BeginChild("##tabcontent");
            if (selected != null) selected.Draw();
            ImGui.EndChild();
            ImGui.End();
            if(resourceEditor != null) resourceEditor.Draw();
            if (projectWindow != null) projectWindow.Draw();
            //Status Bar
            ImGui.SetNextWindowSize(new Vector2(size.X, 25f), ImGuiCond.Always);
            ImGui.SetNextWindowPos(new Vector2(0, size.Y - 6f), ImGuiCond.Always, Vector2.Zero);
            bool sbopened = true;
            ImGui.Begin("statusbar", ref sbopened, 
                ImGuiWindowFlags.NoTitleBar | 
                ImGuiWindowFlags.NoSavedSettings | 
                ImGuiWindowFlags.NoBringToFrontOnFocus | 
                ImGuiWindowFlags.NoMove | 
                ImGuiWindowFlags.NoResize);
            ImGui.Text($"InterfaceEdit{(Project != null ? " - Editing: " : "")}{(Project?.ProjectFile ?? "")}");
            ImGui.End();
            //Finish Render
            ImGui.PopFont();
            guiHelper.Render(RenderState);
        }

    }
}