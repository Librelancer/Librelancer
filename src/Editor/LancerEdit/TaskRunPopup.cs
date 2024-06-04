using System;
using System.Data.SqlTypes;
using System.Numerics;
using System.Threading;
using ImGuiNET;
using LibreLancer.ImUI;

namespace LancerEdit;

public class TaskRunPopup : PopupWindow
{
    private bool finished = false;
    private CancellationTokenSource source;
    private AppLog log;
    private bool canCancel;

    public override string Title { get; set; }
    public override bool NoClose => true;

    public override Vector2 InitSize => new Vector2(400) * ImGuiHelper.Scale;
    public CancellationToken Token => source.Token;

    public void Log(string text)
    {
        log?.AppendText(text);
    }

    public void Finish()
    {
        finished = true;
    }

    public TaskRunPopup(string title, bool canCancel = true)
    {
        Title = title;
        source = new CancellationTokenSource();
        log = new AppLog();
        this.canCancel = canCancel;
    }

    private Action onConfirm;
    private string confirmText;
    public void Confirm(string text, Action onConfirm)
    {
        this.onConfirm = onConfirm;
        this.confirmText = text;
    }

    void Loading()
    {
        if (!finished)
        {
            ImGuiExt.Spinner("##spinner", 10, 2, ImGui.GetColorU32(ImGuiCol.ButtonHovered, 1));
            ImGui.SameLine();
            ImGui.Text("Processing");
        }
        else
        {
            ImGui.Text("Complete!");
        }

        var w = ImGui.GetWindowWidth() - (10 * ImGuiHelper.Scale);
        var h = ImGui.GetContentRegionAvail().Y - (40 * ImGuiHelper.Scale);
        log.Draw(false, new Vector2(w, h));
        if (ImGuiExt.Button("Ok", finished)) {
            ImGui.CloseCurrentPopup();
            log.Dispose();
            log = null;
        }
        if (canCancel)
        {
            ImGui.SameLine();
            if (ImGuiExt.Button("Cancel", !finished && !source.IsCancellationRequested))
            {
                source.Cancel();
            }
        }
        ImGui.SameLine();
        if (ImGuiExt.Button("Copy Log", finished)) {
            ImGui.SetClipboardText(log.GetLogString());
        }
    }
    public override void Draw()
    {
        if (confirmText != null)
        {
            ImGui.Text(confirmText);
            if (ImGui.Button("Yes"))
            {
                onConfirm();
                confirmText = null;
            }

            ImGui.SameLine();
            if (ImGui.Button("No"))
            {
                ImGui.CloseCurrentPopup();
                log.Dispose();
                log = null;
            }
        }
        else
            Loading();
    }
}
