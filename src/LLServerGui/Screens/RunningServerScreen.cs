using System;
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

    private bool showStopConfirm;

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
        DrawTopBar();
        DrawServerStats();
        DrawActions();
    }

    private void DrawTopBar()
    {
        ImGui.Text("Server Running");
        ImGui.Separator();
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
        if (win.Server == null || !win.IsRunning)
        {
            ImGui.TextColored(new Vector4(1, 0, 0, 1), "Server not running");
            return;
        }

        ImGui.Text($"Listening on Port {win.Port}");
        ImGui.Text($"Players Connected: {win.ConnectedPlayers}");

        DrawPerformanceStats();
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

        ImGui.Separator();
        ImGui.Text(
            $"Update Time: Avg {avg:F4}ms | Min {min:F4}ms | Max {max:F4}ms"
        );

        ImGui.PlotLines(
            "##updatetime",
            ref values[0],
            len,
            0,
            "",
            0,
            (float)Math.Max(max, 17),
            new Vector2(400, 150)
        );
    }
}
