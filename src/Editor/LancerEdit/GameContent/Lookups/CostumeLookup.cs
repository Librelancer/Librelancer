using System;
using BepuPhysics.Constraints;
using ImGuiNET;
using LibreLancer.Data.GameData;

namespace LancerEdit.GameContent.Lookups;

public class CostumeLookup
{
    public ObjectLookup<Bodypart> Bodyparts;
    public ObjectLookup<Accessory> Accessories;

    public CostumeLookup(ObjectLookup<Bodypart> bodyparts, ObjectLookup<Accessory> accessories)
    {
        Bodyparts = bodyparts;
        Accessories = accessories;
    }

    public void Draw(
        string id,
        EditorUndoBuffer undo,
        FieldAccessor<Bodypart> head,
        FieldAccessor<Bodypart> body,
        FieldAccessor<Accessory> accessory)
    {
        ImGui.PushID(id);
        Bodyparts.DrawUndo("Head", undo, head, true);
        Bodyparts.DrawUndo("Body", undo, body, true);
        Accessories.DrawUndo("Accessory", undo, accessory, true);
        ImGui.PopID();
    }
}
