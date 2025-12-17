using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;
using ImGuiNET;
using LibreLancer;
using LibreLancer.ImUI;
using LibreLancer.Server;

namespace LLServer.Screens;
public class RunningServerScreen : Screen
{
    readonly MainWindow win;
    readonly ServerConfig config;
    readonly LLServerGuiConfig guiConfig;
    public RunningServerScreen(MainWindow win, ScreenManager sm, PopupManager pm, ServerConfig config, LLServerGuiConfig guiConfig)
        : base(sm, pm)
    {
        this.win = win;
        this.config = config;
        this.guiConfig = guiConfig;
    }

    readonly Vector4 ERROR_TEXT_COLOUR = new Vector4(1f, 0.3f, 0.3f, 1f);
    Vector4 WARN_TEXT_COLOUR = new Vector4(1f, 0.86f, 0.25f, 1f);
    readonly Vector4 SUCCESS_TEXT_COLOUR = new Vector4(0f, 0.8f, 0.2f, 1f);
    static readonly float LABEL_WIDTH = 125f;

    bool showStopConfirm;
    bool isStarting = true;
    IEnumerable<Player> connectedPlayers;
    BannedPlayerDescription[] bannedPlayers;
    AdminCharacterDescription[] admins;

    public override void OnEnter()
    {
        Title = "Server Dashboard";

        if (!win.StartServer(config))
        {
            win.StartupError = true;
            win.QueueUIThread(() =>
                sm.SetScreen(new ServerConfigurationScreen(win, sm, pm, config, guiConfig))
            );
        }
        win.StartupError = false;

    }
    public override void OnExit()
    {
        win.StopServer();
    }
    public override void Draw(double elapsed)
    {
        RefreshData();

        ImGui.PushFont(ImGuiHelper.Roboto, 32);

        if (win.Server == null || (!win.IsRunning && !isStarting))
        {
            ImGuiExt.CenterText(config.ServerName, ERROR_TEXT_COLOUR);
        }
        else if (isStarting)
        {
            ImGuiExt.CenterText(config.ServerName, WARN_TEXT_COLOUR);
        }
        else
        {
            ImGuiExt.CenterText(config.ServerName, SUCCESS_TEXT_COLOUR);
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
                DrawConnectedPlayersTab();
                ImGui.EndTabItem();
            }
            if (ImGui.BeginTabItem("Banned Players"))
            {
                DrawBansTab();
                ImGui.EndTabItem();
            }
            if (ImGui.BeginTabItem("Admins"))
            {
                DrawAdminsTab();
                ImGui.EndTabItem();
            }

            ImGui.EndTabBar();
        }

