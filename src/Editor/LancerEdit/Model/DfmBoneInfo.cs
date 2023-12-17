using System.Numerics;
using ImGuiNET;
using LibreLancer;
using LibreLancer.ImUI;
using LibreLancer.Render;

namespace LancerEdit;

public class DfmBoneInfo : PopupWindow
{
    private BoneInstance instance;
    public DfmBoneInfo(BoneInstance inst)
    {
        this.instance = inst;
    }

    public override string Title
    {
        get => instance.Name;
        set { }
    }
    public override void Draw()
    {
        ImGui.Text("InvBindPose");
        ImGui.Text(instance.InvBindPose.ToString().Replace("} {", "}\n{"));
        ImGui.Text("");
        ImGui.Text("LocalTransform (pos + euler)");
        var tr = instance.LocalTransform();
        ImGui.Text(Vector3.Transform(Vector3.Zero, tr).ToString());
        ImGui.Text(tr.GetEulerDegrees().ToString());
    }
}
