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
using LibreLancer.Data.Schema;
using LibreLancer.Dialogs;
using LibreLancer.Graphics;
using LibreLancer.Interface;
using LibreLancer.Render;
using LibreLancer.Utf.Anm;
using LibreLancer.Utf.Dfm;
using Microsoft.EntityFrameworkCore.Metadata.Internal;

namespace InterfaceEdit
{
    public class MainWindow : Game
    {
        ImGuiHelper guiHelper;
        private RecentFilesHandler recentFiles;
        public PopupManager Popups = new PopupManager();

        public MainWindow() : base(950,600,true)
        {
            recentFiles = new RecentFilesHandler(OpenGui);
        }

        private bool openError = false;
        private TextBuffer errorText;
        public void ErrorDialog(string text)
        {
            errorText?.Dispose();
            errorText = new TextBuffer();
            errorText.SetText(text);
            openError = true;
        }

        public Dictionary<string, string> Variables = new Dictionary<string, string>();
        private DictionaryWindow variableEditor;

        protected override void Load()
        {
            Title = "InterfaceEdit";
            TestApi = new TestingApi(this);
            guiHelper = new ImGuiHelper(this, 1);
            RenderContext.PushViewport(0,0,Width,Height);
            new MaterialMap();
            Fonts = new FontManager();
            LibreLancer.Shaders.AllShaders.Compile(RenderContext);
            Keyboard.KeyDown += args =>
            {
                if (playing && args.Key == Keys.F1)
                    _playContext.Event("Pause");
            };
            CommandBuffer = new CommandBuffer(RenderContext);
            LineRenderer = new LineRenderer(RenderContext);
            LoadVariables();
            variableEditor = new DictionaryWindow("Variables", Variables);
        }

        protected override void Cleanup()
        {
            SaveVariables();
        }

        string VariableFilePath => Path.Combine(Platform.GetLocalConfigFolder(), "ll.interfaceedit.variables.json");
        void LoadVariables()
        {
            try
            {
                if (File.Exists(VariableFilePath))
                {
                    Variables = JSON.Deserialize<Dictionary<string, string>>(File.ReadAllText(VariableFilePath));
                }
            }
            catch
            {
                Variables = new Dictionary<string, string>();
            }
        }

        void SaveVariables()
        {
            try
            {
                File.WriteAllText(VariableFilePath, JSON.Serialize(Variables));
            }
            // ReSharper disable once EmptyGeneralCatchClause
            catch
            {
            }
        }

        private TabControl tabControl = new TabControl();

        private ResourceWindow resourceEditor;
        private ProjectWindow projectWindow;
        public FontManager Fonts;
        public TestingApi TestApi;
        public Project Project;
        public CommandBuffer CommandBuffer;
        public LineRenderer LineRenderer;

        public void UiEvent(string ev)
        {
            _playContext?.Event(ev);
        }

        bool SwitchToTab(string path)
        {
            var tab = tabControl.Tabs.OfType<SaveableTab>().FirstOrDefault(x => x.Filename == path);
            if (tab != null)
            {
                tabControl.SetSelected(tab);
                return true;
            }
            return false;
        }

        public void OpenXml(string path)
        {
            if (!SwitchToTab(path))
            {
                var tab = new DesignerTab(File.ReadAllText(path), path, this);
                tabControl.Tabs.Add(tab);
            }
        }

        public void OpenLua(string path)
        {
            if (!SwitchToTab(path))
            {
                tabControl.Tabs.Add(new ScriptEditor(path));
            }
        }