        if (win.IsRunning)
            isStarting = false;
    }

    // Draw Methods
    private void DrawServerStats()
    {
        ImGui.Text("Status:"); ImGui.SameLine(LABEL_WIDTH * ImGuiHelper.Scale);
        if (win.Server == null || (!win.IsRunning && !isStarting))
        {
            ImGui.TextColored(ERROR_TEXT_COLOUR, "Not running"); ImGui.SameLine(LABEL_WIDTH * ImGuiHelper.Scale);
        }
        else if (isStarting)
        {
            ImGui.TextColored(WARN_TEXT_COLOUR, "Starting"); ImGui.SameLine(LABEL_WIDTH * ImGuiHelper.Scale);
        }
        else
        {
            ImGui.TextColored(SUCCESS_TEXT_COLOUR, "Running"); ImGui.SameLine(LABEL_WIDTH * ImGuiHelper.Scale);
        }

        ImGui.NewLine();
        ImGui.Text("Listening Port:"); ImGui.SameLine(LABEL_WIDTH * ImGuiHelper.Scale);
        ImGui.Text(win.Server?.Server?.Listener?.Port.ToString() ?? "-"); ImGui.SameLine(LABEL_WIDTH * ImGuiHelper.Scale);

        ImGui.NewLine();
        ImGui.Separator();

        ImGui.Text("Players Online"); ImGui.SameLine(LABEL_WIDTH * ImGuiHelper.Scale);
        ImGui.Text(win.ConnectedPlayersCount.ToString()); ImGui.SameLine(LABEL_WIDTH * ImGuiHelper.Scale);

        ImGui.NewLine();
        ImGui.Text("Admins Online"); ImGui.SameLine(LABEL_WIDTH * ImGuiHelper.Scale);
        ImGui.Text(win.Server.Server.AllPlayers.Count(p =>
                p.Character != null &&
                admins.Any(a => a.Name == p.Character.Name)).ToString());
        ImGui.SameLine(LABEL_WIDTH * ImGuiHelper.Scale);

        ImGui.NewLine();
        ImGui.Text("Banned Players"); ImGui.SameLine(LABEL_WIDTH * ImGuiHelper.Scale);
        ImGui.Text(bannedPlayers?.Count().ToString() ?? "-"); ImGui.SameLine(LABEL_WIDTH * ImGuiHelper.Scale);

        ImGui.NewLine();
        ImGui.Dummy(new Vector2(-1, ImGui.GetFrameHeight() * ImGuiHelper.Scale));

        if (ImGui.Button("Stop Server", new Vector2(-1, ImGui.GetFrameHeight() * 2 * ImGuiHelper.Scale)))
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
                                new ServerConfigurationScreen(win, sm, pm, config, guiConfig)
                            );
                        });
                    }
                }
            );

        }
    }
    private void DrawPerformanceStats()
    {
        if (isStarting)
            return;
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
    void DrawConnectedPlayersTab()
    {
        ImGui.BeginChild("connected_players_child", new Vector2(0, 0), ImGuiChildFlags.None);

        float tableHeight = ImGui.GetContentRegionAvail().Y - ImGui.GetFrameHeightWithSpacing() * ImGuiHelper.Scale;
        if (ImGui.BeginTable(
            "connected_players",
            8,
            ImGuiTableFlags.Borders |
            ImGuiTableFlags.RowBg |
            ImGuiTableFlags.Resizable |
            ImGuiTableFlags.ScrollY |
            ImGuiTableFlags.Sortable,
            new Vector2(0, 0)
        ))
        {
            ImGui.TableSetupColumn("Id", ImGuiTableColumnFlags.WidthStretch, 200 * ImGuiHelper.Scale);
            ImGui.TableSetupColumn("Name", ImGuiTableColumnFlags.WidthStretch, 200 * ImGuiHelper.Scale);
            ImGui.TableSetupColumn("System", ImGuiTableColumnFlags.WidthStretch, 200 * ImGuiHelper.Scale);
            ImGui.TableSetupColumn("Last Docked Base", ImGuiTableColumnFlags.WidthStretch, 200 * ImGuiHelper.Scale);
            ImGui.TableSetupColumn("Admin", ImGuiTableColumnFlags.WidthFixed | ImGuiTableColumnFlags.NoResize, 60 * ImGuiHelper.Scale);
            ImGui.TableSetupColumn("Promote", ImGuiTableColumnFlags.WidthFixed | ImGuiTableColumnFlags.NoResize, 60 * ImGuiHelper.Scale);
            ImGui.TableSetupColumn("Ban", ImGuiTableColumnFlags.WidthFixed | ImGuiTableColumnFlags.NoResize, 60 * ImGuiHelper.Scale);
            ImGui.TableSetupColumn("Inspect", ImGuiTableColumnFlags.WidthFixed | ImGuiTableColumnFlags.NoResize, 60 * ImGuiHelper.Scale);
            ImGui.TableHeadersRow();


            if (win.IsRunning && connectedPlayers != null)
            {
                foreach (var player in connectedPlayers)
                {
                    bool isAdmin = player.Character != null && admins.Any(a => a.Id == player.Character.ID);
                    var buttonSize = new Vector2(-1, ImGui.GetFrameHeight());

                    ImGui.TableNextRow();

                    ImGui.TableNextColumn();
                    ImGui.AlignTextToFramePadding();
                    ImGui.Text(player?.Character?.ID.ToString()?? "-");

                    ImGui.TableNextColumn();
                    ImGui.AlignTextToFramePadding();
                    ImGui.Text(player.Character?.Name ?? "-");

                    ImGui.TableNextColumn();
                    ImGui.AlignTextToFramePadding();
                    ImGui.Text(player.Character?.System ?? "-");

                    ImGui.TableNextColumn();
                    ImGui.AlignTextToFramePadding();
                    ImGui.Text(player.Character?.Base ?? "-");

                    ImGui.TableNextColumn();
                    ImGui.AlignTextToFramePadding();
                    var icon = isAdmin ? Icons.Check : Icons.X;
                    var colour = isAdmin ? SUCCESS_TEXT_COLOUR : ERROR_TEXT_COLOUR;
                    ImGui.TextColored(colour, icon.ToString());

                    ImGui.TableNextColumn();
                    var uiId = player.Character?.Name ?? "-";
                    if (ImGuiExt.Button($"{Icons.ArrowUp.ToString()}##{uiId}", !isAdmin && uiId != "-", buttonSize))
                    {
                        pm.MessageBox("Confirm", $"Are you sure you want to promote {player.Character.Name} to an admin?", false, MessageBoxButtons.YesNo, response =>
                        {
                            if (response == MessageBoxResponse.Yes)
                            {
                                PromotePlayer(player.Character.ID, player.Character.Name);
                            }
                        });
                    }

                    ImGui.TableNextColumn();
                    if (ImGuiExt.Button($"{Icons.Fire.ToString()}##{player.Name?? "-"}",!String.IsNullOrWhiteSpace(player.Name) && !bannedPlayers.Any(b => b.Characters.Any(c => c == player.Name )), buttonSize))
                    {
                       pm.OpenPopup(new BanPopup(player.Name, expiry =>
                       {
                           if (expiry.HasValue)
                           {
                               BanPlayer(player.Name, expiry.Value);
                           }
                       }));
                    }
                    ImGui.TableNextColumn();
                    if (ImGui.Button($"{Icons.Eye}##{player.Name ?? "-"}", buttonSize))
                    {
                        pm.OpenPopup(new InspectorPopup(player));
                    }
                }
            }
            ImGui.EndTable();
        }
        ImGui.EndChild();
    }
    private void DrawBansTab()
    {
        ImGui.BeginChild("banned_players_child", new Vector2(0, 0), ImGuiChildFlags.None);

        float tableHeight = ImGui.GetContentRegionAvail().Y - ImGui.GetFrameHeightWithSpacing() * ImGuiHelper.Scale;
        if (ImGui.BeginTable(
            "banned_players",
            5,
            ImGuiTableFlags.Borders |
            ImGuiTableFlags.RowBg |
            ImGuiTableFlags.Resizable |
            ImGuiTableFlags.ScrollY |
            ImGuiTableFlags.Sortable,
            new Vector2(0, 0)
        ))
        {
            ImGui.TableSetupColumn("Id", ImGuiTableColumnFlags.WidthStretch, 200 * ImGuiHelper.Scale);
            ImGui.TableSetupColumn("Characters", ImGuiTableColumnFlags.WidthStretch, 200 * ImGuiHelper.Scale);
            ImGui.TableSetupColumn("Ban Expiry", ImGuiTableColumnFlags.WidthStretch, 200 * ImGuiHelper.Scale);
            ImGui.TableSetupColumn("Unban", ImGuiTableColumnFlags.WidthFixed | ImGuiTableColumnFlags.NoResize, 60 * ImGuiHelper.Scale);
            ImGui.TableSetupColumn("Increase Ban", ImGuiTableColumnFlags.WidthFixed | ImGuiTableColumnFlags.NoResize, 100 * ImGuiHelper.Scale);
            ImGui.TableHeadersRow();

            var buttonSize = new Vector2(-1, ImGui.GetFrameHeight());

            if (bannedPlayers != null)
            {
                foreach (var player in bannedPlayers)
                {

                    ImGui.TableNextRow();

                    ImGui.TableNextColumn();
                    ImGui.AlignTextToFramePadding();
                    ImGui.Text(player.AccountId.ToString());

                    ImGui.TableNextColumn();
                    ImGui.AlignTextToFramePadding();
                    ImGui.Text(String.Join(", ", player.Characters));

                    ImGui.TableNextColumn();
                    ImGui.AlignTextToFramePadding();
                    ImGui.Text(player.BanExpiry.HasValue
                            ? player.BanExpiry.Value.ToString("G") // system culture, short date + time
                            : "-");

                    ImGui.TableNextColumn();
                    if (ImGui.Button($"{Icons.Eraser .ToString()}##{player.AccountId.ToString()}", buttonSize))
                    {
                        pm.MessageBox("Confirm", $"Are you sure you want to unban \n{player.AccountId.ToString()}?", false, MessageBoxButtons.YesNo, response =>
                        {
                            if (response == MessageBoxResponse.Yes)
                            {
                                UnbanPlayer(player.AccountId);
                            }
                        });
                    }

                    ImGui.TableNextColumn();
                    if (ImGui.Button($"{Icons.Fire.ToString()}##{player.AccountId.ToString()}", buttonSize))
                    {
                        pm.OpenPopup(new BanPopup(player.AccountId.ToString(), expiry =>
                        {
                            if (expiry.HasValue)
                            {
                                BanPlayer(player.AccountId, expiry.Value);
                            }
                        }));

                    }
                }
            }

            ImGui.EndTable();
        }
        ImGui.EndChild();
    }
    private void DrawAdminsTab()
    {
        ImGui.BeginChild("admin_players_child", new Vector2(0, 0), ImGuiChildFlags.None);

        float tableHeight = ImGui.GetContentRegionAvail().Y - ImGui.GetFrameHeightWithSpacing() * ImGuiHelper.Scale;
        if (ImGui.BeginTable(
            "admin_players",
            6,
            ImGuiTableFlags.Borders |
            ImGuiTableFlags.RowBg |
            ImGuiTableFlags.Resizable |
            ImGuiTableFlags.ScrollY |
            ImGuiTableFlags.Sortable,
            new Vector2(0, 0)
        ))
        {
            ImGui.TableSetupColumn("Id", ImGuiTableColumnFlags.WidthStretch, 200 * ImGuiHelper.Scale);
            ImGui.TableSetupColumn("Name", ImGuiTableColumnFlags.WidthStretch, 200 * ImGuiHelper.Scale);
            ImGui.TableSetupColumn("System", ImGuiTableColumnFlags.WidthStretch, 200 * ImGuiHelper.Scale);
            ImGui.TableSetupColumn("Last Docked Base", ImGuiTableColumnFlags.WidthStretch, 200 * ImGuiHelper.Scale);
            ImGui.TableSetupColumn("Online", ImGuiTableColumnFlags.WidthFixed | ImGuiTableColumnFlags.NoResize, 60 * ImGuiHelper.Scale);
            ImGui.TableSetupColumn("Demote", ImGuiTableColumnFlags.WidthFixed | ImGuiTableColumnFlags.NoResize, 60 * ImGuiHelper.Scale);
            ImGui.TableHeadersRow();

            if (admins != null)
            {
                foreach (var admin in admins)
                {
                    ImGui.TableNextRow();

                    ImGui.TableNextColumn();
                    ImGui.AlignTextToFramePadding();
                    ImGui.Text(admin.Id.ToString());

                    ImGui.TableNextColumn();
                    ImGui.AlignTextToFramePadding();
                    ImGui.Text(admin.Name);

                    ImGui.TableNextColumn();
                    ImGui.AlignTextToFramePadding();
                    ImGui.Text(admin.System);

                    ImGui.TableNextColumn();
                    ImGui.AlignTextToFramePadding();
                    ImGui.Text(admin.LastDockedLocation);

                    ImGui.TableNextColumn();
                    ImGui.AlignTextToFramePadding();
                    var isOnline = win.Server.Server.AllPlayers.Any(p => p.Name == admin.Name);
                    var icon = isOnline ? Icons.Check : Icons.X;
                    var color = isOnline ? SUCCESS_TEXT_COLOUR : ERROR_TEXT_COLOUR;
                    ImGui.TextColored(color, icon.ToString());


                    ImGui.TableNextColumn();
                    if (
                        ImGui.Button(
                            $"{Icons.ArrowDown.ToString()}##{admin.Id.ToString()}",
                            new Vector2(-1, ImGui.GetFrameHeight()
                            )))
                    {
                        pm.MessageBox("Confirm", $"Are you sure you want to demote {admin.Name}?", false, MessageBoxButtons.YesNo, response =>
                        {
                            if (response == MessageBoxResponse.Yes)
                            {
                                DemotePlayer(admin.Id, admin.Name);
                            }
                        });
                    }
                }
            }

            ImGui.EndTable();
        }
        ImGui.EndChild();
    }

    // Server Player Actions
    private void PromotePlayer(long characterId, string name)
    {
        Task.Run(async () =>
        {
            FLLog.Info("Server", $"Promoting {name} to admin");
            win.Server?.Server?.Database?.AdminCharacter(characterId).Wait();
            win.Server?.Server?.AdminChanged(characterId, true);
        });
    }
    private void DemotePlayer(long characterId, string name)
    {
        Task.Run(async () =>
        {
            FLLog.Info("Server", $"Demoting {name} from admin");
            win.Server?.Server?.Database?.DeadminCharacter(characterId).Wait();
            win.Server?.Server?.AdminChanged(characterId, false);
        });
    }
    private void BanPlayer(string characterName, DateTime expiry)
    {
        Task.Run(async () =>
        {
            Guid? account = await win.Server.Server.Database.FindAccount(characterName);

            if (account.HasValue)
            {
                win.Server?.Server?.Database?.BanAccount(account.Value, expiry).Wait();
                FLLog.Info("Server", $"Banned {characterName}");
            }
        });
    }
    private void BanPlayer(Guid? account, DateTime expiry)
    {
        Task.Run(async () =>
        {
            if (account != null)
            {
                win.Server?.Server?.Database?.BanAccount(account.Value, expiry).Wait();
                FLLog.Info("Server", $"Banned {account.Value}");
            }
        });
    }
    private void UnbanPlayer(Guid? account)
    {
        Task.Run(async () =>
        {
            if (account.HasValue)
            {
                win.Server?.Server?.Database?.UnbanAccount(account.Value).Wait();
                FLLog.Info("Server", $"Unbanned {account}");
            }
        });
    }
    private void RefreshData()
    {
        if (win.IsRunning)
        {
            connectedPlayers = win.Server.Server.AllPlayers;
            bannedPlayers = win.Server.Server.Database?.GetBannedPlayers();
            admins = win.Server.Server.Database?.GetAdmins();
        }
    }
}
