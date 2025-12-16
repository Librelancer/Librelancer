using System;
using System.Numerics;
using ImGuiNET;
using LibreLancer.ImUI;

namespace LLServer
{
    public class BanPopup : PopupWindow
    {
        public override string Title { get; set; } = "Confirm Ban Expiry";
        public override ImGuiWindowFlags WindowFlags => ImGuiWindowFlags.NoCollapse;
        public override bool NoClose => true;
        public override Vector2 InitSize => new Vector2(400, 300);

        readonly string playerName;
        readonly Action<DateTime?> callback;

        DateTime expiryUtc;

        public BanPopup(string playerName, Action<DateTime?> callback)
        {
            Title = "Ban Player";
            this.playerName = playerName;
            this.callback = callback;

            expiryUtc = DateTime.UtcNow.AddDays(7); // default
        }
        public override void Draw(bool appearing)
        {
            ImGui.Text("Are you sure you want to ban:");
            ImGui.Spacing();
            ImGui.TextColored(new Vector4(0f, 0.8f, 0.2f, 1f), playerName);
            ImGui.Spacing();
            ImGui.Separator();
            ImGui.Spacing();

            DrawDateTimeEditor();

            ImGui.Spacing();
            ImGui.Separator();
            ImGui.Spacing();

            if (ImGui.Button("Confirm Ban", new Vector2(120 * ImGuiHelper.Scale, 0)))
            {
                callback?.Invoke(expiryUtc);
                ImGui.CloseCurrentPopup();
            }

            ImGui.SameLine();

            if (ImGui.Button("Cancel", new Vector2(120 * ImGuiHelper.Scale, 0)))
            {
                callback?.Invoke(null);
                ImGui.CloseCurrentPopup();
            }
        }

        void DrawDateTimeEditor()
        {
            int year = expiryUtc.Year;
            int month = expiryUtc.Month;
            int day = expiryUtc.Day;
            int hour = expiryUtc.Hour;
            int minute = expiryUtc.Minute;

            ImGui.Text("Ban expires at (UTC)");

            ImGui.PushItemWidth(80 * ImGuiHelper.Scale);
            ImGuiExt.InputIntExpr("Year", ref year);
            ImGui.SameLine();
            ImGuiExt.InputIntExpr("Month", ref month);
            ImGui.SameLine();
            ImGuiExt.InputIntExpr("Day", ref day);

            ImGuiExt.InputIntExpr("Hour", ref hour);
            ImGui.SameLine();
            ImGuiExt.InputIntExpr("Minute", ref minute);
            ImGui.PopItemWidth();

            month = Math.Clamp(month, 1, 12);
            day = Math.Clamp(day, 1, DateTime.DaysInMonth(year, month));
            hour = Math.Clamp(hour, 0, 23);
            minute = Math.Clamp(minute, 0, 59);

            try
            {
                expiryUtc = new DateTime(year, month, day, hour, minute, 0, DateTimeKind.Utc);
            }
            catch
            {
                // Ignore invalid dates until corrected
            }
        }
    }
}
