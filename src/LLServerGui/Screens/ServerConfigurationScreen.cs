using System.IO;
using System.Numerics;
using ImGuiNET;
using LibreLancer.Data;
using LibreLancer.Dialogs;
using LibreLancer.ImUI;

namespace LLServer.Screens;

public class ServerConfigurationScreen : Screen
{
    readonly MainWindow win;
    readonly ServerConfig config;
    public ServerConfigurationScreen(MainWindow win, ScreenManager sm, PopupManager pm, ServerConfig config) : base(sm, pm)
    {
        this.win = win;
        this.config = config;

    }

    static readonly FileDialogFilters dbInputFilters = new FileDialogFilters(
        new FileFilter("Database File", "db")
        );
    static readonly FileDialogFilters inputFilters = new FileDialogFilters(
        new FileFilter("Config File", "json")
        );
    static readonly FileDialogFilters lrpkFilter = new FileDialogFilters(
        new FileFilter("Lancer Pack File", "lrpk")
        );

    static readonly float LABEL_WIDTH = 135f;
    static readonly float BUTTON_WIDTH = 110f;
    readonly Vector4 ERROR_TEXT_COLOUR = new Vector4(1f, 0.3f, 0.3f, 1f);
    readonly Vector4 SUCCESS_TEXT_COLOUR = new Vector4(0f, 0.8f, 0.2f, 1f);

    string _portInputBuffer;

    public override void OnEnter()
    {
        Title = "Server Configuration";
        //config = GetConfigFromFileOrDefault();
        win.StartupError = false;

        base.OnEnter();
    }

    public override void Draw(double elapsed)
    {
        ImGui.PushFont(ImGuiHelper.Roboto, 32);
        ImGuiHelper.CenterText("Server Configuration");
        ImGui.PopFont();

        ImGui.NewLine();
        ImGui.Separator();
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
        ImGui.Text("Login URL"); ImGui.SameLine(LABEL_WIDTH * ImGuiHelper.Scale);
        ImGui.PushItemWidth(-1); ImGui.InputText("##loginUrl", ref config.LoginUrl, 4096);

        ImGui.AlignTextToFramePadding();
        ImGui.Text("Listening Port"); ImGui.SameLine(LABEL_WIDTH * ImGuiHelper.Scale);
        ImGui.PushItemWidth(BUTTON_WIDTH * ImGuiHelper.Scale);
        ref string portInput = ref _portInputBuffer;
        portInput ??= config.Port.ToString();
        if (ImGui.InputText("##serverPort", ref portInput, 6, ImGuiInputTextFlags.CharsDecimal))
        {
            if (ushort.TryParse(portInput, out ushort port))
            {
                config.Port = port;
            }
        }
        ImGui.NewLine();
        ImGui.Separator();
        ImGui.NewLine();

        ImGui.Text("Use .lprk File?"); ImGui.SameLine(LABEL_WIDTH * ImGuiHelper.Scale);
        ImGui.Checkbox("##useLrpk", ref config.UseLrpkFile);

        if (!config.UseLrpkFile)
        {
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
        }
        else
        {
            ImGui.AlignTextToFramePadding();
            ImGui.Text("Freelancer .lrpk Path"); ImGui.SameLine(LABEL_WIDTH * ImGuiHelper.Scale);
            if (ImGui.Button("Select File", new Vector2(BUTTON_WIDTH * ImGuiHelper.Scale, 0)))
            {
                win.QueueUIThread(() =>
                {
                    FileDialog.Open(file =>
                    {
                        if (file == null || file.Length == 0)
                        {
                            return;
                        }
                        config.LrpkFilePath = file;
                    }, lrpkFilter);
                });
            }
            ImGui.SameLine(); ImGui.PushItemWidth(-1); ImGui.InputText("##lrpkPath", ref config.LrpkFilePath, 4096);
        }

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
                    config.DatabasePath = filepath;

                },
                dbInputFilters);
            });
        }
        ImGui.SameLine(); ImGui.PushItemWidth(-1); ImGui.InputText("##dbfile", ref config.DatabasePath, 4096);

        ImGui.AlignTextToFramePadding();
        ImGui.Text("Configuration File"); ImGui.SameLine(LABEL_WIDTH * ImGuiHelper.Scale);
        ImGui.PushItemWidth(-1); ImGui.InputText("##configfile", ref win.ConfigPath, 4096, ImGuiInputTextFlags.ReadOnly);

        ImGui.NewLine();
        ImGui.Spacing();
        ImGui.Separator();
        ImGui.Spacing();
        if (ImGui.Button("Load Config", new Vector2(BUTTON_WIDTH * ImGuiHelper.Scale, 0)))
        {
            win.QueueUIThread(() =>
            {
                FileDialog.Open(filepath =>
                {
                    if (filepath == null || filepath.Length == 0)
                    {
                        return;
                    }
                    //save local config 
                    config.lastConfigPath = filepath;
                    File.WriteAllText(win.ConfigPath, JSON.Serialize(config));

                    win.ConfigPath = filepath;
                    var newConfig = win.GetConfigFromFileOrDefault(filepath);
                    config.CopyFrom(newConfig);
                },
                inputFilters);
            });
        }
        ImGui.SameLine();
        ImGui.Dummy(new Vector2(ImGui.GetContentRegionAvail().X - BUTTON_WIDTH - 10 * ImGuiHelper.Scale, ImGui.GetFrameHeight()));
        ImGui.SameLine();
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
            ImGuiHelper.CenterText("Server Startup Error", ERROR_TEXT_COLOUR);
            ImGui.EndChild();
        }

    }
    public override void OnExit()
    {
    }
    void LaunchServer()
    {

        File.WriteAllText(win.ConfigPath, JSON.Serialize(config));

        win.QueueUIThread(() =>
        {
            sm.SetScreen(
                new RunningServerScreen(win, sm, pm, config)
            );
        });

    }
}
