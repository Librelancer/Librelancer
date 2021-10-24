// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Diagnostics;
using System.IO;
using System.Numerics;
using LibreLancer;
using LibreLancer.Exceptions;
using LibreLancer.ImUI;
using ImGuiNET;

namespace Launcher
{
    class MainWindow : Game
    {
        ImGuiHelper imGui;
        ViewportManager Viewport;
        public MainWindow() : base(500, 300, false)
        {

        }
        GameConfig config;
        protected override void Load()
        {
            Title = "Librelancer";
            imGui = new ImGuiHelper(this);
            Viewport = new ViewportManager(RenderContext);
            Viewport.Push(0, 0, Width, Height);
            FileDialog.RegisterParent(this);
            freelancerFolder = new TextBuffer(512);
            config = GameConfig.Create();
            freelancerFolder.SetText(config.FreelancerPath);
            resolutionX = config.BufferWidth;
            resolutionY = config.BufferHeight;
            vsync = config.Settings.VSync;
            skipIntroMovies = !config.IntroMovies;
            masterVolume = config.Settings.MasterVolume;
            musicVolume = config.Settings.MusicVolume;
            sfxVolume = config.Settings.SfxVolume;
            if (Program.introForceDisable) skipIntroMovies = true;
        }
        int resolutionX = 640;
        int resolutionY = 480;
        bool vsync;
        bool skipIntroMovies;
        float masterVolume;
        float musicVolume;
        float sfxVolume;
        TextBuffer freelancerFolder;
        protected override void Draw(double elapsed)
        {
            Viewport.Replace(0, 0, Width, Height);
            RenderContext.ClearColor = new Color4(0.2f, 0.2f, 0.2f, 1f);
            RenderContext.ClearAll();
            imGui.NewFrame(elapsed);
            RenderContext.Renderer2D.DrawString("Arial", 16, "Librelancer", new Vector2(8), Color4.Black);
            RenderContext.Renderer2D.DrawString("Arial", 16, "Librelancer", new Vector2(6), Color4.White);
            var startY = RenderContext.Renderer2D.LineHeight("Arial", 16) + 8;
            ImGui.PushFont(ImGuiHelper.Noto);
            var size = (Vector2)ImGui.GetIO().DisplaySize;
            ImGui.SetNextWindowSize(new Vector2(size.X, size.Y - startY), ImGuiCond.Always);
            ImGui.SetNextWindowPos(new Vector2(0, startY), ImGuiCond.Always, Vector2.Zero);
            bool childopened = true;
            ImGui.Begin("screen", ref childopened,
                ImGuiWindowFlags.NoTitleBar |
                ImGuiWindowFlags.NoSavedSettings |
                ImGuiWindowFlags.NoBringToFrontOnFocus |
                ImGuiWindowFlags.NoMove |
                ImGuiWindowFlags.NoResize | 
                ImGuiWindowFlags.NoBackground);
            if (ImGui.BeginPopupModal("Error", ref openError, ImGuiWindowFlags.AlwaysAutoResize))
            {
                ImGui.Text(errorText);
                if (ImGui.Button("Ok")) ImGui.CloseCurrentPopup();
                ImGui.EndPopup();
            }
            ImGui.AlignTextToFramePadding();
            ImGui.Text("Freelancer Directory: ");
            ImGui.SameLine();
            freelancerFolder.InputText("##folder", ImGuiInputTextFlags.None, 280);
            ImGui.SameLine();
            if (ImGui.Button("..."))
            {
                string newFolder;
                if ((newFolder = FileDialog.ChooseFolder()) != null)
                {
                    freelancerFolder.SetText(newFolder);
                }
            }
            ImGui.AlignTextToFramePadding();
            ImGui.Text("Resolution: ");
            ImGui.SameLine();
            ImGui.PushItemWidth(130);
            ImGui.InputInt("##resX", ref resolutionX, 0, 0);
            resolutionX = MathHelper.Clamp(resolutionX, 600, 16384);
            ImGui.SameLine();
            ImGui.Text("x");
            ImGui.SameLine();
            ImGui.InputInt("##resY", ref resolutionY, 0, 0);
            resolutionY = MathHelper.Clamp(resolutionY, 400, 16384);
            ImGui.PopItemWidth();
            SoundSlider("Master Volume: ", ref masterVolume);
            SoundSlider("Music Volume: ", ref musicVolume);
            SoundSlider("Sfx Volume: ", ref sfxVolume);
            ImGui.Checkbox("VSync", ref vsync);
            if (Program.introForceDisable)
                ImGui.Text("Intro Movies Disabled");
            else
                ImGui.Checkbox("Skip Intro Movies", ref skipIntroMovies);
            ImGui.Dummy(new Vector2(16));
            ImGui.Dummy(new Vector2(1));
            ImGui.SameLine(ImGui.GetWindowWidth() - 70);
            if (ImGui.Button("Launch")) LaunchClicked();
            ImGui.End();
            ImGui.PopFont();
            imGui.Render(RenderContext);
        }

        string errorText = "";
        bool openError = false;
        void LaunchClicked()
        {
            try
            {
                config.FreelancerPath = freelancerFolder.GetText();
                config.IntroMovies = !skipIntroMovies;
                config.Settings.MasterVolume = masterVolume;
                config.Settings.MusicVolume = musicVolume;
                config.Settings.SfxVolume = sfxVolume;
                config.Settings.VSync = vsync;
                config.BufferWidth = resolutionX;
                config.BufferHeight = resolutionY;
                config.Validate();
            }
            catch (InvalidFreelancerDirectory)
            {
                ImGui.OpenPopup("Error");
                openError = true;
                errorText = "Invalid Freelancer Directory";
                return;
            }
            catch (Exception)
            {
                ImGui.OpenPopup("Error");
                openError = true;
                errorText = "Invalid Configuration";
                return;
            }
            config.Save();
            Process.Start(Path.Combine(GetBasePath(), "lancer"));
            Exit();
        }
        static void SoundSlider(string text, ref float flt)
        {
            ImGui.PushID(text);
            ImGui.AlignTextToFramePadding();
            ImGui.Text(text);
            ImGui.SameLine();
            ImGui.SliderFloat("##slider", ref flt, 0, 1);
            ImGui.PopID();
        }
        
        private string GetBasePath()
        {
            using var processModule = Process.GetCurrentProcess().MainModule;
            return Path.GetDirectoryName(processModule?.FileName);
        }
    }
}
