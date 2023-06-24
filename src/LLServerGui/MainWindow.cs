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
    public MainWindow() : base(800,600,false)
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
        FileDialog.RegisterParent(this);
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
    private RenderTarget2D lastFrame;

    protected override void Draw(double elapsed)
    {
        if(!guiRender.DoRender(elapsed))
        {
            if(Width !=0 && Height != 0 && lastFrame != null)
                lastFrame.BlitToScreen();
            WaitForEvent(); //Yield like a regular GUI program
            return;
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
        if (Width != 0 && Height != 0)
        {
            if (lastFrame == null ||
                lastFrame.Width != Width ||
                lastFrame.Height != Height)
            {
                if (lastFrame != null) lastFrame.Dispose();
                lastFrame = new RenderTarget2D(Width, Height);
            }
            RenderContext.RenderTarget = lastFrame;
            RenderContext.ClearColor = new Color4(0.2f, 0.2f, 0.2f, 1f);
            RenderContext.ClearAll();
            guiRender.Render(RenderContext);
            RenderContext.RenderTarget = null;
            lastFrame.BlitToScreen();
        }
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
                } else {
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

    private BannedPlayerDescription[] BannedPlayers;
    private Guid? banId;
    private string banSearchString;

    bool BeginModalWithClose(string id, ImGuiWindowFlags? flags = null)
    {
        bool x = true;
        if (flags.HasValue)
            return ImGui.BeginPopupModal(id, ref x, flags.Value);
        return ImGui.BeginPopupModal(id, ref x);
    }

    void RunningServer()
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
                if(BannedPlayers.Length == 0)
                    ImGui.Text("There are no banned players");
                ImGui.BeginChild("##banned");
                foreach (var b in BannedPlayers)
                {
                    ImGui.TextUnformatted($"Id: {b.AccountId}");
                    ImGui.SameLine();
                    if (ImGui.Button("Unban"))
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
                BannedPlayers = server.Server.Database.GetBannedPlayers();
                ImGui.OpenPopup("Unban");
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
        }
        log.Draw();
    }
}