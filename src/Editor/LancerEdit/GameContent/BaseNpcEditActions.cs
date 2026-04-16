using LibreLancer.Data.GameData;
using LibreLancer.Data.GameData.World;
using LibreLancer.Data.Schema.MBases;

namespace LancerEdit.GameContent;

// NpcDeleteAction needs custom logic (tab.CheckDeleted), so it cannot be a simple ListRemove<T>.
public sealed class NpcDeleteAction(BaseRoom target, BaseNpc npc, BaseNpcEditorTab tab) : EditorAction
{
    private int index = -1;

    public override string ToString() => $"Delete NPC: {npc.Nickname}";

    public override void Commit()
    {
        index = target.Npcs.IndexOf(npc);
        target.Npcs.Remove(npc);
        tab.CheckDeleted(npc);
    }

    public override void Undo()
    {
        if (index >= 0 && index <= target.Npcs.Count)
            target.Npcs.Insert(index, npc);
        else
            target.Npcs.Add(npc);
    }
}
