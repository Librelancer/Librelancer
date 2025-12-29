using System;
using System.Numerics;
using ImGuiNET;
using LibreLancer.ImUI;

namespace LLServer;

public class BanPopup : PopupWindow
{
    public override string Title { get; set; } = "Confirm Ban Expiry";
    public override ImGuiWindowFlags WindowFlags => ImGuiWindowFlags.NoCollapse;
    public override bool NoClose => true;
    public override Vector2 InitSize => new Vector2(400, 300);

    private readonly string playerName;
    private readonly Action<DateTime?> callback;

    private DateTime expiryUtc;

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

    private void DrawDateTimeEditor()
    {
        var year = expiryUtc.Year;
        var month = expiryUtc.Month;
        var day = expiryUtc.Day;
        var hour = expiryUtc.Hour;
        var minute = expiryUtc.Minute;

        ImGui.Text("Ban expires at (UTC)");

        ImGui.PushItemWidth(80 * ImGuiHelper.Scale);
        ImGui.InputInt("Year", ref year);
        ImGui.SameLine();
        ImGui.InputInt("Month", ref month);
        ImGui.SameLine();
        ImGui.InputInt("Day", ref day);

        ImGui.InputInt("Hour", ref hour);
        ImGui.SameLine();
        ImGui.InputInt("Minute", ref minute);
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
