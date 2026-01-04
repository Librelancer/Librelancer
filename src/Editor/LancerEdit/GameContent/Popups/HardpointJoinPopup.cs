using System;
using System.Collections.Generic;
using System.Numerics;
using ImGuiNET;
using LancerEdit.GameContent.Lookups;
using LibreLancer;
using LibreLancer.ImUI;
using LibreLancer.Render;
using LibreLancer.World;

namespace LancerEdit.GameContent.Popups;

public record struct HpGizmoData(Transform3D Transform, float Scale);
public sealed class HardpointJoinPopup : PopupWindow
{
    public override string Title { get; set; } = "Join Objects";
    public override ImGuiWindowFlags WindowFlags => ImGuiWindowFlags.AlwaysAutoResize;

    private GameObject parent;
    private GameObject child;
    private JoinHardpointsHandler onSet;
    private Action<HpGizmoData[]> drawPreviews;
    static bool setParent = true;

    private HardpointLookup hpParent;
    private Hardpoint selectedParent;
    private HardpointLookup hpChild;
    private Hardpoint selectedChild;
    public delegate void JoinHardpointsHandler(Hardpoint parentHp, Hardpoint childHp, bool setParentProperty);


    public HardpointJoinPopup(GameObject parent, GameObject child, JoinHardpointsHandler onSet, Action<HpGizmoData[]> drawPreviews)
    {
        this.parent = parent;
        hpParent = new HardpointLookup(parent);
        this.child = child;
        hpChild = new HardpointLookup(child);
        this.onSet = onSet;
        this.drawPreviews = drawPreviews;
    }

    public override void Draw(bool appearing)
    {
        var previewParent = selectedParent;
        var previewChild = selectedChild;

        ImGui.Text($"Parent: {parent.Nickname}");

        if (hpParent.Draw("##parent", ref selectedParent, out var pHover))
            previewParent = pHover;
        ImGui.Text($"Child: {parent.Nickname}");
        if (hpChild.Draw("##child", ref selectedChild, out var cHover))
            previewChild = cHover;
        ImGui.Checkbox("Set Parent", ref setParent);
        ImGui.SetItemTooltip("Check to set the child's parent property to the parent object");
        if(ImGuiExt.Button("Ok", selectedParent != null || selectedChild != null))
        {
            onSet(selectedParent, selectedChild, setParent);
            ImGui.CloseCurrentPopup();
        }
        ImGui.SameLine();
        if (ImGui.Button("Cancel"))
        {
            ImGui.CloseCurrentPopup();
        }

        // Previews
        var pv = new List<HpGizmoData>();
        if (previewParent != null)
        {
            float scale = 5;
            if (parent.RenderComponent is ModelRenderer mr)
                scale = mr.Model.GetRadius() / GizmoRender.ScaleFactor;
            pv.Add(new(previewParent.Transform * parent.LocalTransform, scale));
        }
        if (previewChild != null)
        {
            float scale = 5;
            if (child.RenderComponent is ModelRenderer mr)
                scale = mr.Model.GetRadius() / GizmoRender.ScaleFactor;
            pv.Add(new(previewChild.Transform * child.LocalTransform, scale));
        }
        drawPreviews(pv.ToArray());
    }
}
