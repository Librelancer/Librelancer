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
    static readonly float LOGS_MIN_HEIGHT = 100f;
    static readonly Vector4 ERROR_TEXT_COLOUR = new Vector4(1f, 0.3f, 0.3f, 1f);
    

#if DEBUG
    static readonly string statusFormat = "FPS: {0} | Status: {1} | Connected: {2}/{3}";
    static readonly string titleFormat = "Librelancer Server - Debug - {0}";
#else
    static readonly string statusFormat = "Status: {1}  | Connected Players {2}";
    static readonly string titleFormat = "Librelancer Server - {0}";
#endif

    // UI Data
    bool logsOpen = false;
    float logsHeight = 200f;

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
        guiRender = new ImGuiHelper(this, 1);
        RenderContext.PushViewport(0, 0, Width, Height);
        ConfigPath = Path.Combine(Platform.GetBasePath(), "llserver.json");
        config = GetConfigFromFileOrDefault();
        sm.SetScreen(
            new ServerConfigurationScreen(this, sm, pm, config)
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

        // Height taken by logs (collapsed = header height only)
        float logsAreaHeight = logsOpen ? logsHeight : ImGui.GetFrameHeight();

        // Main content area (above logs + status bar)
        ImGui.BeginChild(
            "##content",
            new Vector2(
                avail.X,
                avail.Y - STATUS_BAR_HEIGHT - logsAreaHeight
            ),
            ImGuiChildFlags.Borders
        );
        sm.Draw(elapsed);
        Title = String.Format(titleFormat, sm.Current.Title);
        ImGui.EndChild();

        // Logs panel (bottom, inside main window)
        DrawLogsPanel(avail.X);

        // Status bar (unchanged)
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
                if (Theme.IconMenuItem(Icons.Play, "Start", !IsRunning))
                {
                    Task.Run(() =>
                    {
                        File.WriteAllText(ConfigPath, JSON.Serialize(config));

                        QueueUIThread(() =>
                        {
                            sm.SetScreen(
                                new RunningServerScreen(this, sm, pm, config)
                            );
                        });
                    });
                }
                ImGui.Spacing();
                if (Theme.IconMenuItem(Icons.Stop, "Stop", IsRunning))
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
                                        StopServer();
                                        sm.SetScreen(new ServerConfigurationScreen(this, sm, pm, config));
                                        return;
                                    });
                                }
                            });
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

    void DrawLogsPanel(float width)
    {
        // Header
        ImGui.PushStyleColor(ImGuiCol.Header, ImGui.GetStyle().Colors[(int)ImGuiCol.FrameBg]);
        ImGui.PushStyleColor(ImGuiCol.HeaderHovered, ImGui.GetStyle().Colors[(int)ImGuiCol.FrameBgHovered]);
        ImGui.PushStyleColor(ImGuiCol.HeaderActive, ImGui.GetStyle().Colors[(int)ImGuiCol.FrameBgActive]);

        

        if (ImGui.CollapsingHeader("Logs", ImGuiTreeNodeFlags.DefaultOpen))
        {
            ImGui.InvisibleButton("##logs_resize", new Vector2(width, 4));
            if (ImGui.IsItemHovered() || ImGui.IsItemActive())
            {
                ImGui.SetMouseCursor(ImGuiMouseCursor.ResizeNS);
            }

            if (ImGui.IsItemActive())
            {
                logsHeight -= ImGui.GetIO().MouseDelta.Y;
                logsHeight = Math.Clamp(logsHeight, LOGS_MIN_HEIGHT, ImGui.GetIO().DisplaySize.Y * 0.6f);
            }


            logsOpen = true;

            ImGui.Separator();

            // Log contents
            log.Draw(
                buttons: false,
                size: new Vector2(width, logsHeight - ImGui.GetFrameHeight() * 2)
            );
        }
        else
        {
            logsOpen = false;
        }
        ImGui.Spacing();
        ImGui.PopStyleColor(3);
    }

    ServerConfig GetConfigFromFileOrDefault()
    {
        ServerConfig config;

        if (File.Exists(ConfigPath))
        {
            config = JSON.Deserialize<ServerConfig>(File.ReadAllText(ConfigPath));
            if (config != null)
                return config;
        }


        config = new ServerConfig();
        if (Platform.RunningOS == OS.Windows)
        {
            var combinedPath = Path.Join(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "\\Microsoft Games\\Freelancer");
            string flPathRegistry = IntPtr.Size == 8
                ? "HKEY_LOCAL_MACHINE\\SOFTWARE\\Wow6432Node\\Microsoft\\Microsoft Games\\Freelancer\\1.0"
                : "HKEY_LOCAL_MACHINE\\SOFTWARE\\Microsoft\\Microsoft Games\\Freelancer\\1.0";
            var actualPath = (string)Registry.GetValue(flPathRegistry, "AppPath", combinedPath);
            if (!string.IsNullOrEmpty(actualPath))
            {
                config.FreelancerPath = actualPath;
            }
        }

        config.ServerName = "M9Universe";
        config.ServerDescription = "My Cool Freelancer server";
        config.DatabasePath = Path.Combine(Platform.GetBasePath(), "llserver.db");

        return config;
    }
}
