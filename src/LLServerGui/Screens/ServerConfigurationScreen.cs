using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using ImGuiNET;
using LibreLancer;
using LibreLancer.Data;
using LibreLancer.ImUI;
using Microsoft.Win32;

namespace LLServer.Screens;


public class ServerConfigurationScreen : Screen
{
    public ServerConfigurationScreen(MainWindow window, ScreenManager screens, PopupManager popups) : base(screens, popups)
    {
        this.window = window;

        configPath = Path.Combine(Platform.GetBasePath(), "llserver.json");
    }

    private readonly MainWindow window;

    private ServerConfig config;
    private string configPath;
    private bool startupError;

    public override void OnEnter()
    {
        config = GetConfigFromFileOrDefault();
        startupError = false;

        base.OnEnter();
    }

    public override void Draw(double elapsed)
    {
        ImGui.PushFont(ImGuiHelper.Roboto, 32);
        ImGui.Text("Server Configuration");
        ImGui.PopFont();

        ImGui.NewLine();
        ImGui.PushItemWidth(540);

        InputTextLabel("Server Name", "##serverName", ref config.ServerName);

        ImGui.Text("Server Description");
        ImGui.InputTextMultiline(
            "##description",
            ref config.ServerDescription,
            4096,
            ImGui.GetContentRegionAvail()
        );

        InputTextLabel("Freelancer Path", "##flpath", ref config.FreelancerPath);
        InputTextLabel("Database File", "##dbfile", ref config.DatabasePath);
        InputTextLabel("Configuration File", "##configfile", ref configPath);

        ImGui.PopItemWidth();
        ImGui.NewLine();

        if (startupError)
        {
            ImGui.TextColored(
                new System.Numerics.Vector4(1, 0, 0, 1),
                "Server startup failed"
            );
        }

        if (ImGui.Button("Launch"))
        {
            LaunchServer();
        }
    }

    public override void OnExit()
    {
        base.OnExit();
    }

    ServerConfig GetConfigFromFileOrDefault()
    {
        ServerConfig config = new ServerConfig();

        if (File.Exists(configPath))
            config = JSON.Deserialize<ServerConfig>(File.ReadAllText(configPath));
        else
        {
            config = new ServerConfig();

            if (string.IsNullOrEmpty(config.FreelancerPath))
            {
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
            }
            config.ServerName = "M9Universe";
            config.ServerDescription = "My Cool Freelancer server";
            config.DatabasePath = Path.Combine(Platform.GetBasePath(), "llserver.db");
        }

        return config;
    }

    void LaunchServer()
    {
        Task.Run(() =>
        {
            if (!window.StartServer(config))
            {
                window.QueueUIThread(() => startupError = true);
            }
            else
            {
                File.WriteAllText(configPath, JSON.Serialize(config));

                window.QueueUIThread(() =>
                {
                    Screens.SetScreen(
                        new RunningServerScreen(window, Screens, Popups)
                    );
                });
            }
        });
    }

    void InputTextLabel(string label, string id, ref string text)
    {
        ImGui.AlignTextToFramePadding();
        ImGui.Text(label);
        ImGui.InputText(id, ref text, 4096);
    }

}
