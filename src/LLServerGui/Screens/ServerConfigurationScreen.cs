using System.IO;
using System.Numerics;
using ImGuiNET;
using LibreLancer.Data;
using LibreLancer.Dialogs;
using LibreLancer.ImUI;

namespace LLServer.Screens;

public class ServerConfigurationScreen(
    MainWindow win,
    ScreenManager sm,
    PopupManager pm,
    ServerConfig config,
    LLServerGuiConfig guiConfig)
    : Screen(sm, pm)
{
    private static readonly FileDialogFilters _dbInputFilters = new FileDialogFilters(
        new FileFilter("Database File", "db")
        );

    private static readonly FileDialogFilters _inputFilters = new FileDialogFilters(
        new FileFilter("Config File", "json")
        );

    private static readonly FileDialogFilters _lrpkFilter = new FileDialogFilters(
        new FileFilter("Lancer Pack File", "lrpk")
        );

    private string? portInputBuffer;

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
        ImGuiExt.CenterText("Server Configuration");
        ImGui.PopFont();

        ImGui.NewLine();
        ImGui.Separator();
        ImGui.NewLine();

        ImGui.AlignTextToFramePadding();
        ImGui.Text("Server Name"); ImGui.SameLine(Theme.LabelWidthMedium);
        ImGui.PushItemWidth(-1); ImGui.InputText("##serverName", ref config.ServerName, 4096);

        ImGui.AlignTextToFramePadding();
        ImGui.Text("Server Description"); ImGui.SameLine(Theme.LabelWidthMedium);
        ImGui.PushItemWidth(-1);
        ImGui.InputTextMultiline(
            "##description",
            ref config.ServerDescription,
            4096,
            new Vector2(0, ImGui.GetFrameHeight() * 4)
        );

        ImGui.AlignTextToFramePadding();
        ImGui.Text("Login URL"); ImGui.SameLine(Theme.LabelWidthMedium);
        ImGui.PushItemWidth(-1); ImGui.InputText("##loginUrl", ref config.LoginUrl, 4096);

        ImGui.AlignTextToFramePadding();
        ImGui.Text("Listening Port"); ImGui.SameLine(Theme.LabelWidthMedium);
        ImGui.PushItemWidth(Theme.ButtonWidth);
        ref string portInput = ref portInputBuffer;
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

        // TODO: add this back in when lrpk support is ready
        ImGui.AlignTextToFramePadding();
        ImGui.Text("Freelancer Path"); ImGui.SameLine(Theme.LabelWidthMedium);
        if (ImGui.Button("Choose Folder", new Vector2(Theme.ButtonWidth, 0)))
        {
            win.QueueUIThread(() =>
            {
                FileDialog.ChooseFolder(folder =>
                {
                    if (string.IsNullOrEmpty(folder))
                    {
                        return;
                    }
                    config.FreelancerPath = folder;

                });
            });
        }
        /* LRPK UI - to be enabled at a later date
        ImGui.SameLine();
        ImGui.AlignTextToFramePadding();
        ImGui.Text("Freelancer .lrpk Path"); ImGui.SameLine(Theme.LABEL_WIDTH_MEDIUM);
        if (ImGui.Button("Choose .lrpk", new Vector2(Theme.BUTTON_WIDTH, 0)))
        {
            win.QueueUIThread(() =>
            {
                FileDialog.Open(file =>
                {
                    if (file == null || file.Length == 0)
                    {
                        return;
                    }
                    config.FreelancerPath = file;
                }, lrpkFilter);
            });
        }
        */
        ImGui.SameLine(); ImGui.PushItemWidth(-1); ImGui.InputText("##flpath", ref config.FreelancerPath, 4096);


        ImGui.AlignTextToFramePadding();
        ImGui.Text("Database File"); ImGui.SameLine(Theme.LabelWidthMedium);
        if (ImGui.Button("Select File##db", new Vector2(Theme.ButtonWidth, 0)))
        {
            win.QueueUIThread(() =>
            {
                FileDialog.Open(filepath =>
                {
                    if (string.IsNullOrEmpty(filepath))
                    {
                        return;
                    }
                    config.DatabasePath = filepath;

                },
                _dbInputFilters);
            });
        }
        ImGui.SameLine(); ImGui.PushItemWidth(-1); ImGui.InputText("##dbfile", ref config.DatabasePath, 4096);

        ImGui.AlignTextToFramePadding();
        ImGui.Text("Configuration File"); ImGui.SameLine(Theme.LabelWidthMedium);
        ImGui.PushItemWidth(-1); ImGui.InputText("##configfile", ref win.ServerGuiConfig.LastConfigPath, 4096, ImGuiInputTextFlags.ReadOnly);

        ImGui.AlignTextToFramePadding();
        ImGui.Text("Auto Start Server"); ImGui.SameLine(Theme.LabelWidthMedium);
        ImGui.Checkbox("##autoStart", ref guiConfig.AutoStartServer);

        ImGui.NewLine();
        ImGui.Spacing();
        ImGui.Separator();
        ImGui.Spacing();
        if (ImGui.Button("Load Config", new Vector2(Theme.ButtonWidth, 0)))
        {
            win.QueueUIThread(() =>
            {
                FileDialog.Open(filepath =>
                {
                    if (string.IsNullOrEmpty(filepath))
                    {
                        return;
                    }
                    //save local config

                    guiConfig.LastConfigPath = filepath;
                    win.SaveServerGuiConfig();

                    var newConfig = win.GetServerConfigFromFileOrDefault(filepath);
                    config.CopyFrom(newConfig);
                },
                _inputFilters);
            });
        }
        ImGui.SameLine();
        ImGui.Dummy(new Vector2(ImGui.GetContentRegionAvail().X - Theme.ButtonWidth - 10 * ImGuiHelper.Scale, ImGui.GetFrameHeight()));
        ImGui.SameLine();
        if (ImGui.Button("Launch Server", new Vector2(Theme.ButtonWidth, 0)))
        {
            win.QueueUIThread(LaunchServer);
        }

        ImGui.Spacing();
        ImGui.Separator();
        ImGui.Spacing();

        if (!win.StartupError)
        {
            return;
        }

        ImGui.Dummy(new Vector2(0, ImGui.GetContentRegionAvail().Y - ImGui.GetFrameHeightWithSpacing() - 5 * ImGuiHelper.Scale));
        ImGui.BeginChild("startupError", new Vector2(0, ImGui.GetFrameHeightWithSpacing() * ImGuiHelper.Scale), ImGuiChildFlags.None, ImGuiWindowFlags.NoScrollbar);
        ImGuiExt.CenterText("Server Startup Error", Theme.ErrorTextColor);
        ImGui.EndChild();

    }
    public override void OnExit()
    {
    }

    private void LaunchServer()
    {

        File.WriteAllText(win.ServerGuiConfig.LastConfigPath, JSON.Serialize(config));

        win.QueueUIThread(() =>
        {
            sm.SetScreen(
                new RunningServerScreen(win, sm, pm, config, guiConfig)
            );
        });

    }
}
