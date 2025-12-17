// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Diagnostics;
using System.IO;
using System.Numerics;
using ImGuiNET;
using LibreLancer;
using LibreLancer.ImUI;
using Microsoft.Win32;
using Launcher.Screens;

namespace Launcher
{
    public class MainWindow : Game
    {
        ImGuiHelper imGui;
        public MainWindow() : base(640, 350, true)
        {

        }
        public GameConfig config;

        PopupManager pm = new PopupManager();
        ScreenManager sm = new ScreenManager();

        protected override void Load()
        {
            Title = "Librelancer Launcher";
            imGui = new ImGuiHelper(this, 1);
            RenderContext.PushViewport(0, 0, Width, Height);

            config = GameConfig.Create();

            sm.SetScreen(new LauncherScreen(this, config, sm, pm));

            if (string.IsNullOrEmpty(config.FreelancerPath))
            {
                if (Platform.RunningOS == OS.Windows)
                {
                    var combinedPath = Path.Join(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "\\Microsoft Games\\Freelancer");
                    string flPathRegistry = IntPtr.Size == 8
                        ? "HKEY_LOCAL_MACHINE\\SOFTWARE\\Wow6432Node\\Microsoft\\Microsoft Games\\Freelancer\\1.0"
                        : "HKEY_LOCAL_MACHINE\\SOFTWARE\\Microsoft\\Microsoft Games\\Freelancer\\1.0";
                    var actualPath = (string)Registry.GetValue(flPathRegistry, "AppPath", combinedPath);
                    if (!string.IsNullOrEmpty(actualPath)) config.FreelancerPath=(actualPath);
                }
            }

        }

        bool fullscreen;


        protected override void Draw(double elapsed)
        {
            imGui.NewFrame(elapsed);
            RenderContext.ReplaceViewport(0, 0, Width, Height);
            RenderContext.ClearColor = new Color4(0.2f, 0.2f, 0.2f, 1f);
            RenderContext.ClearAll();
            ImGui.PushFont(ImGuiHelper.Roboto, 0);
            var size = (Vector2)ImGui.GetIO().DisplaySize;

            ImGui.SetNextWindowSize(new Vector2(size.X, size.Y), ImGuiCond.Always);
            ImGui.SetNextWindowPos(new Vector2(0, 0), ImGuiCond.Always, Vector2.Zero);

            bool childopened = true;
            ImGui.Begin("screen", ref childopened,
                ImGuiWindowFlags.NoTitleBar |
                ImGuiWindowFlags.NoSavedSettings |
                ImGuiWindowFlags.NoBringToFrontOnFocus |
                ImGuiWindowFlags.NoMove |
                ImGuiWindowFlags.NoResize |
                ImGuiWindowFlags.NoBackground);

            sm.Draw(elapsed);
            
            ImGui.End();
            ImGui.PopFont();
            imGui.Render(RenderContext);
        }
        
        public void StartGame()
        {
            
            Program.startPath = Path.Combine(GetBasePath(), "lancer");
            Exit();
        }

        private string GetBasePath()
        {
            using var processModule = Process.GetCurrentProcess().MainModule;
            return Path.GetDirectoryName(processModule?.FileName);
        }
    }
}
