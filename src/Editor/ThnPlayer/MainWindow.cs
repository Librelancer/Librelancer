// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using LibreLancer;
using LibreLancer.ImUI;
using LibreLancer.Infocards;
using ImGuiNET;
using LibreLancer.Media;

namespace ThnPlayer
{
    public class MainWindow : Game
    {
        public ViewportManager Viewport;
        public GameDataManager GameData;
        public GameResourceManager Resources;
        public Billboards Billboards;
        public NebulaVertices Nebulae;
        public Renderer2D Renderer2D;
        public SoundManager Sounds;
        public AudioManager Audio;
        private const float ROTATION_SPEED = 1f;
        ImGuiHelper guiHelper;
        FontManager fontMan;
        bool vSync = true;
        public MainWindow() : base(1024,768,false)
        {
            FLLog.UIThread = this;
        }
        protected override void Load()
        {
            Title = "Thn Player";
            guiHelper = new ImGuiHelper(this);
            FileDialog.RegisterParent(this);
            Viewport = new ViewportManager(this.RenderState);
            Viewport.Push(0, 0, 800, 600);
            Billboards = new Billboards();
            Nebulae = new NebulaVertices();
            Resources = new GameResourceManager(this);
            Renderer2D = new Renderer2D(this.RenderState);
            Audio = new AudioManager(this);
            Sounds = new SoundManager(Audio);
            Services.Add(Sounds);
            Services.Add(Billboards);
            Services.Add(Nebulae);
            Services.Add(Resources);
            Services.Add(Renderer2D);
            fontMan = new FontManager(this);
            fontMan.ConstructDefaultFonts();
            Services.Add(fontMan);
            Services.Add(new GameConfig());
        }

        protected override void Cleanup()
        {
            Audio.Dispose();
        }

        private Cutscene cutscene;
        protected override void Update(double elapsed)
        {
            if(cutscene != null)
                cutscene.Update(TimeSpan.FromSeconds(elapsed));
        }

        protected override void Draw(double elapsed)
        {
            VertexBuffer.TotalDrawcalls = 0;
            EnableTextInput();
            Viewport.Replace(0, 0, Width, Height);
            RenderState.ClearColor = new Color4(0.2f, 0.2f, 0.2f, 1f);
            RenderState.ClearAll();
            //
            if (cutscene != null)
            {
                cutscene.Draw();
            }  
            //
            guiHelper.NewFrame(elapsed);
            ImGui.PushFont(ImGuiHelper.Noto);
            bool openLoad = false;
            //Main Menu
            ImGui.BeginMainMenuBar();
            if(ImGui.BeginMenu("File")) {
                if(Theme.IconMenuItem("Load Game Data","open",Color4.White,true)) {
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
                if (Theme.IconMenuItem("Open Thn", "open", Color4.White, GameData != null))
                {
                    var file = FileDialog.Open();
                    if (file != null)
                    {
                        var script = new ThnScript(file);
                        var ctx = new ThnScriptContext(new[] { script });
                        cutscene = new Cutscene(ctx, GameData, new Viewport(0,0,Width,Height), this);
                    }
                }
                if(Theme.IconMenuItem("Quit","quit",Color4.White,true)) {
                    Exit();
                }
                ImGui.EndMenu();
            }
            var h = ImGui.GetWindowHeight();
            ImGui.EndMainMenuBar();
            bool popupopen = true;
            if(openLoad) {
                ImGui.OpenPopup("Loading");
                openLoad = false;
            }
            popupopen = true;
            if(ImGui.BeginPopupModal("Loading", ref popupopen, ImGuiWindowFlags.AlwaysAutoResize)) {
                if (loaded) ImGui.CloseCurrentPopup();
                ImGuiExt.Spinner("##spinner", 10, 2, ImGuiNative.igGetColorU32(ImGuiCol.ButtonHovered, 1));
                ImGui.SameLine();
                ImGui.Text("Loading");
                ImGui.EndPopup();
            }
            ImGui.PopFont();
            guiHelper.Render(RenderState);
        }
        void OnLoadComplete()
        {
            Sounds.SetGameData(GameData);
            fontMan.LoadFontsFromGameData(GameData);
            Resources.ClearTextures();
            loaded = true;
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
                GameData.LoadData();
                FLLog.Info("Game", "Finished loading game data");
                EnsureUIThread(OnLoadComplete);
            });
            GameDataLoaderThread.Name = "GamedataLoader";
            GameDataLoaderThread.Start();
        }
    }
}
