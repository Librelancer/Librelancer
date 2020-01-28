// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Collections.Generic;
using System.IO;
using LibreLancer;
using LibreLancer.ImUI;
using ImGuiNET;
using LibreLancer.Data;
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
        }

        List<DockTab> tabs = new List<DockTab>();
        private DockTab selected = null;
        private ResourceWindow resourceEditor;
        private ProjectWindow projectWindow;
        public UiContext Context;
        public FontManager Fonts;
        TestingApi api = new TestingApi();


        protected override void Update(double elapsed)
        {
            base.Update(elapsed);
        }

        public string XmlFolder = null;

        void WriteBlankFiles()
        {
            var resources = new InterfaceResources();
            File.WriteAllText(Path.Combine(XmlFolder, "resources.xml"), resources.ToXml());
            File.WriteAllText(Path.Combine(XmlFolder, "stylesheet.xml"), "<Stylesheet></Stylesheet>");
        }

        public void OpenXml(string path)
        {
            var tab = new DesignerTab(File.ReadAllText(path), path, this);
            tabs.Add(tab);
        }

        public void OpenLua(string path)
        {
            tabs.Add(new ScriptEditor(path));
        }

        void NewGui(string folder)
        {
            Context = new UiContext();
            Context.FlDirectory = folder;
            Context.ResourceManager = new GameResourceManager(this);
            Context.Renderer2D = this.Renderer2D;
            Context.FileSystem = FileSystem.FromFolder(folder);
            Context.RenderState = RenderState;
            Context.Fonts = Fonts;
            Context.GameApi = api;
            var flIni = new FreelancerIni(Context.FileSystem);
            if (flIni.XInterfacePath != null)
            {
                XmlFolder = Context.FileSystem.Resolve(flIni.XInterfacePath);
                if (!Context.FileSystem.FileExists(Path.Combine(flIni.XInterfacePath, "resources.xml")))
                    WriteBlankFiles();
            }
            else
            {
                var dataPath = Context.FileSystem.Resolve(flIni.DataPath);
                XmlFolder = Path.Combine(dataPath, "XmlUi");
                Directory.CreateDirectory(XmlFolder);
                WriteBlankFiles();
                var flIniPath = Context.FileSystem.Resolve("EXE\\freelancer.ini");
                var flIniText = File.ReadAllText(flIniPath);
                File.WriteAllText(flIniPath, $"{flIniText}\n\n[Extended]\nxinterface = XmlUi");
            }
            Context.OpenFolder(flIni.XInterfacePath);
            try
            {
                var navbarIni = new LibreLancer.Data.BaseNavBarIni(Context.FileSystem);
                Context.NavbarIcons = navbarIni.Navbar;
            }
            catch (Exception)
            {
                Context.NavbarIcons = null;
            }

            try
            {
                var hud = new HudIni();
                hud.AddIni(flIni.HudPath, Context.FileSystem);
                var maneuvers = new List<Maneuver>();
                var p = flIni.DataPath.Replace('\\', Path.DirectorySeparatorChar);
                foreach (var m in hud.Maneuvers)
                {
                    maneuvers.Add(new Maneuver()
                    {
                        Action = m.Action,
                        ActiveModel = Path.Combine(p,m.ActiveModel),
                        InactiveModel = Path.Combine(p,m.InactiveModel)
                    });
                }
                api.ManeuverData = maneuvers.ToArray();
            }
            catch (Exception)
            {
                api.ManeuverData = null;
            }
            if (flIni.JsonResources != null)
                Context.Infocards = new InfocardManager(flIni.JsonResources.Item1, flIni.JsonResources.Item2);
            else if (flIni.Resources != null)
                Context.Infocards = new InfocardManager(flIni.Resources);
            Fonts.LoadFontsFromIni(flIni, Context.FileSystem);
            Context.DataPath = flIni.DataPath;
            resourceEditor = new ResourceWindow(this, Context);
            resourceEditor.IsOpen = true;
            projectWindow = new ProjectWindow(XmlFolder, this);
            projectWindow.IsOpen = true;
            tabs.Add(new StylesheetEditor(XmlFolder, Context));
        }
        
        public void WriteResources() => File.WriteAllText(Path.Combine(XmlFolder, "resources.xml"), Context.Resources.ToXml());

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
                if (Theme.IconMenuItem("Open", "open", Color4.White, true))
                {
                    string f;
                    if ((f = FileDialog.ChooseFolder()) != null)
                    {
                        NewGui(f);
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
                    ImGui.MenuItem("Bar", "", ref api.HasBar);
                    ImGui.MenuItem("Trader", "", ref api.HasTrader);
                    ImGui.MenuItem("Equipment", "", ref api.HasEquip);
                    ImGui.MenuItem("Ship Dealer", "", ref api.HasShipDealer);
                    ImGui.EndMenu();
                }
                if (ImGui.BeginMenu("Active Room"))
                {
                    var rooms = api.GetNavbarButtons();
                    for (int i = 0; i < rooms.Length; i++)
                    {
                        if (ImGui.MenuItem(rooms[i].IconName + "##" + i, "", api.ActiveHotspotIndex == i))
                            api.ActiveHotspotIndex = i;
                    }
                    ImGui.EndMenu();
                }
                
                if (ImGui.BeginMenu("Room Actions"))
                {
                    ImGui.MenuItem("Launch", "", ref api.HasLaunchAction) ;
                    ImGui.MenuItem("Repair", "", ref api.HasRepairAction);
                    ImGui.MenuItem("Missions", "", ref api.HasMissionVendor);
                    ImGui.MenuItem("News", "", ref api.HasNewsAction);
                    ImGui.EndMenu();
                }
                ImGui.EndMenu();
            }
            if (Context != null && ImGui.BeginMenu("View"))
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
            if (prevSel != selected && selected is DesignerTab des)
                des.SwitchedTo();
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
            ImGui.Text($"InterfaceEdit{(XmlFolder != null ? " - Editing: " : "")}{(XmlFolder ?? "")}");
            ImGui.End();
            //Finish Render
            ImGui.PopFont();
            guiHelper.Render(RenderState);
        }
        
    }
}