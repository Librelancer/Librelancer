using System;
using System.Linq;
using System.Numerics;
using ImGuiNET;
using LancerEdit.GameContent.Lookups;
using LibreLancer.Client;
using LibreLancer.Data.GameData;
using LibreLancer.Data.GameData.World;
using LibreLancer.ImUI;

namespace LancerEdit.GameContent.Popups;

public class NpcPlacementPopup : PopupWindow
{
    public override string Title { get; set; } = "Placement";
    public override Vector2 InitSize => new(300 * ImGuiHelper.Scale);

    private string? spot = null;
    private ResolvedThn? thn = null;
    private StringAutocomplete actionField;
    private string actionText = "bartender";
    private ObjectLookup<string?> spotLookup;
    private Action<BaseNpcPlacement> commit;
    private VfsFileSelectorControl fileSelector;
    private GameDataContext gd;
     private bool selectingFile = false;

    public NpcPlacementPopup(BaseNpc npc, RoomNpcSpot[] spots, GameDataContext gd, Action<BaseNpcPlacement> commit)
    {
        if (npc.Placement != null)
        {
            spot = npc.Placement.Spot;
            thn = npc.Placement.FidgetScript;
            actionText = npc.Placement.Action;
        }
        actionField = new StringAutocomplete("action", ["bartender", "Equipment", "ShipDealer", "trader"],
            actionText, v => actionText = v);
        var validSpots = spots.Where(x => !x.Dynamic)
            .OrderBy(x => x.Nickname)
            .Select(x => x.Nickname).ToArray();
        spotLookup = new ObjectLookup<string?>(validSpots, x => x ?? "(none)");
        fileSelector = new("##file", gd.GameData.VFS, gd.GameData.Items.Ini.Freelancer.DataPath,
            VfsFileSelector.MakeFilter(".thn"));
        this.commit = commit;
        this.gd = gd;
    }

    public override void Draw(bool appearing)
    {
        if (selectingFile)
        {
            switch (fileSelector.Draw(out var selectedFile))
            {
                case FileSelectorState.Selected:
                    var scriptFile = gd.GameData.Items.DataPath(selectedFile);
                    var sourcePath = selectedFile!.Replace('/', '\\');
                    thn = new ResolvedThn()
                    {
                        DataPath = scriptFile,
                        SourcePath = sourcePath,
                        ReadCallback = gd.GameData.Items.ThornReadCallback,
                        VFS = gd.GameData.VFS
                    };
                    selectingFile = false;
                    break;
                case FileSelectorState.Cancel:
                    selectingFile = false;
                    break;
            }
        }
        else
        {
            bool canSave = !string.IsNullOrWhiteSpace(spot) &&
                           thn?.DataPath != null &&
                           !string.IsNullOrWhiteSpace(actionText);

            if (Controls.BeginEditorTable("form"))
            {
                spotLookup.Draw("Spot", ref spot);
                Controls.EditControlSetup("Fidget Script", 0, -Controls.ButtonWidth($"{Icons.Edit}"));
                ImGui.LabelText("", $"{thn?.SourcePath ?? "(none)"}");
                ImGui.SameLine();
                if (ImGui.Button($"{Icons.Edit}"))
                    selectingFile = true;
                Controls.EditControlSetup("Action", 0);
                actionField.Draw();
                Controls.EndEditorTable();
            }

            if (ImGuiExt.Button("Ok", canSave))
            {
                commit(new(spot!, thn!, actionText!));
            }

            ImGui.SameLine();
            if (ImGui.Button("Cancel"))
            {
                ImGui.CloseCurrentPopup();
            }
        }
    }
}
