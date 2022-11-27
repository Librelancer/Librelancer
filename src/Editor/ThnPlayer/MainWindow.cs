// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Threading;
using LibreLancer;
using LibreLancer.ImUI;
using ImGuiNET;
using LibreLancer.Interface;
using LibreLancer.Media;
using LibreLancer.Render;
using LibreLancer.Sounds;
using LibreLancer.Thn;

namespace ThnPlayer
{
    class DecompiledThn
    {
        public string Name;
        public string Text;
    }
    public class MainWindow : Game
    {
        public GameDataManager GameData;
        public GameResourceManager Resources;
        public Billboards Billboards;
        public NebulaVertices Nebulae;
        public Renderer2D Renderer2D;
        public SoundManager Sounds;
        public Typewriter Typewriter;
        public AudioManager Audio;
        private bool decompiledOpen = true;
        List<string> openFiles = new List<string>();

        private DecompiledThn[] decompiled;

        public string PreloadDataDir;
        public string[] PreloadOpen;
        
        ImGuiHelper guiHelper;
        FontManager fontMan;
        private RecentFilesHandler recents;
        public MainWindow() : base(1024,768,false)
        {
            FLLog.UIThread = this;
            recents = new RecentFilesHandler((x) => Open(x));
        }
        protected override void Load()
        {
            Title = "Thn Player";
            LibreLancer.Shaders.AllShaders.Compile();
            guiHelper = new ImGuiHelper(this, DpiScale);
            FileDialog.RegisterParent(this);
            RenderContext.PushViewport(0, 0, 800, 600);
            Billboards = new Billboards();
            Nebulae = new NebulaVertices();
            Resources = new GameResourceManager(this);
            Audio = new AudioManager(this);
            Sounds = new SoundManager(Audio, this);
            Services.Add(Sounds);
            Services.Add(Billboards);
            Services.Add(Nebulae);
            Services.Add(Resources);
            fontMan = new FontManager();
            fontMan.ConstructDefaultFonts();
            Services.Add(fontMan);
            Services.Add(new GameConfig());
            Typewriter = new Typewriter(this);
            Services.Add(Typewriter);
            Keyboard.KeyDown += KeyboardOnKeyDown;
        }

        private void KeyboardOnKeyDown(KeyEventArgs e)
        {
            if (e.Key == Keys.F5) Reload();
        }

        protected override void Cleanup()
        {
            Audio.Dispose();
        }

        private Cutscene cutscene;
        protected override void Update(double elapsed)
        {
            if (cutscene != null)
            {
                cutscene.UpdateViewport(new Rectangle(0, 0, Width, Height));
                cutscene.Update(elapsed);
            }

            Audio.UpdateAsync().Wait();
            Typewriter.Update(elapsed);
        }

        protected override void OnDrop(string file)
        {
            if(isMultipleOpen)
                openFiles.Add(file);
            else if (loaded)
                Open(file);
        }

        private bool isMultipleOpen = false;

        private string[] toReload = null;
        void Open(params string[] files)
        {
            if(files.Length <= 0) return;
            if (files.Length == 1) {
                recents.FileOpened(files[0]);
            }
            var lastFile = Path.GetFileName(files.Last());
            Title = $"{lastFile} - ThnPlayer";
            Audio.ReleaseAllSfx();
            toReload = files;
            decompiled = files.Select(x => new DecompiledThn()
            {
                Name = Path.GetFileName(x),
                Text = ThnDecompile.Decompile(x)
            }).ToArray();
            var ctx = new ThnScriptContext(null);
            cutscene = new Cutscene(ctx, GameData, new Rectangle(0,0,Width,Height), this);
            cutscene.BeginScene(files.Select(x => new ThnScript(x)));
        }

