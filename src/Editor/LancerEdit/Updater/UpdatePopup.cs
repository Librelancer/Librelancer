using System.Numerics;
using System.Threading;
using System.Threading.Tasks;
using ImGuiNET;
using LibreLancer;
using LibreLancer.ImUI;

namespace LancerEdit.Updater;

public class UpdatePopup : PopupWindow
{
    public override string Title { get; set; } = "Updating";
    public override bool NoClose => true;
    public override ImGuiWindowFlags WindowFlags => ImGuiWindowFlags.AlwaysAutoResize;

    private CancellationTokenSource source;
    public CancellationToken Token => source.Token;

    public int Stage = 0;

    private CancellationToken token;
    private long totalLength = -1;
    private long downloaded = 0;

    private UpdateChecks updater;

    public UpdatePopup(UpdateChecks updater)
    {
        source = new CancellationTokenSource();
        this.updater = updater;
    }


    public void SetProgress(long downloaded, long totalLength)
    {
        this.downloaded = downloaded;
        this.totalLength = totalLength;
    }

    private string msg = null;

    public void Message(string text)
    {
        this.msg = text;
    }

    private string newUrl = null;

    public void NewVersion(string url)
    {
        newUrl = url;
        Stage = 1;
    }

    void Checking()
    {
        ImGuiExt.Spinner("##spinner", 10, 2, ImGui.GetColorU32(ImGuiCol.ButtonHovered, 1));
        ImGui.SameLine();
        ImGui.Text("Checking for updates");
        if (ImGui.Button("Cancel"))
        {
            source.Cancel();
            ImGui.CloseCurrentPopup();
        }
    }

    void Confirming()
    {
        ImGui.Text("A new version is available. Download and install?");
        if (ImGui.Button("Yes"))
        {
            Stage = 2;
            Task.Run(async () =>
            {
                var result = await UpdateDownloader.DownloadUpdate(newUrl, SetProgress, Token);
                if (result.IsError) {
                    Message("Download failed");
                }
                else
                {
                    Message("Launching updater");
                    updater.Update(result.Data);
                }
            });
        }
        ImGui.SameLine();
        if (ImGui.Button("No"))
        {
            ImGui.CloseCurrentPopup();
        }
    }

    void Downloading()
    {
        ImGuiExt.Spinner("##spinner", 10, 2, ImGui.GetColorU32(ImGuiCol.ButtonHovered, 1));
        ImGui.SameLine();
        if(downloaded == 0 && totalLength == -1)
            ImGui.Text("Downloading");
        else
            ImGui.Text(
                $"Downloading {DebugDrawing.SizeSuffix(downloaded)} out of {(totalLength < 0 ? "?" : DebugDrawing.SizeSuffix(totalLength))}");
        if (totalLength != -1)
            ImGui.ProgressBar((float)((double)downloaded / (double)totalLength),
                new Vector2(350, 25) * ImGuiHelper.Scale);
        if (ImGui.Button("Cancel"))
        {
            source.Cancel();
            ImGui.CloseCurrentPopup();
        }
    }

    public override void Draw()
    {
        if (msg != null)
        {
            ImGui.TextUnformatted(msg);
            if(ImGui.Button("Ok"))
                ImGui.CloseCurrentPopup();
        }
        else
        {
            if(Stage == 0)
                Checking();
            else if(Stage == 1)
                Confirming();
            else if (Stage == 2)
                Downloading();
        }
    }
}
