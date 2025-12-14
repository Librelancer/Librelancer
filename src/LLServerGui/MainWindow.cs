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
    void StartupGui()
    {
        ImGui.PushFont(ImGuiHelper.Roboto, 32);
        ImGui.Text("Server Configuration");
        ImGui.PopFont();
        ImGui.NewLine();
        ImGui.PushItemWidth(540);
        InputTextLabel("Server Name", "##serverName", ref config.ServerName);
        ImGui.Text("Server Description");
        ImGui.InputTextMultiline("##description", ref config.ServerDescription, 4096, Vector2.Zero);
        InputTextLabel("Freelancer Path", "##flpath", ref config.FreelancerPath);
        InputTextLabel("Database File", "##dbfile", ref config.DatabasePath);
        InputTextLabel("Configuration File", "##configfile", ref configPath);
        ImGui.PopItemWidth();
        ImGui.NewLine();
        if (ImGui.Button("Launch"))
        {
            isRunning = true;
            Task.Run(() =>
            {
                server = new ServerApp(config);
                if (!server.StartServer())
                {
                    QueueUIThread(() => startupError = true);
                }
                else
                {
                    server.Server.PerformanceStats = new ServerPerformance(this);
                    File.WriteAllText(configPath, JSON.Serialize(config));
                }
            });
        }
    }
    void RunningServerGui()
    {
        if (startupError)
        {
            ImGui.TextColored(new Vector4(1,0,0,1), "Server startup failed. Click Stop to go back to server configuration");
            ImGui.SameLine();
            if (ImGui.Button("Stop")) {
                Reset();
            }
        }
        if (ServerReady())
        {
            ImGui.SetNextWindowSize(new Vector2(400, 400) * ImGuiHelper.Scale, ImGuiCond.FirstUseEver);
            if (BeginModalWithClose("Admins"))
            {
                InputTextLabel("Character: ", "##character", ref adminSearchString);
                ImGui.SameLine();
                if (ImGui.Button("Make Admin"))
                {
                    Task.Run(async () =>
                    {
                        var adminId = await server.Server.Database.FindCharacter(adminSearchString);
                        if (adminId != null)
                        {
                            FLLog.Info("Server", $"Making {adminId.Value} admin");
                            await server.Server.Database.AdminCharacter(adminId.Value).ConfigureAwait(false);
                            server.Server.AdminChanged(adminId.Value, true);
                            admins = server.Server.Database.GetAdmins();
                            adminSearchString = "";
                        }
                    });
                }
                foreach (var a in admins)
                {
                    ImGui.Separator();
                    ImGui.Text(a.Name);
                    if (ImGui.Button("Remove Admin##" + a.Id))
                    {
                        FLLog.Info("Server", $"Removing admin from {a.Name}");
                        server.Server.Database.DeadminCharacter(a.Id).Wait();
                        server.Server.AdminChanged(a.Id, false);
                        admins = server.Server.Database.GetAdmins();
                    }
                }
                ImGui.EndPopup();
            }
            if (BeginModalWithClose("Ban", ImGuiWindowFlags.AlwaysAutoResize))
            {
                InputTextLabel("Character: ", "##character", ref banSearchString);
                ImGui.SameLine();
                if (ImGui.Button("Find Account"))
                {
                    Task.Run(async () =>
                    {
                        banId = await server.Server.Database.FindAccount(banSearchString);
                    });
                }
                if (banId != null)
                {
                    ImGui.Text($"Found account: {banId}");
                    if (ImGui.Button("Ban"))
                    {
                        server.Server.Database.BanAccount(banId.Value, DateTime.UtcNow.AddDays(30));
                        FLLog.Info("Server", $"Banned account {banId.Value}");
                        ImGui.CloseCurrentPopup();
                    }
                }
                ImGui.EndPopup();
            }
            ImGui.SetNextWindowSize(new Vector2(400, 400) * ImGuiHelper.Scale, ImGuiCond.FirstUseEver);
            if (BeginModalWithClose("Unban"))
            {
                if(bannedPlayers.Length == 0)
                    ImGui.Text("There are no banned players");
                ImGui.BeginChild("##banned");
                foreach (var b in bannedPlayers)
                {
                    ImGui.Text($"Id: {b.AccountId}");
                    ImGui.SameLine();
                    if (ImGui.Button("Unban##" + b.AccountId))
                    {
                        server.Server.Database.UnbanAccount(b.AccountId);
                        FLLog.Info("Server", $"Unbanned account {b.AccountId}");
                        ImGui.CloseCurrentPopup();
                    }
                    ImGui.Text($"Ban Expiry: {b.Expiry.ToLocalTime()}");
                    ImGui.TextWrapped($"Characters: {string.Join(", ", b.Characters)}");
                    ImGui.Separator();
                }
                ImGui.EndChild();
                ImGui.EndPopup();
            }

            if (ImGui.Button("Ban Player"))
            {
                banId = null;
                banSearchString = "";
                ImGui.OpenPopup("Ban");
            }
            ImGui.SameLine();
            if (ImGui.Button("Unban Player"))
            {
                bannedPlayers = server.Server.Database.GetBannedPlayers();
                ImGui.OpenPopup("Unban");
            }
            ImGui.SameLine();
            if (ImGui.Button("Admins"))
            {
                admins = server.Server.Database.GetAdmins();
                adminSearchString = "";
                ImGui.OpenPopup("Admins");
            }
            ImGui.SameLine();
            if (ImGui.Button("Stop"))
            {
                FLLog.Info("Server", "Stopping server");
                server.StopServer();
                Reset();
                return;
            }
            ImGui.Text($"Server Running on Port {server.Server.Listener.Port}");
            ImGui.Text(
                $"Players Connected: {server.Server.Listener.Server.ConnectedPeersCount}/{server.Server.Listener.MaxConnections}");
            Span<float> values = stackalloc float[ServerPerformance.MAX_TIMING_ENTRIES];
            var len = server.Server.PerformanceStats.Timings.Count;
            double avg = 0;
            double max = 0;
            double min = double.MaxValue;
            for (int i = 0; i < len; i++)
            {
                var v = server.Server.PerformanceStats.Timings[i];
                max = Math.Max(max, v);
                min = Math.Min(min, v);
                values[i] = v;
                avg += v;
            }
            avg /= len;
            ImGui.Text($"Update Time: (Avg: {avg:F4}ms/Min: {min:F4}ms/Max: {max:F4}ms)");
            ImGui.PlotLines("##updatetime", ref values[0], len, 0, "", 0, (float)Math.Max(max, 17),
                new Vector2(400, 150));
        }
        log.Draw();
    }
    void InputTextLabel(string label, string id, ref string text)
    {
        ImGui.AlignTextToFramePadding();
        ImGui.Text(label);
        ImGui.InputText(id, ref text, 4096);
    }
    bool BeginModalWithClose(string id, ImGuiWindowFlags? flags = null)
    {
        bool x = true;
        if (flags.HasValue)
            return ImGui.BeginPopupModal(id, ref x, flags.Value);
        return ImGui.BeginPopupModal(id, ref x);
    }

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