        void Reload()
        {
            if (toReload != null) Open(toReload);
        }
        protected override void Draw(double elapsed)
        {
            VertexBuffer.TotalDrawcalls = 0;
            EnableTextInput();
            RenderContext.ReplaceViewport(0, 0, Width, Height);
            RenderContext.ClearColor = new Color4(0.2f, 0.2f, 0.2f, 1f);
            RenderContext.ClearAll();
            //
            if (cutscene != null)
            {
                cutscene.Draw(elapsed);
            }
            Typewriter.Render();
            //
            guiHelper.NewFrame(elapsed);
            ImGui.PushFont(ImGuiHelper.Noto);
            bool openLoad = false;
            bool openMultiple = false;
            isMultipleOpen = false;
            if (PreloadDataDir != null)
            {
                openLoad = true;
                LoadData(PreloadDataDir);
                PreloadDataDir = null;
            }
            //Main Menu
            ImGui.BeginMainMenuBar();
            if(ImGui.BeginMenu("File")) {
                if(Theme.IconMenuItem(Icons.Open, "Load Game Data",true)) {
                    var folder = FileDialog.ChooseFolder();
                    if(folder != null) {
                        if(GameConfig.CheckFLDirectory(folder)) {
                            openLoad = true;
                            LoadData(folder);
                        } else {
                            //Error dialog
                        }
                    }
                }
                if (Theme.IconMenuItem(Icons.Open, "Open Thn", GameData != null))
                {
                    var file = FileDialog.Open();
                    if (file != null)
                    {
                        Open(file);
                    }
                }
                
                if (GameData != null) recents.Menu();
                else Theme.IconMenuItem(Icons.Open, "Open Recent", false);

                if (Theme.IconMenuItem(Icons.Open, "Open Multiple", GameData != null))
                {
                    openFiles = new List<string>();
                    openMultiple = true;
                }
                
                if(Theme.IconMenuItem(Icons.Quit, "Quit",true)) {
                    Exit();
                }
                ImGui.EndMenu();
            }

            if (ImGui.BeginMenu("View"))
            {
                ImGui.MenuItem("Decompiled", "", ref decompiledOpen);
                ImGui.EndMenu();
            }

            if (toReload != null && ImGui.MenuItem("Reload (F5)")) Reload();
            var h = ImGui.GetWindowHeight();
            ImGui.EndMainMenuBar();
            bool popupopen = true;
            if(openLoad) {
                ImGui.OpenPopup("Loading");
                openLoad = false;
            }
            popupopen = true;
            if(ImGuiExt.BeginModalNoClose("Loading", ImGuiWindowFlags.AlwaysAutoResize)) {
                if (loaded) ImGui.CloseCurrentPopup();
                ImGuiExt.Spinner("##spinner", 10, 2, ImGui.GetColorU32(ImGuiCol.ButtonHovered, 1));
                ImGui.SameLine();
                ImGui.Text("Loading");
                ImGui.EndPopup();
            }

            popupopen = true;
            if (openMultiple)
                ImGui.OpenPopup("Open Multiple");
            if (ImGui.BeginPopupModal("Open Multiple", ref popupopen, ImGuiWindowFlags.AlwaysAutoResize))
            {
                isMultipleOpen = true;
                if (ImGui.Button("+"))
                {
                    var file = FileDialog.Open();
                    if (file != null)
                    {
                        openFiles.Add(file);
                    }
                }
                ImGui.BeginChild("##files", new Vector2(200, 200), true, ImGuiWindowFlags.HorizontalScrollbar);
                int j = 0;
                foreach (var f in openFiles)
                    ImGui.Selectable(ImGuiExt.IDWithExtra(f, j++));
                ImGui.EndChild();
                if (ImGuiExt.Button("Open", openFiles.Count > 0))
                {
                    ImGui.CloseCurrentPopup();
                    Open(openFiles.ToArray());
                }
            }
            if (decompiled != null)
            {
                if (decompiledOpen)                     
                {
                    ImGui.SetNextWindowSize(new Vector2(300,300), ImGuiCond.FirstUseEver);
                    int j = 0;
                    if (ImGui.Begin("Decompiled", ref decompiledOpen))
                    {
                        ImGui.BeginTabBar("##tabs", ImGuiTabBarFlags.Reorderable);
                        foreach (var file in decompiled)
                        {
                            var tab = ImGuiExt.IDWithExtra(file.Name, j++);
                            if (ImGui.BeginTabItem(tab))
                            {
                                if (ImGui.Button("Copy"))
                                {
                                    SetClipboardText(file.Text);
                                }

                                ImGui.SetNextItemWidth(-1);
                                var th = ImGui.GetWindowHeight() - 100;
                                ImGui.PushFont(ImGuiHelper.SystemMonospace);
                                ImGui.InputTextMultiline("##src", ref file.Text, uint.MaxValue, new Vector2(0, th),
                                    ImGuiInputTextFlags.ReadOnly);
                                ImGui.PopFont();
                                ImGui.EndTabItem();
                            }
                        }
                        ImGui.EndTabBar();
                    }
                }
            }
            ImGui.PopFont();
            guiHelper.Render(RenderContext);
        }
        void OnLoadComplete()
        {
            Sounds.SetGameData(GameData);
            fontMan.LoadFontsFromGameData(GameData);
            Resources.ClearTextures();
            loaded = true;
            if (PreloadOpen != null)
            {
                Open(PreloadOpen);
                PreloadOpen = null;
            }
        }

        private bool loaded = true;
        void LoadData(string path)
        {
            loaded = false;
            if (cutscene != null)
            {
                cutscene.Dispose();
                cutscene = null;
            }
            Thread GameDataLoaderThread = new Thread(() =>
            {
                GameData = new GameDataManager(path, Resources);
                GameData.LoadData(this);
                FLLog.Info("Game", "Finished loading game data");
                EnsureUIThread(OnLoadComplete);
            });
            GameDataLoaderThread.Name = "GamedataLoader";
            GameDataLoaderThread.Start();
        }
    }
}