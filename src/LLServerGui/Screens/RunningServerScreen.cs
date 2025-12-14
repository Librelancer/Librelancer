using System;
using System.Collections.Generic;
using System.Numerics;
using System.Threading.Tasks;
using ImGuiNET;
using LibreLancer.ImUI;
using LibreLancer.Server;

namespace LLServer.Screens;
public class RunningServerScreen : Screen
{
    readonly MainWindow win;
    readonly ServerConfig config;
    public RunningServerScreen(MainWindow win, ScreenManager sm, PopupManager pm, ServerConfig config)
        : base(sm, pm)
    {
        this.win = win;
        this.config = config;
    }

    readonly Vector4 ERROR_TEXT_COLOUR = new Vector4(1f, 0.3f, 0.3f, 1f);
    Vector4 WARN_TEXT_COLOUR = new Vector4(1f, 0.86f, 0.25f, 1f);
    readonly Vector4 SUCCESS_TEXT_COLOUR = new Vector4(0f, 0.8f, 0.2f, 1f);
    static readonly float LABEL_WIDTH = 125f;

    private bool showStopConfirm;
    ConnectedCharacterDescription[] connectedPlayers;

    public override void OnEnter()
    {
        Title = "Server Dashboard";

        if (!win.StartServer(config))
        {
            win.QueueUIThread(() =>
                sm.SetScreen(new ServerConfigurationScreen(win, sm, pm, config))
            );
        }
    }

    public override void OnExit()
    {
        win.StopServer();
    }

    public override void Draw(double elapsed)
    {
        RefreshDbData();

        ImGui.PushFont(ImGuiHelper.Roboto, 32);
        if (win.Server == null || !win.IsRunning)
        {
            CenterText(config.ServerName, ERROR_TEXT_COLOUR);
        }
        else if (!win.ServerReady)
        {
            CenterText(config.ServerName, WARN_TEXT_COLOUR);
        }
        else
        {
            CenterText(config.ServerName, SUCCESS_TEXT_COLOUR);

        }
        ImGui.PopFont();
        ImGui.Spacing();
        ImGui.Separator();

        ImGui.PushItemWidth(-1);
        if (ImGui.BeginTable(
            "server_stats_layout",
            3,
            ImGuiTableFlags.SizingStretchProp))
        {
            ImGui.TableSetupColumn("stats", ImGuiTableColumnFlags.WidthFixed);
            ImGui.TableSetupColumn("spacing", ImGuiTableColumnFlags.WidthFixed);
            ImGui.TableSetupColumn("graph", ImGuiTableColumnFlags.WidthStretch);

            ImGui.TableNextColumn();
            DrawServerStats();

            ImGui.TableNextColumn();
            ImGui.Dummy(Vector2.One);

            ImGui.TableNextColumn();
            DrawPerformanceStats();

            ImGui.EndTable();
        }

        ImGui.Spacing();
        ImGui.Separator();
        ImGui.Spacing();

        if (ImGui.BeginTabBar("server_tabs"))
        {
            if (ImGui.BeginTabItem("Connected Players"))
            {
                DrawPlayersTab();
                ImGui.EndTabItem();
            }

            if (ImGui.BeginTabItem("Admins"))
            {
                //DrawAdminsAndBansTab();
                ImGui.EndTabItem();
            }
            if (ImGui.BeginTabItem("Bans"))
            {
                //DrawAdminsAndBansTab();
                ImGui.EndTabItem();
            }

            ImGui.EndTabBar();
        }

        DrawActions();
    }

    private void RefreshDbData()
    {
        if(win.IsRunning && win.ServerReady)
            connectedPlayers = win.Server.Server.Database.GetConnectedCharacters();
    }

    private void DrawActions()
    {
        if (ImGui.Button("Stop Server"))
        {

            pm.MessageBox(
                title: "Confirm Stop",
                message: "The server is currently running.\n\nStop the server and return to configuration?",
                multiline: false,
                buttons: MessageBoxButtons.YesNo,
                callback: response =>
                {
                    if (response == MessageBoxResponse.Yes)
                    {
                        win.QueueUIThread(() =>
                        {
                            sm.SetScreen(
                                new ServerConfigurationScreen(win, sm, pm, config)
                            );
                        });
                    }
                }
            );

        }
    }

    private void DrawServerStats()
    {
        ImGui.Text("Status:"); ImGui.SameLine(LABEL_WIDTH * ImGuiHelper.Scale);
        if(win.Server == null || !win.IsRunning)
        {
            ImGui.TextColored(ERROR_TEXT_COLOUR, "Not running"); ImGui.SameLine(LABEL_WIDTH * ImGuiHelper.Scale);
        }
        else if(!win.ServerReady)
        {
            ImGui.TextColored(WARN_TEXT_COLOUR, "Starting"); ImGui.SameLine(LABEL_WIDTH * ImGuiHelper.Scale);
        }
        else
        {
            ImGui.TextColored(SUCCESS_TEXT_COLOUR, "Running"); ImGui.SameLine(LABEL_WIDTH * ImGuiHelper.Scale);

        }
        ImGui.NewLine();
        ImGui.Text("Listening Port:"); ImGui.SameLine(LABEL_WIDTH * ImGuiHelper.Scale);
        ImGui.Text(win.Port.ToString()); ImGui.SameLine(LABEL_WIDTH * ImGuiHelper.Scale);
        ImGui.NewLine();
        ImGui.Separator();
        ImGui.Text("Players Connected"); ImGui.SameLine(LABEL_WIDTH * ImGuiHelper.Scale);
        ImGui.Text(win.ConnectedPlayersCount.ToString()); ImGui.SameLine(LABEL_WIDTH * ImGuiHelper.Scale);
        ImGui.NewLine();
        ImGui.Text("Banned Players"); ImGui.SameLine(LABEL_WIDTH * ImGuiHelper.Scale);
        ImGui.Text("-"); ImGui.SameLine(LABEL_WIDTH * ImGuiHelper.Scale);
        ImGui.NewLine();
    }

