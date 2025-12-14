// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;
using ImGuiNET;
using LibreLancer;
using LibreLancer.Data;
using LibreLancer.Dialogs;
using LibreLancer.ImUI;
using LibreLancer.Server;
using LLServer.Screens;
using Microsoft.Win32;
using static LibreLancer.Client.CGameSession;

namespace LLServer;

public class MainWindow : Game
{
    public MainWindow() : base(600, 600, false, true)
    {

    }

    AppLog log;
    ServerConfig config;
    ImGuiHelper guiRender;
    ServerApp server;

    PopupManager pm = new PopupManager();
    ScreenManager sm = new ScreenManager();

    // App State 
    bool isRunning = false;
    string configPath = Path.Combine(Platform.GetBasePath(), "llserver.json");
    bool startupError;

    // Running Server Data
    BannedPlayerDescription[] bannedPlayers;
    AdminCharacterDescription[] admins;
    Guid? banId;
    string banSearchString;
    string adminSearchString;

    public ServerApp Server => server;

    public bool IsRunning =>
        server?.Server?.Listener?.Server?.IsRunning ?? false;

    public int ConnectedPlayers =>
        server?.Server?.Listener?.Server?.ConnectedPeersCount ?? 0;

    public int Port =>
        server?.Server?.Listener?.Port ?? 0;

    public ServerPerformance ServerPerformance =>
        server?.Server?.PerformanceStats;




    // Event Handlers
    private void LogAppendLine(string message, LogSeverity level)
    {
        log.AppendText($"{message}\n");
    }

    // Lifecycle hooks & Helpers
    protected override void Load()
    {
        log = new AppLog();
        FLLog.UIThread = this;
        FLLog.AppendLine += LogAppendLine;
        Title = "Librelancer Server";
        guiRender = new ImGuiHelper(this, 1);
        RenderContext.PushViewport(0, 0, Width, Height);

        sm.SetScreen(
            new ServerConfigurationScreen(this, sm, pm)
        );
    }
    protected override void Draw(double elapsed)
    {
        var process = guiRender.DoRender(elapsed);
        if (process == ImGuiProcessing.Sleep)
        {
            WaitForEvent(500);
        }
        else if (process == ImGuiProcessing.Slow)
        {
            WaitForEvent(50);
        }

        guiRender.NewFrame(elapsed);

        RenderContext.ReplaceViewport(0, 0, Width, Height);
        RenderContext.ClearColor = new Color4(0.2f, 0.2f, 0.2f, 1f);
        RenderContext.ClearAll();

        ImGui.PushFont(ImGuiHelper.Roboto, 0);

        var size = (Vector2)ImGui.GetIO().DisplaySize;
        ImGui.SetNextWindowSize(size, ImGuiCond.Always);
        ImGui.SetNextWindowPos(Vector2.Zero, ImGuiCond.Always);

        bool screenIsOpen = true;
        ImGui.Begin(
            "screen",
            ref screenIsOpen,
            ImGuiWindowFlags.NoTitleBar |
            ImGuiWindowFlags.NoSavedSettings |
            ImGuiWindowFlags.NoBringToFrontOnFocus |
            ImGuiWindowFlags.NoMove |
            ImGuiWindowFlags.NoResize |
            ImGuiWindowFlags.NoBackground
        );
        //Draw Menu Bar

        ImGui.BeginChild(
            "##content",
            Vector2.Zero,   // take all available space
            ImGuiChildFlags.Borders,
            ImGuiWindowFlags.NoScrollbar
        );
        ImGui.EndChild();

        //Draw Status bar

        sm.Draw(elapsed);
        ImGui.End();
        pm.Run();
        ImGui.PopFont();


        RenderContext.ClearColor = new Color4(0.2f, 0.2f, 0.2f, 1f);
        RenderContext.ClearAll();

        guiRender.Render(RenderContext);
    }

    protected override void Cleanup() => server?.StopServer();
    void Reset()
    {
        startupError = isRunning = false;
        server = null;
    }

    // State Queries
    bool ServerReady() => server.Server?.Listener?.Server?.IsRunning ?? false;

    // UI And Helpers

    public bool StartServer(ServerConfig config)
    {
        server = new ServerApp(config);

        if (!server.StartServer())
            return false;

        server.Server.PerformanceStats = new ServerPerformance(this);
        return true;
    }

    public void StopServer()
    {
        server?.StopServer();
        server = null;
    }
}
