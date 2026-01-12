// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.IO;
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

namespace LLServer;

public class MainWindow() : Game(600, 600, true)
{
    public ServerApp? Server { get; private set; }

    public bool IsRunning => Server?.Server?.Listener?.Server?. IsRunning ?? false;
    public int ConnectedPlayersCount => Server?.Server?.Listener?.Server?.ConnectedPeersCount ?? 0;
    public ServerPerformance? ServerPerformance => Server?.Server?.PerformanceStats;

    public LLServerGuiConfig ServerGuiConfig = null!;

    public bool StartupError;
    private string serverGuiConfigPath = null!;
    private AppLog log = null!;
    private ServerConfig serverConfig = null!;
    private ImGuiHelper guiRender = null!;
    private readonly PopupManager pm = new();
    private readonly ScreenManager sm = new();

    private static readonly float STATUS_BAR_HEIGHT = 30f;
    private static readonly float LOGS_MIN_HEIGHT = 100f;

    private static readonly FileDialogFilters saveAsFilter = new(
        new FileFilter("json")
        );

#if DEBUG
    static readonly string statusFormat = "FPS: {0} | Status: {1} | Connected: {2}/{3}";
    static readonly string titleFormat = "Librelancer Server - Debug - {0}";
#else
    private static readonly string statusFormat = "Status: {1}  | Connected Players {2}";
    private static readonly string titleFormat = "Librelancer Server - {0}";
#endif


    // UI Data
    private bool logsOpen = false;
    private float logsHeight = 200f;

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

        serverGuiConfigPath = Path.Combine(Platform.GetBasePath(), "llserverGui.json");
        ServerGuiConfig = GetServerGuiConfigFromFileOrDefault(serverGuiConfigPath);
        serverConfig = GetServerConfigFromFileOrDefault(ServerGuiConfig.LastConfigPath);