    private void DrawPerformanceStats()
    {
        var perf = win.ServerPerformance;
        if (perf == null)
            return;

        Span<float> values = stackalloc float[ServerPerformance.MAX_TIMING_ENTRIES];
        int len = perf.Timings.Count;

        double avg = 0;
        double max = 0;
        double min = double.MaxValue;

        for (int i = 0; i < len; i++)
        {
            double v = perf.Timings[i];
            avg += v;
            max = Math.Max(max, v);
            min = Math.Min(min, v);
            values[i] = (float)v;
        }

        avg /= len;

        ImGui.Text(
            $"Update Time: Avg {avg:F4}ms | Min {min:F4}ms | Max {max:F4}ms"
        );
        ImGui.Spacing();
        ImGui.PushItemWidth(-1);

        ImGui.PlotLines(
            "##updatetime",
            ref values[0],
            len,
            0,
            "",
            0,
            (float)Math.Max(max, 17),
            new Vector2(-1, 150 * ImGuiHelper.Scale)
        );
    }
    void DrawPlayersTab()
    {
        float tableHeight = ImGui.GetContentRegionAvail().Y - ImGui.GetFrameHeightWithSpacing() * ImGuiHelper.Scale;
        ImGui.BeginChild("connected_players_child", new Vector2(0, tableHeight), ImGuiChildFlags.Borders);

        if (ImGui.BeginTable(
            "connected_players",
            8,
            ImGuiTableFlags.Borders |
            ImGuiTableFlags.RowBg |
            ImGuiTableFlags.Resizable |
            ImGuiTableFlags.ScrollY ,
            new Vector2(0, 250 * ImGuiHelper.Scale)
        ))
        {
            ImGui.TableSetupColumn("Id", ImGuiTableColumnFlags.WidthFixed, 200 * ImGuiHelper.Scale);
            ImGui.TableSetupColumn("Name", ImGuiTableColumnFlags.WidthFixed, 200 * ImGuiHelper.Scale);
            ImGui.TableSetupColumn("System", ImGuiTableColumnFlags.WidthFixed, 200 * ImGuiHelper.Scale);
            ImGui.TableSetupColumn("Last Docked Base", ImGuiTableColumnFlags.WidthFixed, 200 * ImGuiHelper.Scale);
            ImGui.TableSetupColumn("Admin", ImGuiTableColumnFlags.WidthFixed, 60 * ImGuiHelper.Scale);
            ImGui.TableSetupColumn("Make Admin", ImGuiTableColumnFlags.WidthFixed, 100 * ImGuiHelper.Scale);
            ImGui.TableSetupColumn("Ban", ImGuiTableColumnFlags.WidthFixed, 80 * ImGuiHelper.Scale);
            ImGui.TableSetupColumn("Inspect", ImGuiTableColumnFlags.WidthFixed, 80 * ImGuiHelper.Scale);
            ImGui.TableHeadersRow();

            foreach (var player in connectedPlayers)
            {
                ImGui.TableNextRow();

                ImGui.TableNextColumn();
                ImGui.Text(player.Id.ToString());

                ImGui.TableNextColumn();
                ImGui.Text(player.Name);

                ImGui.TableNextColumn();
                ImGui.Text(player.System);

                ImGui.TableNextColumn();
                ImGui.Text(player.LastDockedBase);

                ImGui.TableNextColumn();
                ImGui.Text(player.IsAdmin ? Icons.Check.ToString() : "");

                ImGui.TableNextColumn();
                if (!player.IsAdmin && ImGui.SmallButton($"{Icons.ArrowUp.ToString()}##{player.Name}"))
                {
                    pm.MessageBox("Confirm", $"Are you sure you want to promote {player.Name} to an admin?", false, MessageBoxButtons.YesNo, response => {
                        if (response == MessageBoxResponse.Yes)
                        {
                            //PromotePlayer(player.Name);
                        }
                    });
                }

                ImGui.TableNextColumn();
                if (ImGui.SmallButton($"{Icons.Fire.ToString()}##{player.Name}"))
                {
                    //OpenBanPopup(player.Name);
                }
                ImGui.TableNextColumn();
                if (ImGui.SmallButton($"{Icons.Eye}##{player.Name}"))
                {
                    //OpenBanPopup(player.Name);
                }
            }

            ImGui.EndTable();
            
        }
        ImGui.EndChild();


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
        ImGui.TextColored(colour, text);

    }
}
