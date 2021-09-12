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
        private RecentFilesHandler recentFiles;
        public MainWindow() : base(950,600,false)
        {
            TestApi = new TestingApi(this);
            recentFiles = new RecentFilesHandler(OpenGui);
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
        public TestingApi TestApi;
        public Project Project;


        public void UiEvent(string ev)
        {
            _playContext?.Event(ev);
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

        void OpenGui(string path)
        {
            Project = new Project(this);
            Project.Open(path);
            recentFiles.FileOpened(path);
            resourceEditor = new ResourceWindow(this, Project.UiData);
            resourceEditor.IsOpen = true;
            projectWindow = new ProjectWindow(Project.XmlFolder, this);
            projectWindow.IsOpen = true;
            tabs.Add(new StylesheetEditor(Project.XmlFolder, Project.XmlLoader, Project.UiData));
            TestApi._Infocard = Project.TestingInfocard;
        }

        private FileDialogFilters projectFilters =
            new FileDialogFilters(new FileFilter("Project Files", "librelancer-uiproj"));

        public double RenderDelta;
        protected override void Draw(double elapsed)
        {
            var delta = elapsed;
            RenderDelta = delta;
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
                recentFiles.Menu();
                if (!playing && selected is SaveableTab saveable)
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

                if (ImGui.MenuItem("Compile", Project != null && !playing))
                {
                    CompileProject();
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
                    ImGui.MenuItem("Commodity Trader", "", ref TestApi.HasCommodityTraderAction);
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

            if (Project != null && !playing && ImGui.BeginMenu("Play"))
            {
                foreach (var file in projectWindow.GetClasses())
                {
                    if (ImGui.MenuItem(file))
                    {
                        StartPlay(Path.GetFileNameWithoutExtension(file));
                    }
                }

                ImGui.EndMenu();
            }
            if (Project != null && playing && ImGui.MenuItem("Stop"))
            {
                playing = false;
                _playContext = null;
                _playData = null;
            }
            var menu_height = ImGui.GetWindowSize().Y;
            ImGui.EndMainMenuBar();
            var size = (Vector2)ImGui.GetIO().DisplaySize;
            size.Y -= menu_height;
            ImGui.SetNextWindowSize(new Vector2(size.X, size.Y - 25), ImGuiCond.Always);
            ImGui.SetNextWindowPos(new Vector2(0, menu_height), ImGuiCond.Always, Vector2.Zero);
            if (playing)
            {
                try
                {
                    Player(delta);
                }
                catch (Exception e)
                {
                    var detail = new StringBuilder();
                    BuildExceptionString(e,detail);
                    CrashWindow.Run("Interface Edit", "Runtime Error", detail.ToString());
                    playing = false;
                    _playContext = null;
                    _playData = null;
                }
            }
            else Tabs();
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
            recentFiles.DrawErrors();
            //Finish Render
            ImGui.PopFont();
            guiHelper.Render(RenderState);
        }


        private UiData _playData;
        private UiContext _playContext;
        void StartPlay(string classname)
        {
            try
            {
                _playData = new UiData()
                {
                    Fonts = Project.UiData.Fonts,
                    Infocards = Project.UiData.Infocards,
                    DataPath = Project.UiData.DataPath,
                    FileSystem = Project.UiData.FileSystem,
                    FlDirectory = Project.UiData.FlDirectory,
                    ResourceManager = Project.UiData.ResourceManager,
                    NavbarIcons = Project.UiData.NavbarIcons,
                    NavmapIcons = Project.UiData.NavmapIcons,
                    Resources = Project.UiData.Resources
                };
                _playData.SetBundle(Compiler.Compile(Project.XmlFolder, Project.XmlLoader));
                _playContext = new UiContext(_playData)
                {
                    RenderState = RenderState,
                    Renderer2D = Renderer2D
                };
                _playContext.GameApi = TestApi;
                _playContext.LoadCode();
                _playContext.OpenScene(classname);
                playing = true;
            }
            catch (Exception e)
            {
                var detail = new StringBuilder();
                BuildExceptionString(e,detail);
                CrashWindow.Run("Interface Edit", "Compile Error", detail.ToString());
            }
        }
        
        void Tabs()
        {
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
        }

        private int rtX = -1, rtY = -1;
        private RenderTarget2D renderTarget;
        private int renderTargetImage;
        private bool lastDown = false;
        void Player(double delta)
        {
            bool childopened = true;
            ImGui.Begin("playwindow", ref childopened,
                ImGuiWindowFlags.NoTitleBar |
                ImGuiWindowFlags.NoSavedSettings |
                ImGuiWindowFlags.NoBringToFrontOnFocus |
                ImGuiWindowFlags.NoMove |
                ImGuiWindowFlags.NoResize);
            var szX = Math.Max((int) ImGui.GetWindowContentRegionWidth(), 32);
            var szY = Math.Max((int) ImGui.GetWindowHeight() - 20, 32);
            if (rtX != szX || rtY != szY)
            {
                rtX = szX;
                rtY = szY;
                if (renderTarget != null)
                {
                    ImGuiHelper.DeregisterTexture(renderTarget.Texture);
                    renderTarget.Dispose();
                }
                renderTarget = new RenderTarget2D(rtX, rtY);
                renderTargetImage = ImGuiHelper.RegisterTexture(renderTarget.Texture);
            }
            RenderState.RenderTarget = renderTarget;
            Viewport.Push(0,0,rtX,rtY);
            RenderState.ClearColor = Color4.Black;
            RenderState.ClearAll();
            //Do drawing
            _playContext.GlobalTime = TotalTime;
            _playContext.ViewportWidth = rtX;
            _playContext.ViewportHeight = rtY;
            _playContext.RenderWidget(delta);
            //
            Viewport.Pop();
            RenderState.RenderTarget = null;
            //We don't use ImageButton because we need to be specific about sizing
            var cPos = ImGui.GetCursorPos();
            ImGui.Image((IntPtr) renderTargetImage, new Vector2(rtX, rtY), new Vector2(0, 1), new Vector2(1, 0));
            ImGui.SetCursorPos(cPos);
            var wPos = ImGui.GetWindowPos();
            var mX = (int) (Mouse.X - cPos.X - wPos.X);
            var mY = (int) (Mouse.Y - cPos.Y - wPos.Y);
            ImGui.InvisibleButton("##renderThing", new Vector2(rtX, rtY));
            if (ImGui.IsItemHovered())
            {
                if (ImGui.GetIO().MouseWheel != 0) {
                    _playContext.OnMouseWheel(ImGui.GetIO().MouseWheel);
                }
                _playContext.Update(null, TotalTime, mX, mY, false);
                if(ImGui.IsItemClicked(0)) _playContext.OnMouseClick();
                var isDown = ImGui.IsMouseDown(0);
                if (lastDown && !isDown) _playContext.OnMouseUp();
                if (isDown && !lastDown) _playContext.OnMouseDown();
                _playContext.MouseLeftDown = isDown;
                lastDown = isDown;
            }
            else {
                _playContext.Update(null, TotalTime, 0, 0, false);
                _playContext.MouseLeftDown = false;
                if (lastDown)
                {
                    lastDown = false;
                    _playContext.OnMouseUp();
                }
            }
            ImGui.End();
        }

        static void BuildExceptionString(Exception e, StringBuilder detail)
        {
            if (e is MoonSharp.Interpreter.InterpreterException ie)
            {
                detail.AppendLine(ie.DecoratedMessage);
            }
            detail.AppendLine(e.GetType().FullName);
            detail.AppendLine(e.Message);
            detail.AppendLine(e.StackTrace);
            if (e.InnerException != null)
            {
                detail.AppendLine("---");
                BuildExceptionString(e.InnerException, detail);
            }
        }
        private bool playing = false;
        void CompileProject()
        {
            try
            {
                Compiler.Compile(Project.XmlFolder, Project.XmlLoader, Path.Combine(Project.XmlFolder, "out"));
            }
            catch (Exception e)
            {
                var detail = new StringBuilder();
                BuildExceptionString(e, detail);
                CrashWindow.Run("Interface Edit", "Compile Error", detail.ToString());
            }
        }

    }
}