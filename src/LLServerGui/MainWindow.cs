using System;
using System.Collections.Generic;
using System.Numerics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using ImGuiNET;
using LibreLancer;
using LibreLancer.Data;
using LibreLancer.ImUI;
using LibreLancer.Server;

namespace LLServer;

public class MainWindow : Game
{
    private ImGuiHelper guiRender;
    public MainWindow() : base(800,600,false, true)
    {
    }

    private AppLog log;
    private string configPath = Path.Combine(Platform.GetBasePath(), "llserver.json");
    private ServerApp server;

    protected override void Load()
    {
        log = new AppLog();
        FLLog.UIThread = this;
        FLLog.AppendLine += LogAppendLine;
        guiRender = new ImGuiHelper(this, DpiScale);
        RenderContext.PushViewport(0, 0, Width, Height);
        if (File.Exists(configPath))
            config = JSON.Deserialize<ServerConfig>(File.ReadAllText(configPath));
        else
        {
            config = new ServerConfig();
        }
    }

    private void LogAppendLine(string message, LogSeverity level)
    {
        log.AppendText($"{message}\n");
    }

    private bool isRunning = false;

    protected override void Draw(double elapsed)
    {
        if(!guiRender.DoRender(elapsed))
        {
            WaitForEvent(500); //Yield like a regular GUI program (0.5s)
        }
        guiRender.NewFrame(elapsed);
        RenderContext.ReplaceViewport(0, 0, Width, Height);
        RenderContext.ClearColor = new Color4(0.2f, 0.2f, 0.2f, 1f);
        RenderContext.ClearAll();
        ImGui.PushFont(ImGuiHelper.Noto);
        var size = (Vector2)ImGui.GetIO().DisplaySize;
        ImGui.SetNextWindowSize(new Vector2(size.X, size.Y), ImGuiCond.Always);
        ImGui.SetNextWindowPos(new Vector2(0, 0), ImGuiCond.Always, Vector2.Zero);
        bool screenIsOpen = true;
        ImGui.Begin("screen", ref screenIsOpen,
            ImGuiWindowFlags.NoTitleBar |
            ImGuiWindowFlags.NoSavedSettings |
            ImGuiWindowFlags.NoBringToFrontOnFocus |
            ImGuiWindowFlags.NoMove |
            ImGuiWindowFlags.NoResize |
            ImGuiWindowFlags.NoBackground);
        if(isRunning)
            RunningServer();
        else
            StartupGui();
        ImGui.End();
        ImGui.PopFont();
        RenderContext.ClearColor = new Color4(0.2f, 0.2f, 0.2f, 1f);
        RenderContext.ClearAll();
        guiRender.Render(RenderContext);
    }

    private ServerConfig config;

    void InputTextLabel(string label, string id, ref string text)
    {
        ImGui.AlignTextToFramePadding();
        ImGui.TextUnformatted(label);
        ImGui.InputText(id, ref text, 4096);
    }
    void StartupGui()
    {
        ImGui.Text("Server Configuration");
        ImGui.TextUnformatted($"Configuration file: '{configPath}'");
        ImGui.Separator();
        InputTextLabel("Server Name", "##serverName", ref config.ServerName);
        ImGui.Text("Server Description");
        ImGui.InputTextMultiline("##description", ref config.ServerDescription, 4096, Vector2.Zero);
        InputTextLabel("Freelancer Path", "##flpath", ref config.FreelancerPath);
        InputTextLabel("Database File", "##dbfile", ref config.DatabasePath);
        if (ImGui.Button("Launch"))
        {
            isRunning = true;
            Task.Run(() =>
            {
                server = new ServerApp(config);
                if (!server.StartServer()) {
                    QueueUIThread(() => startupError = true);
                } else
                {
                    server.Server.PerformanceStats = new ServerPerformance(this);
                    File.WriteAllText(configPath, JSON.Serialize(config));
                }
            });
        }
    }

    protected override void Cleanup()
    {
        server?.StopServer();
    }

    private bool startupError;

    void Reset()
    {
        startupError = isRunning = false;
        server = null;
    }

    bool ServerReady() => server.Server?.Listener?.Server?.IsRunning ?? false;

    private BannedPlayerDescription[] bannedPlayers;
    private AdminCharacterDescription[] admins;
    private Guid? banId;
    private string banSearchString;
    private string adminSearchString;

    bool BeginModalWithClose(string id, ImGuiWindowFlags? flags = null)
    {
        bool x = true;
        if (flags.HasValue)
            return ImGui.BeginPopupModal(id, ref x, flags.Value);
        return ImGui.BeginPopupModal(id, ref x);
    }

    unsafe void RunningServer()
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
                    var adminId = server.Server.Database.FindCharacter(adminSearchString);
                    if (adminId != null)
                    {
                        FLLog.Info("Server", $"Making {adminId.Value} admin");
                        server.Server.Database.AdminCharacter(adminId.Value).Wait();
                        server.Server.AdminChanged(adminId.Value, true);
                        admins = server.Server.Database.GetAdmins();
                        adminSearchString = "";
                    }
                }
                foreach (var a in admins)
                {
                    ImGui.Separator();
                    ImGui.TextUnformatted(a.Name);
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
                    banId = server.Server.Database.FindAccount(banSearchString);
                if (banId != null)
                {
                    ImGui.TextUnformatted($"Found account: {banId}");
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
                    ImGui.TextUnformatted($"Id: {b.AccountId}");
                    ImGui.SameLine();
                    if (ImGui.Button("Unban##" + b.AccountId))
                    {
                        server.Server.Database.UnbanAccount(b.AccountId);
                        FLLog.Info("Server", $"Unbanned account {b.AccountId}");
                        ImGui.CloseCurrentPopup();
                    }
                    ImGui.TextUnformatted($"Ban Expiry: {b.Expiry.ToLocalTime()}");
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
            ImGui.TextUnformatted($"Server Running on Port {server.Server.Listener.Port}");
            ImGui.TextUnformatted(
                $"Players Connected: {server.Server.Listener.Server.ConnectedPeersCount}/{server.Server.Listener.MaxConnections}");
            float* values = stackalloc float[ServerPerformance.MAX_TIMING_ENTRIES];
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
            ImGui.TextUnformatted($"Update Time: (Avg: {avg:F4}ms/Min: {min:F4}ms/Max: {max:F4}ms)");
            ImGui.PlotLines("##updatetime", ref values[0], len, 0, "", 0, (float)Math.Max(max, 17),
                new Vector2(400, 150));
        }
        log.Draw();
    }
}
