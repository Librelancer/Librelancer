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

public class RunningServerScreen(
    MainWindow win,
    ScreenManager sm,
    PopupManager pm,
    ServerConfig config,
    LLServerGuiConfig guiConfig)
    : Screen(sm, pm)
{
    private bool isStarting = true;

    private List<BannedPlayerDescription>? bannedPlayers = [];
    private List<AdminCharacterDescription>? admins = [];

    private readonly List<Player> lobbyPlayers = [];
    private readonly List<Player> universePlayers = [];


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
        RefreshDataAll();
    }

    public override void OnExit()
    {
        win.StopServer();
    }

    public override void Draw(double elapsed)
    {
        HandleServerEvents();

        ImGui.PushFont(ImGuiHelper.Roboto, 32);

        if (win.Server == null || (!win.IsRunning && !isStarting))
        {
            ImGuiExt.CenterText(config.ServerName, Theme.ErrorTextColor);
        }
        else if (isStarting)
        {
            ImGuiExt.CenterText(config.ServerName, Theme.WarnTextColor);
        }
        else
        {
            ImGuiExt.CenterText(config.ServerName, Theme.SuccessTextColor);
        }

        ImGui.PopFont();
        ImGui.Spacing();
        ImGui.Separator();

        ImGui.PushItemWidth(-1);

        if (ImGui.BeginTable("server_stats_layout", 3, ImGuiTableFlags.SizingStretchProp))
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
        ImGui.Text("Status:");

        ImGui.SameLine(Theme.LabelWidthMedium * ImGuiHelper.Scale);

        if (win.Server == null || (!win.IsRunning && !isStarting))
        {
            ImGui.TextColored(Theme.ErrorTextColor, "Not running");
        }
        else if (isStarting)
        {
            ImGui.TextColored(Theme.WarnTextColor, "Starting");
        }
        else
        {
            ImGui.TextColored(Theme.SuccessTextColor, "Running");
        }

        ImGui.Text("Listening Port:");

        ImGui.SameLine(Theme.LabelWidthMedium * ImGuiHelper.Scale);

        ImGui.Text(win.Server?.Server?.Listener?.Port.ToString() ?? "-");

        ImGui.Separator();

        ImGui.Text("Players in Lobby");
        ImGui.SameLine(Theme.LabelWidthMedium * ImGuiHelper.Scale);
        ImGui.Text(lobbyPlayers.Count().ToString());

        ImGui.Text("Players in Game");
        ImGui.SameLine(Theme.LabelWidthMedium * ImGuiHelper.Scale);
        ImGui.Text(universePlayers.Count().ToString());

        ImGui.Text("Admins in Game");
        ImGui.SameLine(Theme.LabelWidthMedium * ImGuiHelper.Scale);
        ImGui.Text(universePlayers.Count(p =>
            p.Character != null && admins != null &&
            admins.Any(a => a.Name == p.Character.Name)).ToString());

        ImGui.Text("Banned Players");

        ImGui.SameLine(Theme.LabelWidthMedium * ImGuiHelper.Scale);

        ImGui.Text(bannedPlayers?.Count().ToString() ?? "-");

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

        perf.Update();

        Span<float> values = stackalloc float[ServerPerformance.MAX_TIMING_ENTRIES];
        var len = perf.Timings.Count;

        double avg = 0;
        double max = 0;
        var min = double.MaxValue;

        for (var i = 0; i < len; i++)
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

    private void DrawConnectedPlayersTab()
    {
        ImGui.BeginChild("connected_players_child", new Vector2(0, 0));

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
            ImGui.TableSetupColumn("Admin", ImGuiTableColumnFlags.WidthFixed | ImGuiTableColumnFlags.NoResize,
                60 * ImGuiHelper.Scale);
            ImGui.TableSetupColumn("Promote", ImGuiTableColumnFlags.WidthFixed | ImGuiTableColumnFlags.NoResize,
                60 * ImGuiHelper.Scale);
            ImGui.TableSetupColumn("Ban", ImGuiTableColumnFlags.WidthFixed | ImGuiTableColumnFlags.NoResize,
                60 * ImGuiHelper.Scale);
            ImGui.TableSetupColumn("Inspect", ImGuiTableColumnFlags.WidthFixed | ImGuiTableColumnFlags.NoResize,
                60 * ImGuiHelper.Scale);
            ImGui.TableHeadersRow();


            if (win.IsRunning)
            {
                var buttonSize = new Vector2(-1, ImGui.GetFrameHeight());
                foreach (var player in universePlayers)
                {
                    var isAdmin = player.Character is { Admin: true };

                    ImGui.TableNextRow();

                    ImGui.TableNextColumn();
                    ImGui.AlignTextToFramePadding();
                    ImGui.Text(player.AccountId.ToString() ?? "-");

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
                    var colour = isAdmin ? Theme.SuccessTextColor : Theme.ErrorTextColor;
                    ImGui.TextColored(colour, icon.ToString());

                    ImGui.TableNextColumn();
                    var uiId = player.Character?.Name ?? "-";

                    if (ImGuiExt.Button($"{Icons.ArrowUp.ToString()}##{uiId}", !isAdmin && uiId != "-", buttonSize))
                    {
                        pm.MessageBox("Confirm",
                            $"Are you sure you want to promote {player.Character.Name} to an admin?", false,
                            MessageBoxButtons.YesNo, response =>
                            {
                                if (response == MessageBoxResponse.Yes)
                                {
                                    PromotePlayer(player.Character.ID, player.Character.Name);
                                }
                            });
                    }

                    ImGui.TableNextColumn();
                    if (ImGuiExt.Button($"{Icons.Fire.ToString()}##{player.Name ?? "-"}",
                            bannedPlayers.All(b => b.AccountId != player.AccountId), buttonSize))
                    {
                        pm.OpenPopup(new BanPopup(player.Name ?? "-", expiry =>
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

                foreach (var player in lobbyPlayers)
                {
                    var isAdmin = player.Character != null && player.Character.Admin;

                    ImGui.TableNextRow();

                    ImGui.TableNextColumn();
                    ImGui.AlignTextToFramePadding();
                    ImGui.Text(player?.AccountId.ToString() ?? "-");

                    ImGui.TableNextColumn();
                    ImGui.AlignTextToFramePadding();
                    ImGui.Text("<Unknown Player>");

                    ImGui.TableNextColumn();
                    ImGui.AlignTextToFramePadding();
                    ImGui.Text("Lobby");

                    ImGui.TableNextColumn();
                    ImGui.AlignTextToFramePadding();
                    ImGui.Text("-");

                    ImGui.TableNextColumn();
                    ImGui.AlignTextToFramePadding();
                    var icon = isAdmin ? Icons.Check : Icons.X;
                    var colour = isAdmin ? Theme.SuccessTextColor : Theme.ErrorTextColor;
                    ImGui.TextColored(colour, icon.ToString());

                    ImGui.TableNextColumn();
                    var uiId = player?.Character?.Name ?? "-";

                    ImGuiExt.Button($"{Icons.ArrowUp.ToString()}##{uiId}", false, buttonSize);

                    ImGui.TableNextColumn();
                    if (ImGuiExt.Button($"{Icons.Fire.ToString()}##{player.Name ?? "-"}",
                            bannedPlayers.All(b => b.AccountId != player.AccountId), buttonSize))
                    {
                        pm.OpenPopup(new BanPopup(player.Name, expiry =>
                        {
                            if (expiry.HasValue)
                            {
                                Guid? g = player.AccountId;
                                BanPlayerWithGuid(g, expiry);
                            }
                        }));
                    }

                    ImGui.TableNextColumn();
                    ImGuiExt.Button($"{Icons.Eye}##{player.Name ?? "-"}", false, buttonSize);
                }

                /*
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
                    var colour = isAdmin ? Theme.SUCCESS_TEXT_COLOUR : Theme.ERROR_TEXT_COLOUR;
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
                */
            }

            ImGui.EndTable();
        }

        ImGui.EndChild();
    }

    private void DrawBansTab()
    {
        ImGui.BeginChild("banned_players_child", new Vector2(0, 0));

        var tableHeight = ImGui.GetContentRegionAvail().Y - ImGui.GetFrameHeightWithSpacing() * ImGuiHelper.Scale;
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
            ImGui.TableSetupColumn("Unban", ImGuiTableColumnFlags.WidthFixed | ImGuiTableColumnFlags.NoResize,
                60 * ImGuiHelper.Scale);
            ImGui.TableSetupColumn("Increase Ban", ImGuiTableColumnFlags.WidthFixed | ImGuiTableColumnFlags.NoResize,
                100 * ImGuiHelper.Scale);
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
                    if (ImGui.Button($"{Icons.Eraser.ToString()}##{player.AccountId.ToString()}", buttonSize))
                    {
                        pm.MessageBox("Confirm", $"Are you sure you want to unban \n{player.AccountId.ToString()}?",
                            false, MessageBoxButtons.YesNo, response =>
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
                                BanPlayerWithGuid(player.AccountId, expiry.Value);
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

        var tableHeight = ImGui.GetContentRegionAvail().Y - ImGui.GetFrameHeightWithSpacing() * ImGuiHelper.Scale;
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
            ImGui.TableSetupColumn("Online", ImGuiTableColumnFlags.WidthFixed | ImGuiTableColumnFlags.NoResize,
                60 * ImGuiHelper.Scale);
            ImGui.TableSetupColumn("Demote", ImGuiTableColumnFlags.WidthFixed | ImGuiTableColumnFlags.NoResize,
                60 * ImGuiHelper.Scale);
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
                    var isOnline = win.Server?.Server.AllPlayers.Any(p => p.Name == admin.Name) ?? false;
                    var icon = isOnline ? Icons.Check : Icons.X;
                    var color = isOnline ? Theme.SuccessTextColor : Theme.ErrorTextColor;
                    ImGui.TextColored(color, icon.ToString());


                    ImGui.TableNextColumn();
                    if (
                        ImGui.Button(
                            $"{Icons.ArrowDown.ToString()}##{admin.Id.ToString()}",
                            new Vector2(-1, ImGui.GetFrameHeight()
                            )))
                    {
                        pm.MessageBox("Confirm", $"Are you sure you want to demote {admin.Name}?", false,
                            MessageBoxButtons.YesNo, response =>
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
        Task.Run(() =>
        {
            FLLog.Info("Server", $"Promoting {name} to admin");
            win.Server?.Server?.Database?.AdminCharacter(characterId).Wait();
            win.Server?.Server?.AdminChanged(characterId, true);
            return Task.CompletedTask;
        });
    }

    private void DemotePlayer(long characterId, string name)
    {
        Task.Run(() =>
        {
            FLLog.Info("Server", $"Demoting {name} from admin");
            win.Server?.Server?.Database?.DeadminCharacter(characterId).Wait();
            win.Server?.Server?.AdminChanged(characterId, false);
            return Task.CompletedTask;
        });
    }

    private void BanPlayer(string characterName, DateTime expiry)
    {
        Task.Run(async () =>
        {
            Guid? account = await win.Server?.Server.Database.FindAccount(characterName)!;

            if (account.HasValue)
            {
                win.Server?.Server?.Database?.BanAccount(account.Value, expiry).Wait();
                FLLog.Info("Server", $"Banned {characterName}");
            }
        });
    }

    private void BanPlayerWithGuid(Guid? account, DateTime? expiry)
    {
        Task.Run(() =>
        {
            if (account == null)
            {
                return Task.CompletedTask;
            }

            // TODO: Replace expiry.Value once ServerDatabase has had nullable enabled
            win.Server?.Server?.Database?.BanAccount(account.Value, expiry.Value).Wait();
            FLLog.Info("Server", $"Banned {account.Value}");

            return Task.CompletedTask;
        });
    }

    private void UnbanPlayer(Guid? account)
    {
        Task.Run(() =>
        {
            if (!account.HasValue)
            {
                return Task.CompletedTask;
            }

            win.Server?.Server?.Database?.UnbanAccount(account.Value).Wait();
            FLLog.Info("Server", $"Unbanned {account}");

            return Task.CompletedTask;
        });
    }

    // Data Methods
    private void RefreshDataAll() // performs sql queries to the DB to retrieve all data - should be used wisely
    {
        if (win.IsRunning)
        {
            //connectedPlayers = win.Server.Server.AllPlayers;
            bannedPlayers = win.Server?.Server.Database?.GetBannedPlayers().ToList();
            admins = win.Server?.Server.Database?.GetAdmins().ToList();
        }
    }

    private void HandleServerEvents()
    {
        while (win.Server?.Server.ServerEvents.Count > 0)
        {
            if (win.Server.Server.ServerEvents.TryDequeue(out var serverEvent))
            {
                switch (serverEvent.Type)
                {
                    case ServerEventType.PlayerConnected:
                        HandlePlayerConnected(serverEvent.GetPayload<PlayerConnectedEventPayload>());
                        break;
                    case ServerEventType.PlayerDisconnected:
                        HandlePlayerDisconnected(serverEvent.GetPayload<PlayerDisconnectedEventPayload>());
                        break;
                    case ServerEventType.CharacterConnected:
                        HandleCharacterConnected(serverEvent.GetPayload<CharacterConnectedEventPayload>());
                        break;
                    case ServerEventType.CharacterDisconnected:
                        HandleCharacterDisconnected(serverEvent.GetPayload<CharacterDisconnectedEventPayload>());
                        break;
                    case ServerEventType.PlayerAdminChanged:
                        HandleCharacterAdminChanged(serverEvent.GetPayload<CharacterAdminChangedEventPayload>());
                        break;
                    case ServerEventType.PlayerBanChanged:
                        HandlePlayerBannedChanged(serverEvent.GetPayload<PlayerBanChangedEventPayload>());
                        break;
                    default:
                        break;
                }
            }
        }
    }

    private void HandlePlayerBannedChanged(PlayerBanChangedEventPayload? playerBanChangedEventPayload)
    {
        FLLog.Info("Server Gui", "Received Character Admin Changed Server Event");
        if (playerBanChangedEventPayload == null) return;

        var player = playerBanChangedEventPayload.BannedPlayer;
        if (player == null) return;

        if (playerBanChangedEventPayload.IsBanned)
        {
            if (bannedPlayers.Any(b => b.AccountId == player.AccountId)) return;
            bannedPlayers.Add(player);
        }
        else
        {
            bannedPlayers.RemoveAll(b => b.AccountId == player.AccountId);
        }
    }

    private void HandleCharacterAdminChanged(CharacterAdminChangedEventPayload? characterAdminChangedEventPayload)
    {
        FLLog.Info("Server Gui", "Received Character Admin Changed Server Event");
        if (characterAdminChangedEventPayload == null) return;

        var player = characterAdminChangedEventPayload.AdminCharacter;
        if (player == null) return;

        if (characterAdminChangedEventPayload.IsAdmin)
        {
            if (admins.Any(a => a.Id == player.Id)) return;
            admins.Add(player);
        }
        else
        {
            admins.RemoveAll(a => a.Id == player.Id);
        }
    }


    private void HandleCharacterDisconnected(CharacterDisconnectedEventPayload? characterDisconnectedEventPayload)

    {
        FLLog.Info("Server Gui", "Received Character Disconnect Server Event");
        if (characterDisconnectedEventPayload == null) return;

        var player = characterDisconnectedEventPayload.DisconnectedCharacter;
        if (player == null) return;

        universePlayers.RemoveAll(p => p.AccountId == player.AccountId);

    }

    private void HandleCharacterConnected(CharacterConnectedEventPayload? characterConnectedEventPayload)
    {
        FLLog.Info("Server Gui", "Received Character Connect Server Event");

        var player = characterConnectedEventPayload?.ConnectedCharacter;
        if (player == null)
        {
            return;
        }

        lobbyPlayers.RemoveAll(p => p.AccountId == player.AccountId);

        if (universePlayers.Any(p => p.AccountId == player.AccountId)) return;

        universePlayers.Add(player);
    }

    private void HandlePlayerDisconnected(PlayerDisconnectedEventPayload? playerDisconnectedEventPayload)
    {
        FLLog.Info("Server Gui", "Received Player Disconnect Server Event");
        if (playerDisconnectedEventPayload == null) return;

        var player = playerDisconnectedEventPayload.DisconnectedPlayer;
        if (player == null) return;

        FLLog.Info("Server Gui", $"{player.Name} {player.AccountId}");

        lobbyPlayers.RemoveAll(x => x.AccountId == player.AccountId);
        universePlayers.RemoveAll(x => x.AccountId == player.AccountId);
    }

    private void HandlePlayerConnected(PlayerConnectedEventPayload? playerConnectedEventPayload)
    {
        FLLog.Info("Server Gui", "Received Player Connect Server Event");
        if (playerConnectedEventPayload == null) return;

        var player = playerConnectedEventPayload.ConnectedPlayer;
        if (player == null) return;

        if (lobbyPlayers.Any(p => p.AccountId == player.AccountId)) return;

        lobbyPlayers.Add(player);
    }
}
