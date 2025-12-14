using System;
using System.Collections.Generic;
using System.IO;
using System.Numerics;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;
using ImGuiNET;
using LibreLancer;
using LibreLancer.Data;
using LibreLancer.Dialogs;
using LibreLancer.ImUI;
using LibreLancer.Server.Ai;
using Microsoft.Win32;
using static System.Net.Mime.MediaTypeNames;

namespace LLServer.Screens;


public class ServerConfigurationScreen : Screen
{
    public ServerConfigurationScreen(MainWindow win, ScreenManager sm, PopupManager pm) : base(sm, pm)
    {
        this.win = win;

        win.ConfigPath = Path.Combine(Platform.GetBasePath(), "llserver.json");
    }

    static readonly FileDialogFilters dbInputFilters = new FileDialogFilters(
        new FileFilter("Database File", "db")
        );
    static readonly FileDialogFilters inputFilters = new FileDialogFilters(
        new FileFilter("Config File", "json")
        );

    static readonly float LABEL_WIDTH = 125f;
    static readonly float BUTTON_WIDTH = 110f;
    readonly Vector4 ERROR_TEXT_COLOUR = new Vector4(1f, 0.3f, 0.3f, 1f);
    readonly Vector4 SUCCESS_TEXT_COLOUR = new Vector4(0f, 0.8f, 0.2f, 1f);

    readonly MainWindow win;
    ServerConfig config;

    public override void OnEnter()
    {
        config = GetConfigFromFileOrDefault();
        win.StartupError = false;

        base.OnEnter();
    }

    public override void Draw(double elapsed)
    {
        ImGui.PushFont(ImGuiHelper.Roboto, 32);
        CenterText("Server Configuration");
        ImGui.PopFont();

        ImGui.NewLine();
        ImGui.AlignTextToFramePadding();
        ImGui.Text("Server Name"); ImGui.SameLine(LABEL_WIDTH * ImGuiHelper.Scale);
        ImGui.PushItemWidth(-1); ImGui.InputText("##serverName", ref config.ServerName, 4096);

        ImGui.AlignTextToFramePadding();
        ImGui.Text("Server Description"); ImGui.SameLine(LABEL_WIDTH * ImGuiHelper.Scale);
        ImGui.PushItemWidth(-1);
        ImGui.InputTextMultiline(
            "##description",
            ref config.ServerDescription,
            4096,
            new Vector2(0, ImGui.GetFrameHeight() * 4 * ImGuiHelper.Scale)
        );

        ImGui.AlignTextToFramePadding();
        ImGui.Text("Freelancer Path"); ImGui.SameLine(LABEL_WIDTH * ImGuiHelper.Scale);
        if (ImGui.Button("Select Folder", new Vector2(BUTTON_WIDTH * ImGuiHelper.Scale, 0)))
        {
            win.QueueUIThread(() =>
            {
                FileDialog.ChooseFolder(folder =>
                {
                    if (folder == null || folder.Length == 0)
                    {
                        return;
                    }
                    config.FreelancerPath = folder;

                });
            });
        }
        ImGui.SameLine(); ImGui.PushItemWidth(-1); ImGui.InputText("##flpath", ref config.FreelancerPath, 4096);

        ImGui.AlignTextToFramePadding();
        ImGui.Text("Database File"); ImGui.SameLine(LABEL_WIDTH * ImGuiHelper.Scale);
        if (ImGui.Button("Select File##db", new Vector2(BUTTON_WIDTH * ImGuiHelper.Scale, 0)))
        {
            win.QueueUIThread(() =>
            {
                FileDialog.Open(filepath =>
                {
                    if (filepath == null || filepath.Length == 0)
                    {
                        return;
                    }
                    config.FreelancerPath = filepath;

                },
                dbInputFilters);
            });
        }
        ImGui.SameLine(); ImGui.PushItemWidth(-1); ImGui.InputText("##dbfile", ref config.DatabasePath, 4096);

        ImGui.AlignTextToFramePadding();
        ImGui.Text("Configuration File"); ImGui.SameLine(LABEL_WIDTH * ImGuiHelper.Scale);
        if (ImGui.Button("Select File##config", new Vector2(BUTTON_WIDTH * ImGuiHelper.Scale, 0)))
        {
            win.QueueUIThread(() =>
            {
                FileDialog.Open(filepath =>
                {
                    if (filepath == null || filepath.Length == 0)
                    {
                        return;
                    }
                    config.FreelancerPath = filepath;
                },
                inputFilters);
            });
        }
        ImGui.SameLine(); ImGui.PushItemWidth(-1); ImGui.InputText("##configfile", ref win.ConfigPath, 4096);

        ImGui.Spacing();
        ImGui.Separator();
        ImGui.Spacing();

        if (ImGui.Button("Launch Server", new Vector2(BUTTON_WIDTH * ImGuiHelper.Scale, 0)))
        {
            win.QueueUIThread(() =>
            {
                LaunchServer();
            });
        }
        ImGui.Spacing();
        ImGui.Separator();
        ImGui.Spacing();
        if (win.StartupError)
        {
            ImGui.Dummy(new Vector2(0, ImGui.GetContentRegionAvail().Y - ImGui.GetFrameHeightWithSpacing() - 5 * ImGuiHelper.Scale));
            ImGui.BeginChild("startupError", new Vector2(0, ImGui.GetFrameHeightWithSpacing() * ImGuiHelper.Scale), ImGuiChildFlags.None, ImGuiWindowFlags.NoScrollbar);
            CenterText("Server Startup Error", ERROR_TEXT_COLOUR);
            ImGui.EndChild();
        }

    }

    public override void OnExit()
    {
    }

    ServerConfig GetConfigFromFileOrDefault()
    {
        ServerConfig config = new ServerConfig();

        if (File.Exists(win.ConfigPath))
            config = JSON.Deserialize<ServerConfig>(File.ReadAllText(win.ConfigPath));
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
            File.WriteAllText(win.ConfigPath, JSON.Serialize(config));

            win.QueueUIThread(() =>
            {
                Screens.SetScreen(
                    new RunningServerScreen(win, Screens, Popups, config)
                );
            });
        });
    }

    void CenterText(string text)
    {
        ImGui.Dummy(new Vector2(1));
        var win = ImGui.GetWindowWidth();
        var txt = ImGui.CalcTextSize(text).X;
        ImGui.SameLine(Math.Max((win / 2f) - (txt / 2f), 0));
        ImGui.Text(text);
    }
    void CenterText(string text, Vector4 colour)
    {
        ImGui.Dummy(new Vector2(1));
        var win = ImGui.GetWindowWidth();
        var txt = ImGui.CalcTextSize(text).X;
        ImGui.SameLine(Math.Max((win / 2f) - (txt / 2f), 0));
        ImGui.TextColored(ERROR_TEXT_COLOUR, text);

    }
}
