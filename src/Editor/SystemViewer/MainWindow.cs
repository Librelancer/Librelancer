// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Collections.Generic;
using System.Numerics;
using System.Linq;
using System.Threading;
using LibreLancer;
using LibreLancer.ImUI;
using LibreLancer.Infocards;
using ImGuiNET;
namespace SystemViewer
{
    public class MainWindow : Game
    {
        public ViewportManager Viewport;
        public GameDataManager GameData;
        public GameResourceManager Resources;
        public Billboards Billboards;
        public NebulaVertices Nebulae;
        public Renderer2D Renderer2D;
        public RichTextEngine RichText;

        private const float ROTATION_SPEED = 1f;

        ImGuiHelper guiHelper;
        GameWorld world;
        DebugCamera camera;
        LibreLancer.GameData.StarSystem curSystem;
        private SystemMap systemMap = new SystemMap();
        FontManager fontMan;
        bool vSync = true;
        public MainWindow() : base(800,600,false)
        {
            FLLog.UIThread = this;
        }
        protected override void Load()
        {
            Title = "System Viewer";
            LibreLancer.Shaders.AllShaders.Compile();
            guiHelper = new ImGuiHelper(this);
            FileDialog.RegisterParent(this);
            Viewport = new ViewportManager(this.RenderState);
            Viewport.Push(0, 0, 800, 600);
            Billboards = new Billboards();
            Nebulae = new NebulaVertices();
            Resources = new GameResourceManager(this);
            Renderer2D = new Renderer2D(this.RenderState);
            RichText = Renderer2D.CreateRichTextEngine();
            Services.Add(Billboards);
            Services.Add(Nebulae);
            Services.Add(Resources);
            Services.Add(Renderer2D);
            fontMan = new FontManager();
            fontMan.ConstructDefaultFonts();
            Services.Add(fontMan);
            Services.Add(new GameConfig());
        }
        protected override void Update(double elapsed)
        {
            if(world != null) {
                ProcessInput(elapsed);
                camera.Update(TimeSpan.FromSeconds(elapsed));
                camera.Free = true;
                world.Update(TimeSpan.FromSeconds(elapsed));
            }
        }
        void ProcessInput(double delta)
        {
            if (Keyboard.IsKeyDown(Keys.Right))
            {
                camera.Rotation = new Vector2(camera.Rotation.X - (ROTATION_SPEED * (float)delta),
                    camera.Rotation.Y);
            }
            if (Keyboard.IsKeyDown(Keys.Left))
            {
                camera.Rotation = new Vector2(camera.Rotation.X + (ROTATION_SPEED * (float)delta),
                    camera.Rotation.Y);
            }
            if (Keyboard.IsKeyDown(Keys.Up))
            {
                camera.Rotation = new Vector2(camera.Rotation.X,
                    camera.Rotation.Y + (ROTATION_SPEED * (float)delta));
            }
            if (Keyboard.IsKeyDown(Keys.Down))
            {
                camera.Rotation = new Vector2(camera.Rotation.X,
                    camera.Rotation.Y - (ROTATION_SPEED * (float)delta));
            }
            if (Keyboard.IsKeyDown(Keys.W))
            {
                camera.MoveVector = -Vector3.UnitZ;
            }
            if (Keyboard.IsKeyDown(Keys.S))
            {
                camera.MoveVector = Vector3.UnitZ;
            }
            if (Keyboard.IsKeyDown(Keys.A))
            {
                camera.MoveVector = -Vector3.UnitX;
            }
            if (Keyboard.IsKeyDown(Keys.D))
            {
                camera.MoveVector = Vector3.UnitX;
            }
            if (Keyboard.IsKeyDown(Keys.D1))
            {
                camera.MoveSpeed = 3000;
            }
            if (Keyboard.IsKeyDown(Keys.D2))
            {
                camera.MoveSpeed = 300;
            }
            if (Keyboard.IsKeyDown(Keys.D3))
            {
                camera.MoveSpeed = 90;
            }
            if(Keyboard.IsKeyDown(Keys.F6))
            {
                openChangeSystem = true;
            }
        }
        const string DEBUG_TEXT =
@"{0} ({1})
Position: (X: {2:0.00}, Y: {3:0.00}, Z: {4:0.00})
C# Memory Usage: {5}
{6} FPS
{7} Draw Calls
{8} Vertex Buffers
";
        bool showDebug = true;
        bool openChangeSystem = false;
        bool openLoad = false;
        string[] systems;
        int sysIndex = 0;
        int sysIndexLoaded = 0;
        bool wireFrame = false;
        bool infocardOpen = true;
        bool universeOpen = true;
        InfocardControl icard;
        Infocard systemInfocard;
        protected override void Draw(double elapsed)
        {
            VertexBuffer.TotalDrawcalls = 0;
            EnableTextInput();
            Viewport.Replace(0, 0, Width, Height);
            RenderState.ClearColor = new Color4(0.2f, 0.2f, 0.2f, 1f);
            RenderState.ClearAll();
            //
            if(world != null) {
                if (wireFrame) RenderState.Wireframe = true;
                world.Renderer.Draw();
                RenderState.Wireframe = false;
            }
            //
            guiHelper.NewFrame(elapsed);
            ImGui.PushFont(ImGuiHelper.Noto);
            //Main Menu
            ImGui.BeginMainMenuBar();
            if(ImGui.BeginMenu("File")) {
                if(Theme.IconMenuItem("Open","open",Color4.White,true)) {
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
                if(Theme.IconMenuItem("Quit","quit",Color4.White,true)) {
                    Exit();
                }
                ImGui.EndMenu();
            }
            if(world != null) {
                if(ImGui.MenuItem("Change System (F6)")) {
                    sysIndex = sysIndexLoaded;
                    openChangeSystem = true;
                }
            }
            if(ImGui.BeginMenu("View")) {
                if (ImGui.MenuItem("Debug Text", "", showDebug, true)) showDebug = !showDebug;
                if (ImGui.MenuItem("Wireframe", "", wireFrame, true)) wireFrame = !wireFrame;
                if (ImGui.MenuItem("Infocard", "", infocardOpen, true)) infocardOpen = !infocardOpen;
                if (ImGui.MenuItem("Universe Map", "", universeOpen, true)) universeOpen = !universeOpen;
                if(ImGui.MenuItem("VSync", "", vSync, true))
                {
                    vSync = !vSync;
                    SetVSync(vSync);
                }
                ImGui.EndMenu();
            }
            var h = ImGui.GetWindowHeight();
            ImGui.EndMainMenuBar();
            //Other Windows
            if(world != null) {
                if(showDebug) {
                    ImGui.SetNextWindowPos(new Vector2(0, h), ImGuiCond.Always, Vector2.Zero);
                    
                    ImGui.Begin("##debugWindow", ImGuiWindowFlags.NoTitleBar | 
                                      ImGuiWindowFlags.NoMove | ImGuiWindowFlags.AlwaysAutoResize | ImGuiWindowFlags.NoBringToFrontOnFocus);
                    ImGui.Text(string.Format(DEBUG_TEXT, curSystem.Name, curSystem.Nickname,
                                            camera.Position.X, camera.Position.Y, camera.Position.Z,
                                             DebugDrawing.SizeSuffix(GC.GetTotalMemory(false)), (int)Math.Round(RenderFrequency), VertexBuffer.TotalDrawcalls, VertexBuffer.TotalBuffers));
                    ImGui.End();
                }
                ImGui.SetNextWindowSize(new Vector2(100, 100), ImGuiCond.FirstUseEver);
                if (infocardOpen)
                {
                    if (ImGui.Begin("Infocard", ref infocardOpen))
                    {
                        var szX = Math.Max(20, ImGui.GetWindowWidth());
                        var szY = Math.Max(20, ImGui.GetWindowHeight());
                        if (icard == null)
                        {
                            icard = new InfocardControl(this, systemInfocard, szX);
                        }
                        icard.Draw(szX);

                    }
                    ImGui.End();
                }

                if (universeOpen)
                {
                    if (ImGui.Begin("Map", ref universeOpen))
                    {
                        ImGui.BeginTabBar("##maptabs");
                        if (ImGui.BeginTabItem("Universe"))
                        {
                            var szX = Math.Max(20, ImGui.GetWindowWidth());
                            var szY = Math.Max(20, ImGui.GetWindowHeight() - 50);
                            string result = UniverseMap.Draw(universeBackgroundRegistered, GameData, (int) szX,
                                (int) szY, 20);
                            if (result != null)
                            {
                                for (int i = 0; i < systems.Length; i++)
                                {
                                    if (result.Equals(systems[i], StringComparison.OrdinalIgnoreCase))
                                    {
                                        sysIndex = i;
                                        ChangeSystem();
                                        break;
                                    }
                                }
                            }

                            ImGui.EndTabItem();
                        }

                        if (ImGui.BeginTabItem("System"))
                        {
                            var szX = Math.Max(20, ImGui.GetWindowWidth());
                            var szY = Math.Max(20, ImGui.GetWindowHeight() - 70);
                            systemMap.Draw((int)szX, (int)szY);
                            ImGui.EndTabItem();
                        }

                        ImGui.EndTabBar();
                    }
                    ImGui.End();
                }
            }
            //dialogs must be children of window or ImGui default "Debug" window appears
            if(openChangeSystem) {
                ImGui.OpenPopup("Change System");
                openChangeSystem = false;
            }
            bool popupopen = true;
            if(ImGui.BeginPopupModal("Change System",ref popupopen, ImGuiWindowFlags.AlwaysAutoResize)) {
                ImGui.Combo("System", ref sysIndex, systems, systems.Length);
                if(ImGui.Button("Ok")) {
                    ChangeSystem();
                    ImGui.CloseCurrentPopup();
                }
                ImGui.SameLine();
                if(ImGui.Button("Cancel")) {
                    ImGui.CloseCurrentPopup();
                }
                ImGui.EndPopup();
            }
            if(openLoad) {
                ImGui.OpenPopup("Loading");
                openLoad = false;
            }
            popupopen = true;
            if(ImGui.BeginPopupModal("Loading", ref popupopen, ImGuiWindowFlags.AlwaysAutoResize)) {
                if (world != null) ImGui.CloseCurrentPopup();
                ImGuiExt.Spinner("##spinner", 10, 2, ImGui.GetColorU32(ImGuiCol.ButtonHovered, 1));
                ImGui.SameLine();
                ImGui.Text("Loading");
                ImGui.EndPopup();
            }
            ImGui.PopFont();
            guiHelper.Render(RenderState);
        }

        void ChangeSystem()
        {
            if (sysIndex != sysIndexLoaded) {
                camera.UpdateProjection();
                camera.Free = false;
                camera.Zoom = 5000;
                Resources.ClearTextures();
                if(universeBackgroundTex != null)
                    ImGuiHelper.DeregisterTexture(universeBackgroundTex);
                universeBackgroundTex = (Resources.FindTexture("fancymap.tga") as Texture2D);
                if (universeBackgroundTex != null)
                    universeBackgroundRegistered = ImGuiHelper.RegisterTexture(universeBackgroundTex);
                else
                    universeBackgroundRegistered = -1;
                curSystem = GameData.GetSystem(systems[sysIndex]);
                systemInfocard = GameData.GetInfocard(curSystem.Infocard, fontMan);
                if (icard != null) icard.SetInfocard(systemInfocard);
                GameData.LoadAllSystem(curSystem);
                world.LoadSystem(curSystem, Resources);
                systemMap.SetObjects(curSystem);
                sysIndexLoaded = sysIndex;
            }
        }
        protected override void OnResize()
        {
            if(camera != null) {
                camera.Viewport = new Viewport(0, 0, Width, Height);
                camera.UpdateProjection();
            }
        }

        private Texture2D universeBackgroundTex;
        private int universeBackgroundRegistered;
        void OnLoadComplete()
        {
            systemMap.CreateContext(this);
            fontMan.LoadFontsFromGameData(GameData);
            camera = new DebugCamera(new Viewport(0, 0, Width, Height));
            camera.Zoom = 5000;
            camera.UpdateProjection();
            var renderer = new SystemRenderer(camera, GameData, Resources, this);
            world = new GameWorld(renderer);
            systems = GameData.ListSystems().OrderBy(x => x).ToArray();
            Resources.ClearTextures();
            Resources.LoadResourceFile(GameData.ResolveDataPath("INTERFACE/NEURONET/NAVMAP/NEWNAVMAP/nav_prettymap.3db"));
            universeBackgroundTex = (Resources.FindTexture("fancymap.tga") as Texture2D);
            if (universeBackgroundTex != null)
                universeBackgroundRegistered = ImGuiHelper.RegisterTexture(universeBackgroundTex);
            else
                universeBackgroundRegistered = -1;
            curSystem = GameData.GetSystem(systems[0]);
            systemInfocard = GameData.GetInfocard(curSystem.Infocard, fontMan);
            GameData.LoadAllSystem(curSystem);
            world.LoadSystem(curSystem, Resources);
            systemMap.SetObjects(curSystem);
        }
        void LoadData(string path)
        {
            if (world != null) {
                world.Renderer.Dispose();
                world.Dispose();
                world = null;
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
