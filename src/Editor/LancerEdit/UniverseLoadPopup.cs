using System.Numerics;
using ImGuiNET;
using LancerEdit.GameContent;
using LibreLancer.ImUI;

namespace LancerEdit;

public class UniverseLoadPopup : PopupWindow
{
    public override string Title { get; set; } = "Loading";
    public override ImGuiWindowFlags WindowFlags => ImGuiWindowFlags.NoResize;
    public override Vector2 InitSize => new Vector2(200, 100) * ImGuiHelper.Scale;
    public override bool NoClose => true;

    private int loadFrames = 0;
    private MainWindow mw;

    public UniverseLoadPopup(MainWindow mw)
    {
        this.mw = mw;
    }
    public override void Draw(bool appearing)
    {
        ImGui.Text($"Loading Universe Editor");
        ImGui.ProgressBar(mw.OpenDataContext.PreviewLoadPercent, new Vector2(180, 0) * ImGuiHelper.Scale);
        ImGuiHelper.AnimatingElement();
        loadFrames++;
        if ((loadFrames > 8 || loadFrames % 2 == 0) &&
            !mw.OpenDataContext.IterateRenderArchetypePreviews(loadFrames > 8 ? 120 : 8))
        {
            mw.AddTab(new UniverseEditorTab(mw.OpenDataContext, mw));
            loadFrames = 0;
            ImGui.CloseCurrentPopup();
        }
    }
}
