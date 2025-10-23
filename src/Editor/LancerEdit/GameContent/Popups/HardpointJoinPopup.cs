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
    private HardpointLookup hpChild;

    public delegate void JoinHardpointsHandler(Hardpoint parentHp, Hardpoint childHp, bool setParentProperty);


    public HardpointJoinPopup(GameObject parent, GameObject child, JoinHardpointsHandler onSet, Action<HpGizmoData[]> drawPreviews)
    {
        this.parent = parent;
        hpParent = new HardpointLookup("Parent", parent);
        this.child = child;
        hpChild = new HardpointLookup("Child", child);
        this.onSet = onSet;
        this.drawPreviews = drawPreviews;
    }

    public override void Draw(bool appearing)
    {
        var previewParent = hpParent.Selected;
        var previewChild = hpChild.Selected;

        ImGui.Text($"Parent: {parent.Nickname}");
        hpParent.Draw();
        if (hpParent.IsOpen)
            previewParent = hpParent.Hovered;
        ImGui.Text($"Child: {parent.Nickname}");
        hpChild.Draw();
        if(hpChild.IsOpen)
            previewChild =  hpChild.Hovered;
        ImGui.Checkbox("Set Parent", ref setParent);
        ImGui.SetItemTooltip("Check to set the child's parent property to the parent object");
        if(ImGuiExt.Button("Ok", hpParent.Selected != null || hpChild.Selected != null))
        {
            onSet(hpParent.Selected, hpChild.Selected, setParent);
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
