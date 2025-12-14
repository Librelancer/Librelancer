using System;
using System.Numerics;
using ImGuiNET;
using LibreLancer.ImUI;
using LibreLancer.Server;

namespace LLServer.Screens
{
    public class RunningServerScreen : Screen
    {
        private readonly MainWindow window;

        private bool showStopConfirm;

        public RunningServerScreen(MainWindow window, ScreenManager screens, PopupManager popups)
            : base(screens, popups)
        {
            this.window = window;

        }

        public override void OnEnter()
        {
            // Nothing to do here right now
            // Server is already started by ServerConfigurationScreen
        }

        public override void OnExit()
        {
            window.StopServer();
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
                Popups.MessageBox(
    title: "Confirm Stop",
    message: "The server is currently running.\n\nStop the server and return to configuration?",
    multiline: false,
    buttons: MessageBoxButtons.YesNo,
    callback: response =>
    {
        if (response == MessageBoxResponse.Yes)
        {
            window.QueueUIThread(() =>
            {
                Screens.SetScreen(
                    new ServerConfigurationScreen(window, Screens, Popups)
                );
            });
        }
    }
);
            }
        }

        private void DrawServerStats()
        {
            var server = window.Server;

            if (window.Server == null || !window.IsRunning)
            {
                ImGui.TextColored(new Vector4(1, 0, 0, 1), "Server not running");
                return;
            }

            ImGui.Text($"Listening on Port {window.Port}");
            ImGui.Text($"Players Connected: {window.ConnectedPlayers}");

            DrawPerformanceStats();
        }

        private void DrawPerformanceStats()
        {
            var perf = window.ServerPerformance;
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
}