        void OpenGui(string path)
        {
            Project = new Project(this);
            if(Project.Open(path)) {
                Project.UiData.ResourceManager.LoadResourceFile(
                Project.UiData.DataPath + (@"ships\rheinland\rh_playerships.mat"));
                Project.UiData.ResourceManager.LoadResourceFile(
                Project.UiData.DataPath + (@"ships\liberty\li_playerships.mat"));
                recentFiles.FileOpened(path);
                resourceEditor = new ResourceWindow(this, Project.UiData);
                resourceEditor.IsOpen = true;
                projectWindow = new ProjectWindow(Project.XmlFolder, this);
                projectWindow.IsOpen = true;
                tabControl.Tabs.Add(new StylesheetEditor(Project.XmlFolder, Project.XmlLoader, Project.UiData));
                TestApi._Infocard = Project.TestingInfocard;
                TestApi._ScannedInfocard = Project.ShipInfocard;
                var str = new StringDeduplication();
                var anm = new AnmFile();
                using(var f = Project.UiData.FileSystem.Open(Project.UiData.DataPath + @"characters\animations\bodygenericmale.anm"))
                    AnmFile.ParseToTable(anm.Scripts, anm.Buffer, str, f, @"characters\animations\bodygenericmale.anm");
                using(var f = Project.UiData.FileSystem.Open(Project.UiData.DataPath + @"characters\animations\facialmale.anm"))
                    AnmFile.ParseToTable(anm.Scripts, anm.Buffer, str, f, @"characters\animations\facialmale.anm");
                CommApp = new CommAppearance()
                {
                    Head = Project.UiData.ResourceManager.GetDrawable(
                        Project.UiData.DataPath + @"characters\heads\br_brighton_head.dfm").Drawable as DfmFile,
                    Body = Project.UiData.ResourceManager.GetDrawable(
                        Project.UiData.DataPath + @"characters\bodies\br_brighton_body.dfm").Drawable as DfmFile,
                    Male = true,
                };
                CommApp.Scripts.Add(anm.Scripts["SC_MLHEAD_MOTION_WALLA_CASL_000LV_XA_%"]);
                CommApp.Scripts.Add(anm.Scripts["SC_MLBODY_CHRB_IDLE_SMALL_000LV_XA_07"]);
            }
            else
            {
                ErrorDialog($"Could not find data folder:\n{Project.FlFolder ?? "NULL"}\n"
                + "Check your project file and editor variables");
                Project = null;
            }
        }

        private FileDialogFilters projectFilters =
            new FileDialogFilters(new FileFilter("Project Files", "librelancer-uiproj"));

        public double RenderDelta;
        private bool commOn = false;
        private CommAppearance CommApp;
        protected override void Draw(double elapsed)
        {
            var delta = elapsed;
            RenderDelta = delta;
            RenderContext.ReplaceViewport(0, 0, Width, Height);
            RenderContext.ClearColor = new Color4(0.2f, 0.2f, 0.2f, 1f);
            RenderContext.ClearAll();
            guiHelper.NewFrame(elapsed);
            ImGui.PushFont(ImGuiHelper.Roboto, 0);
            ImGui.BeginMainMenuBar();
            if (ImGui.BeginMenu("File"))
            {
                if (Theme.IconMenuItem(Icons.File, "New", true))
                {
                    FileDialog.ChooseFolder(folder =>
                    {
                        FileDialog.Save(outpath =>
                        {
                            var proj = new Project(this);
                            proj.Create(folder, outpath);
                            OpenGui(outpath);
                        });
                    });
                }

                if (Theme.IconMenuItem(Icons.Open, "Open", true))
                {
                    FileDialog.Open(OpenGui, projectFilters);
                }
                recentFiles.Menu(Popups);
                if (!playing && tabControl.Selected is SaveableTab saveable)
                {
                    if (Theme.IconMenuItem(Icons.Save, $"Save '{saveable.Title}'",  true))
                    {
                        saveable.Save();
                    }
                }
                else
                {
                    Theme.IconMenuItem(Icons.Save, "Save", false);
                }

                if (ImGui.MenuItem("Compile", Project != null && !playing))
                {
                    CompileProject();
                }
                if (Theme.IconMenuItem(Icons.Quit, "Quit", true))
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
                    ImGui.MenuItem("Ship Dealer", "", ref TestApi.HasShipDealerAction);
                    ImGui.EndMenu();
                }
                ImGui.MenuItem("Multiplayer", "", ref TestApi.Multiplayer);
                ImGui.EndMenu();
            }
            if (Project != null && ImGui.BeginMenu("View"))
            {
                ImGui.MenuItem("Project", "", ref projectWindow.IsOpen);
                ImGui.MenuItem("Resources", "", ref resourceEditor.IsOpen);
                ImGui.EndMenu();
            }

