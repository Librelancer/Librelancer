// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Diagnostics;
using System.IO;
using System.Numerics;
using System.Runtime.Versioning;
using ImGuiNET;
using LibreLancer;
using LibreLancer.ImUI;
using Microsoft.Win32;
using Launcher.Screens;

namespace Launcher;

public class MainWindow() : Game(640, 350, true)
{
    private ImGuiHelper imGui = null!;
    private GameConfig config = null!;
    private readonly PopupManager pm = new();
    private readonly ScreenManager sm = new();

    protected override void Load()
    {
        Title = "Librelancer Launcher";
        imGui = new ImGuiHelper(this, 1);
        RenderContext.PushViewport(0, 0, Width, Height);

        config = GameConfig.Create();

        sm.SetScreen(new LauncherScreen(this, config, sm, pm));

        if (string.IsNullOrEmpty(config.FreelancerPath) || OperatingSystem.IsWindows())
            {
                var flPath = GetFreelancerPath();
                
                if (!string.IsNullOrEmpty(flPath))
                    {
                        config.FreelancerPath=(flPath);
                        
                    } else {

                        return;
                    }
            }
    }

    protected override void Draw(double elapsed)
    {
        var process = imGui.DoRender(elapsed);
        switch (process)
        {
            case ImGuiProcessing.Sleep:
                WaitForEvent(2000);
                break;
            case ImGuiProcessing.Slow:
                WaitForEvent(50);
                break;
        }

        imGui.NewFrame(elapsed);
        RenderContext.ReplaceViewport(0, 0, Width, Height);
        RenderContext.ClearColor = new Color4(0.2f, 0.2f, 0.2f, 1f);
        RenderContext.ClearAll();
        ImGui.PushFont(ImGuiHelper.Roboto, 0);
        var size = (Vector2)ImGui.GetIO().DisplaySize;

        ImGui.SetNextWindowSize(new Vector2(size.X, size.Y), ImGuiCond.Always);
        ImGui.SetNextWindowPos(new Vector2(0, 0), ImGuiCond.Always, Vector2.Zero);

        var childOpened = true;
        ImGui.Begin("screen", ref childOpened,
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
        Program.StartPath = Path.Combine(GetBasePath(), "lancer");
        Exit();
    }

    //[SupportedOSPlatform("windows")]
    private static string GetFreelancerPath()
    {
        // TODO: This function should be in a shared class so that LLServerGui can also use it
        if (!OperatingSystem.IsWindows())
            return "";

        var defaultPath = Path.Join(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), @"\Microsoft Games\Freelancer");
        var registryPath = IntPtr.Size == 8
            ? @"HKEY_LOCAL_MACHINE\SOFTWARE\Wow6432Node\Microsoft\Microsoft Games\Freelancer\1.0"
            : @"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Microsoft Games\Freelancer\1.0";
            
        var installPath = (string?)Registry.GetValue(registryPath, "AppPath", defaultPath);
        return installPath ?? "";
    }

    private static string GetBasePath()
    {
        using var processModule = Process.GetCurrentProcess().MainModule;
        return Path.GetDirectoryName(processModule?.FileName) ?? AppDomain.CurrentDomain.BaseDirectory;
    }
}
