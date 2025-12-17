using System.Numerics;
using ImGuiNET;
using LibreLancer.ImUI;
using LibreLancer.Server;

namespace LLServer
{
    public class InspectorPopup : PopupWindow
    {
        public override string Title { get; set; } = "Player Inspector";
        public override ImGuiWindowFlags WindowFlags => ImGuiWindowFlags.NoCollapse;
        public override bool NoClose => true;
        public override Vector2 InitSize => new Vector2(400, 600);

        readonly Player player;

        public InspectorPopup(Player player)
        {
            this.player = player;

        }
        public override void Draw(bool appearing)
        {
            ImGui.PushFont(ImGuiHelper.Roboto, 32);
            ImGuiExt.CenterText(player?.Name ?? "Unknown Character");
            ImGui.PopFont();

            ImGui.Spacing();
            ImGui.Separator();
            ImGui.Spacing();

            DrawSessionInfo();
            DrawLocation();
            DrawTransform();

            ImGui.Dummy(new Vector2(
                0,
                ImGui.GetContentRegionAvail().Y - (ImGui.GetFrameHeightWithSpacing() *2)));

            ImGui.Spacing();
            ImGui.Separator();
            ImGui.Spacing();

            if (ImGui.Button("Close", new Vector2(-1, 0)))
            {
                ImGui.CloseCurrentPopup();
            }
        }

        void DrawSessionInfo()
        {
            if (!ImGui.CollapsingHeader("Session Info", ImGuiTreeNodeFlags.DefaultOpen))
                return;

            BeginKeyValueTable();
            Row("Name", player?.Name);
            Row("Start Time", player?.StartTime.ToLocalTime().ToString("G"));
            EndKeyValueTable();
        }

        void DrawLocation()
        {
            if (!ImGui.CollapsingHeader("Location", ImGuiTreeNodeFlags.DefaultOpen))
                return;

            BeginKeyValueTable();
            Row("System", player?.System);
            Row("Base", player?.Base ?? "-");
            EndKeyValueTable();
        }

        void DrawTransform()
        {
            if (!ImGui.CollapsingHeader("Transform"))
                return;

            BeginKeyValueTable();
            Row("Position", FormatVector3(player.Position));
            Row("Orientation", FormatQuaternion(player.Orientation));
            EndKeyValueTable();
        }

        // ---------------- Helpers ----------------

        void BeginKeyValueTable()
        {
            ImGui.BeginTable(
                "##kv",
                2,
                ImGuiTableFlags.BordersInnerV |
                ImGuiTableFlags.RowBg |
                ImGuiTableFlags.SizingFixedFit
            );

            ImGui.TableSetupColumn("Key", ImGuiTableColumnFlags.WidthFixed, 140 * ImGuiHelper.Scale);
            ImGui.TableSetupColumn("Value", ImGuiTableColumnFlags.WidthStretch);
        }

        void EndKeyValueTable()
        {
            ImGui.EndTable();
            ImGui.Spacing();
        }

        void Row(string key, string value)
        {
            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.Text(key);
            ImGui.TableNextColumn();
            ImGui.Text(value ?? "-");
        }

        static string FormatVector3(Vector3 v)
            => $"X:{v.X:F2}  Y:{v.Y:F2}  Z:{v.Z:F2}";

        static string FormatQuaternion(Quaternion q)
            => $"X:{q.X:F2}  Y:{q.Y:F2}  Z:{q.Z:F2}  W:{q.W:F2}";
    }
}
