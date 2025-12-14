// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Collections.Generic;
using System.Drawing;
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

    public ServerApp Server => server;
    public bool IsRunning => server?.Server?.Listener?.Server?.IsRunning ?? false;
    public int ConnectedPlayers => server?.Server?.Listener?.Server?.ConnectedPeersCount ?? 0;
    public int Port => server?.Server?.Listener?.Port ?? 0;
    public ServerPerformance ServerPerformance => server?.Server?.PerformanceStats;
    public string ConfigPath;
    public bool StartupError;

    AppLog log;
    ServerConfig config;
    ImGuiHelper guiRender;
    ServerApp server;

    PopupManager pm = new PopupManager();
    ScreenManager sm = new ScreenManager();

    static readonly float STATUS_BAR_HEIGHT = 30f;
    readonly Vector4 ERROR_TEXT_COLOUR = new Vector4(1f, 0.3f, 0.3f, 1f);

#if DEBUG
    const string statusFormat = "FPS: {0} | Status: {1} | Connected: {3}/{2}";
#else
                const string statusFormat = "Status: {1}  | Connected Players {2}";
#endif

    // Running Server Data
    BannedPlayerDescription[] bannedPlayers;
    AdminCharacterDescription[] admins;
    Guid? banId;
    string banSearchString;
    string adminSearchString;

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
            ImGuiWindowFlags.NoBackground |
            ImGuiWindowFlags.MenuBar
            
        );

        DrawMenuBar();
        Vector2 avail = ImGui.GetContentRegionAvail();
        float contentHeight = avail.Y - STATUS_BAR_HEIGHT;

        ImGui.BeginChild(
            "##content",
            new Vector2(ImGui.GetContentRegionAvail().X, ImGui.GetContentRegionAvail().Y - STATUS_BAR_HEIGHT),   // take all available space
            ImGuiChildFlags.Borders
        );
        sm.Draw(elapsed);
        ImGui.EndChild();


        DrawStatusBar();
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
        StartupError = false;
        server = null;
    }

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

    void DrawMenuBar()
    {
        if (ImGui.BeginMainMenuBar())
        {
            if (ImGui.BeginMenu("File"))
            {
                if (Theme.IconMenuItem(Icons.Save,"Save Configuration", true))
                {
                    pm.MessageBox("Save", "Configuration has been saved successfully", false, MessageBoxButtons.Ok);
                    File.WriteAllText(ConfigPath, JSON.Serialize(config));

                }
                ImGui.Spacing();
                ImGui.Separator();
                ImGui.Spacing();
                if (Theme.IconMenuItem(Icons.Quit, "Quit", true))
                {
                    if (IsRunning)
                    {
                        pm.MessageBox(
                            title: "Confirm",
                            message: "The Server is running. Are you sure you want to quit?",
                            multiline: false,
                            buttons: MessageBoxButtons.YesNo, callback: response =>
                            {
                                if (response == MessageBoxResponse.Yes)
                                {
                                    this.QueueUIThread(() =>
                                    {
                                        server.Server.Stop();
                                        Exit();
                                        return;
                                    });
                                }
                            });
                    }
                    Exit();
                }

                ImGui.EndMenu();
            }
            if (ImGui.BeginMenu("Server"))
            {
                if (Theme.IconMenuItem(Icons.Play, "Start", true))
                {
                    if(!StartServer(config)) StartupError = true;
                }
                ImGui.Spacing();
                if (Theme.IconMenuItem(Icons.Stop, "Stop", true))
                {
                    pm.MessageBox(
                            title: "Confirm",
                            message: "The Server is running. Are you sure you want to quit?",
                            multiline: false,
                            buttons: MessageBoxButtons.YesNo, callback: response =>
                            {
                                if (response == MessageBoxResponse.Yes)
                                {
                                    this.QueueUIThread(() =>
                                    {
                                        server.Server.Stop();
                                        sm.SetScreen(new ServerConfigurationScreen(this, sm, pm));
                                        return;
                                    });
                                }
                            });
                    if (!StartServer(config)) StartupError = true;
                }

                ImGui.EndMenu();
            }
            ImGui.EndMainMenuBar();
        }
    }

    void DrawStatusBar()
    {
        var io = ImGui.GetIO();
        ImGui.SetNextWindowSize(new Vector2(io.DisplaySize.X, STATUS_BAR_HEIGHT * ImGuiHelper.Scale), ImGuiCond.Always);
        ImGui.SetNextWindowPos(new Vector2(0, io.DisplaySize.Y - STATUS_BAR_HEIGHT * ImGuiHelper.Scale), ImGuiCond.Always, Vector2.Zero);


        ImGui.Begin(
            "##statusbar",
            ImGuiWindowFlags.NoTitleBar |
            ImGuiWindowFlags.NoResize |
            ImGuiWindowFlags.NoMove |
            ImGuiWindowFlags.NoScrollbar |
            ImGuiWindowFlags.NoSavedSettings
        );

        ImGui.Text(String.Format(statusFormat,
                (int)Math.Round(RenderFrequency),
                IsRunning ? "Running" : "Stopped",
                ConnectedPlayers,
                server?.Server?.Listener?.MaxConnections ?? 0
                ));

        if (StartupError)
        {
            ImGui.SameLine(); ImGui.TextColored(ERROR_TEXT_COLOUR, "Server Startup Error");
        }
        ImGui.End();
    }

}