            if (ImGui.BeginMenu("Options"))
            {
                ImGui.MenuItem("Variables", "", ref variableEditor.IsOpen);
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
                commOn = false;
                _playContext = null;
                _playData = null;
            }
            if (Project != null && playing && ImGui.MenuItem("Toggle Comm"))
            {
                _playContext.Event("Comm", !commOn
                    ? new CommData()
                    {
                        Appearance = CommApp,
                        Source = "Hello World",
                        Affiliation = "Evil Faction",
                    }
                    : null);
                commOn = !commOn;
            }
            var menu_height = ImGui.GetWindowSize().Y;
            ImGui.EndMainMenuBar();
            var size = (Vector2)ImGui.GetIO().DisplaySize;
            size.Y -= menu_height;
            ImGui.SetNextWindowSize(new Vector2(size.X, size.Y - 25 * ImGuiHelper.Scale), ImGuiCond.Always);
            ImGui.SetNextWindowPos(new Vector2(0, menu_height), ImGuiCond.Always, Vector2.Zero);
            if (playing)
            {
                bool childopened = true;
                ImGui.Begin("playwindow", ref childopened,
                    ImGuiWindowFlags.NoTitleBar |
                    ImGuiWindowFlags.NoSavedSettings |
                    ImGuiWindowFlags.NoBringToFrontOnFocus |
                    ImGuiWindowFlags.NoMove |
                    ImGuiWindowFlags.NoResize);
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
                ImGui.End();
            }
            else Tabs(elapsed);
            //Status Bar
            ImGui.SetNextWindowSize(new Vector2(size.X, 25f * ImGuiHelper.Scale), ImGuiCond.Always);
            ImGui.SetNextWindowPos(new Vector2(0, size.Y - 6f), ImGuiCond.Always, Vector2.Zero);
            bool sbopened = true;
            ImGui.Begin("statusbar", ref sbopened,
                ImGuiWindowFlags.NoTitleBar |
                ImGuiWindowFlags.NoSavedSettings |
                ImGuiWindowFlags.NoBringToFrontOnFocus |
                ImGuiWindowFlags.NoMove |
                ImGuiWindowFlags.NoResize);
            ImGui.Text($"InterfaceEdit{(Project != null ? " - Editing: " : "")}{(Project?.ProjectFile ?? "")}");
            if (playing)
            {
                ImGui.SameLine();
                ImGui.Text($"Mouse Wanted: {mouseWanted}");
            }
            ImGui.End();
            variableEditor.Draw();
            if (openError)
            {
                ImGui.OpenPopup("Error");
                openError = false;
            }
            bool pOpen = true;

            if (ImGui.BeginPopupModal("Error", ref pOpen, ImGuiWindowFlags.AlwaysAutoResize))
            {
                ImGui.Text("Error:");
                errorText.InputTextMultiline("##etext", new Vector2(430, 200), ImGuiInputTextFlags.ReadOnly);
                if (ImGui.Button("OK")) ImGui.CloseCurrentPopup();
                ImGui.EndPopup();
            }

            Popups.Run();
            //Finish Render
            ImGui.PopFont();
            guiHelper.Render(RenderContext);
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
                    RenderContext = RenderContext,
                    Lines = LineRenderer
                };
                _playContext.CommandBuffer = CommandBuffer;
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