        if (ServerGuiConfig.AutoStartServer)
        {
            sm.SetScreen(
                new RunningServerScreen(this, sm, pm, serverConfig, ServerGuiConfig)
            );
        }
        else
        {
            sm.SetScreen(
                new ServerConfigurationScreen(this, sm, pm, serverConfig, ServerGuiConfig)
            );
        }

    }
    protected override void Draw(double elapsed)
    {
        var process = guiRender.DoRender(elapsed);
        switch (process)
        {
            case ImGuiProcessing.Sleep:
                WaitForEvent(500);
                break;
            case ImGuiProcessing.Slow:
                WaitForEvent(50);
                break;
        }

        guiRender.NewFrame(elapsed);

        RenderContext.ReplaceViewport(0, 0, Width, Height);
        RenderContext.ClearColor = new Color4(0.2f, 0.2f, 0.2f, 1f);
        RenderContext.ClearAll();

        ImGui.PushFont(ImGuiHelper.Roboto, 0);

        var size = (Vector2)ImGui.GetIO().DisplaySize;
        ImGui.SetNextWindowSize(size, ImGuiCond.Always);
        ImGui.SetNextWindowPos(Vector2.Zero, ImGuiCond.Always);
        ImGui.SetNextWindowSizeConstraints(
            size,   // minimum size
            new Vector2(float.MaxValue, float.MaxValue) // maximum size
        );

        var screenIsOpen = true;
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
        var logsAreaHeight = logsOpen ? logsHeight : ImGui.GetFrameHeight();

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
        Title = string.Format(titleFormat, sm.Current.Title);
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

    protected override void Cleanup() => Server?.StopServer();

    private void Reset()
    {
        StartupError = false;
        Server = null;
    }

    // UI And Helpers
    public bool StartServer(ServerConfig config)
    {
        Server = new ServerApp(config);

        if (!Server.StartServer())
        {
            return false;
        }

        if (Server.Server != null)
        {
            Server.Server.PerformanceStats = new ServerPerformance();
        }

        return true;

    }

    public void StopServer()
    {
        Server?.StopServer();
        Server = null;
    }

    private void DrawMenuBar()
    {
        if (!ImGui.BeginMainMenuBar())
        {
            return;
        }

        if (ImGui.BeginMenu("File"))
        {
            if (Theme.IconMenuItem(Icons.Save, "Save", !IsRunning))
            {
                try
                {
                    File.WriteAllText(ServerGuiConfig.LastConfigPath, JSON.Serialize(serverConfig));
                    File.WriteAllText(serverGuiConfigPath, JSON.Serialize(ServerGuiConfig));
                    pm.MessageBox("Save", "Configuration has been saved successfully", false, MessageBoxButtons.Ok);
                } catch (Exception ex)
                {
                    pm.MessageBox("Error - Save failed", $"Configuration file failed to save:\r\t {ex.Message}", false, MessageBoxButtons.Ok);
                }
            }
            if (Theme.IconMenuItem(Icons.Copy, "Save as", !IsRunning))
            {
                FileDialog.Save(path => {
                    try
                    {
                        File.WriteAllText(path ?? throw new ArgumentNullException(nameof(path)), JSON.Serialize(serverConfig));
                        ServerGuiConfig.LastConfigPath = path;
                        SaveServerGuiConfig();
                        pm.MessageBox("Save as", "Configuration has been saved successfully", false, MessageBoxButtons.Ok);
                    }
                    catch (Exception ex)
                    {
                        pm.MessageBox("Error - Save as failed", $"Configuration file failed to save:\r\t {ex.Message}", false, MessageBoxButtons.Ok);
                    }

                }, saveAsFilter);
            }
            if (Theme.IconMenuItem(Icons.File, "Load", true))
            {
                FileDialog.Open(path => {
                    try
                    {
                        if (string.IsNullOrEmpty(path))
                        {
                            return;
                        }
                        ServerGuiConfig.LastConfigPath = path;
                        SaveServerGuiConfig();

                        var newConfig = GetServerConfigFromFileOrDefault(path);
                        serverConfig.CopyFrom(newConfig);
                    }
                    catch (Exception ex)
                    {
                        pm.MessageBox("Error - Load failed", $"Configuration file failed to load:\r\t {ex.Message}", false, MessageBoxButtons.Ok);
                    }

                }, saveAsFilter);
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
                                    Server?.Server?.Stop();
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
                    File.WriteAllText(ServerGuiConfig.LastConfigPath, JSON.Serialize(serverConfig));

                    QueueUIThread(() =>
                    {
                        sm.SetScreen(
                            new RunningServerScreen(this, sm, pm, serverConfig, ServerGuiConfig)
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
                                sm.SetScreen(new ServerConfigurationScreen(this, sm, pm, serverConfig, ServerGuiConfig));
                                return;
                            });
                        }
                    });
            }

            ImGui.EndMenu();
        }

        ImGui.EndMainMenuBar();
    }

    public void SaveServerGuiConfig()
    {
        File.WriteAllText(serverGuiConfigPath, JSON.Serialize(ServerGuiConfig));
    }

    private void DrawStatusBar()
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
                ConnectedPlayersCount,
                Server?.Server?.Listener?.MaxConnections ?? 0
                ));

        if (StartupError)
        {
            ImGui.SameLine(); ImGui.TextColored(Theme.ErrorTextColor, "Server Startup Error");
        }
        ImGui.End();
    }

    private void DrawLogsPanel(float width)
    {
        // Header
        ImGui.PushStyleColor(ImGuiCol.Header, ImGui.GetStyle().Colors[(int)ImGuiCol.FrameBg]);
        ImGui.PushStyleColor(ImGuiCol.HeaderHovered, ImGui.GetStyle().Colors[(int)ImGuiCol.FrameBgHovered]);
        ImGui.PushStyleColor(ImGuiCol.HeaderActive, ImGui.GetStyle().Colors[(int)ImGuiCol.FrameBgActive]);

        if (ImGui.CollapsingHeader("Logs"))
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

    public ServerConfig GetServerConfigFromFileOrDefault(string path)
    {
        ServerConfig? config;

        if (File.Exists(ServerGuiConfig.LastConfigPath))
        {
            config = JSON.Deserialize<ServerConfig>(File.ReadAllText(path));
            if (config != null)
            {
                return config;
            }
        }


        config = new ServerConfig();
        if (Platform.RunningOS == OS.Windows)
        {
            var combinedPath = Path.Join(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), @"\Microsoft Games\Freelancer");
            var flPathRegistry = IntPtr.Size == 8
                ? @"HKEY_LOCAL_MACHINE\SOFTWARE\Wow6432Node\Microsoft\Microsoft Games\Freelancer\1.0"
                : @"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Microsoft Games\Freelancer\1.0";

            var actualPath = (string?)Registry.GetValue(flPathRegistry, "AppPath", combinedPath);
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

    private static LLServerGuiConfig GetServerGuiConfigFromFileOrDefault(string path)
    {
        LLServerGuiConfig? config;

        if (File.Exists(path))
        {
            config = JSON.Deserialize<LLServerGuiConfig>(File.ReadAllText(path));
            if (config != null)
            {
                return config;
            }
        }


        config = new LLServerGuiConfig
        {
            LastConfigPath = Path.Combine(Platform.GetBasePath(), "llserver.json")
        };

        return config;
    }
}