        void Tabs(double elapsed)
        {
            bool childopened = true;
            ImGui.Begin("tabwindow", ref childopened,
                ImGuiWindowFlags.NoTitleBar |
                ImGuiWindowFlags.NoSavedSettings |
                ImGuiWindowFlags.NoBringToFrontOnFocus |
                ImGuiWindowFlags.NoMove |
                ImGuiWindowFlags.NoResize);
            tabControl.TabLabels();
            ImGui.BeginChild("##tabcontent");
            if (tabControl.Selected != null) tabControl.Selected.Draw(elapsed);
            ImGui.EndChild();
            ImGui.End();
            if(resourceEditor != null) resourceEditor.Draw();
            if (projectWindow != null) projectWindow.Draw();
        }

        private int rtX = -1, rtY = -1;
        private RenderTarget2D renderTarget;
        private ImTextureRef renderTargetImage;
        private bool lastDown = false;
        bool mouseWanted = false;
        void Player(double delta)
        {
            var szX = Math.Max((int) ImGui.GetContentRegionAvail().X, 32);
            var szY = Math.Max((int) ImGui.GetWindowHeight() - (int)(20 * ImGuiHelper.Scale), 32);
            if (rtX != szX || rtY != szY)
            {
                rtX = szX;
                rtY = szY;
                if (renderTarget != null)
                {
                    ImGuiHelper.DeregisterTexture(renderTarget.Texture);
                    renderTarget.Dispose();
                }
                renderTarget = new RenderTarget2D(RenderContext, rtX, rtY);
                renderTargetImage = ImGuiHelper.RegisterTexture(renderTarget.Texture);
            }
            RenderContext.RenderTarget = renderTarget;
            RenderContext.PushViewport(0,0,rtX,rtY);
            RenderContext.ClearColor = Color4.Black;
            RenderContext.ClearAll();
            //Do drawing
            _playContext.GlobalTime = TotalTime;
            _playContext.ViewportWidth = rtX;
            _playContext.ViewportHeight = rtY;
            _playContext.RenderWidget(delta);
            //
            RenderContext.PopViewport();
            RenderContext.RenderTarget = null;
            //We don't use ImageButton because we need to be specific about sizing
            var cPos = ImGui.GetCursorPos();
            ImGui.Image(renderTargetImage, new Vector2(rtX, rtY), new Vector2(0, 1), new Vector2(1, 0));
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
                if (Keyboard.IsKeyDown(Keys.LeftAlt))
                    TestApi.OverridePosition = new Vector2(_playContext.MouseX, _playContext.MouseY);
                else
                    TestApi.OverridePosition = null;
                mouseWanted = _playContext.MouseWanted(mX, mY);
                if (ImGui.IsItemClicked(0))
                {
                    _playContext.OnMouseClick();
                    if(ImGui.IsMouseDoubleClicked(0)) _playContext.OnMouseDoubleClick();
                }
                var isDown = ImGui.IsMouseDown(0);
                if (lastDown && !isDown) _playContext.OnMouseUp();
                if (isDown && !lastDown) _playContext.OnMouseDown();
                _playContext.MouseLeftDown = isDown;
                lastDown = isDown;
            }
            else
            {
                TestApi.OverridePosition = null;
                mouseWanted = false;
                _playContext.Update(null, TotalTime, 0, 0, false);
                _playContext.MouseLeftDown = false;
                if (lastDown)
                {
                    lastDown = false;
                    _playContext.OnMouseUp();
                }
            }
        }

        static void BuildExceptionString(Exception e, StringBuilder detail)
        {
            if (e is WattleScript.Interpreter.InterpreterException ie)
            {
                detail.AppendLine(ie.DecoratedMessage);
                if (ie.CallStack != null)
                {
                    detail.AppendLine("Callstack: ");
                    foreach (var k in ie.CallStack)
                    {
                        detail.AppendLine(k.ToString());
                    }
                }
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
                Compiler.Compile(Project.XmlFolder, Project.XmlLoader, Path.Combine(Project.XmlFolder, "out"), Project.OutputFilename);
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
