using LibreLancer.Data.GameData;
using LibreLancer.Data.GameData.World;
using LibreLancer.Data.Schema.MBases;

namespace LancerEdit.GameContent;

// ── Add / Delete NPC ─────────────────────────────────────────────────────────

public sealed class NpcAddAction(Base target, BaseNpc npc) : EditorAction
{
    public override string ToString() => $"Add NPC: {npc.Nickname}";
    public override void Commit() => target.Npcs.Add(npc);
    public override void Undo()   => target.Npcs.Remove(npc);
}

public sealed class NpcDeleteAction(Base target, BaseNpc npc, BaseNpcEditorTab tab) : EditorAction
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

// ── Rumor Add / Remove ────────────────────────────────────────────────────────

public sealed class NpcAddRumorAction(BaseNpc target, NpcRumor rumor) : EditorAction
{
    public override string ToString() => $"Add Rumor IDS {rumor.Ids}";
    public override void Commit() => target.Rumors.Add(rumor);
    public override void Undo()   => target.Rumors.Remove(rumor);
}

public sealed class NpcRemoveRumorAction(BaseNpc target, NpcRumor rumor) : EditorAction
{
    private int index = -1;

    public override string ToString() => $"Remove Rumor IDS {rumor.Ids}";

    public override void Commit()
    {
        index = target.Rumors.IndexOf(rumor);
        target.Rumors.Remove(rumor);
    }

    public override void Undo()
    {
        if (index >= 0 && index <= target.Rumors.Count)
            target.Rumors.Insert(index, rumor);
        else
            target.Rumors.Add(rumor);
    }
}

public abstract class RumorFieldModification<T>(NpcRumor target, T old, T updated, string name)
    : EditorModification<T>(old, updated)
{
    protected NpcRumor Target = target;
    public override string ToString() => $"{name}: {Old} → {Updated}";
}

public sealed class NpcSetRumorStart(NpcRumor target, string old, string updated)
    : RumorFieldModification<string>(target, old, updated, "SetRumorStart")
{
    public override void Set(string value) => Target.Start = value;
}

public sealed class NpcSetRumorEnd(NpcRumor target, string old, string updated)
    : RumorFieldModification<string>(target, old, updated, "SetRumorEnd")
{
    public override void Set(string value) => Target.End = value;
}

public sealed class NpcSetRumorRep(NpcRumor target, int old, int updated)
    : RumorFieldModification<int>(target, old, updated, "SetRumorRep")
{
    public override void Set(int value) => Target.RepRequired = value;
}

public sealed class NpcSetRumorIds(NpcRumor target, int old, int updated)
    : RumorFieldModification<int>(target, old, updated, "SetRumorIds")
{
    public override void Set(int value) => Target.Ids = value;
}

// ── Generic field modifications ───────────────────────────────────────────────

public abstract class NpcFieldModification<T>(BaseNpc target, T old, T updated, string name)
    : EditorModification<T>(old, updated)
{
    protected BaseNpc Target = target;
    public override string ToString() => $"{name}: {Old} → {Updated}";
}

public sealed class NpcSetNickname(BaseNpc target, string? old, string? updated)
    : NpcFieldModification<string?>(target, old, updated, "SetNickname")
{
    public override void Set(string? value) => Target.Nickname = value ?? "";
}

public sealed class NpcSetBody(BaseNpc target, string? old, string? updated)
    : NpcFieldModification<string?>(target, old, updated, "SetBody")
{
    public override void Set(string? value) => Target.Body = value;
}

public sealed class NpcSetHead(BaseNpc target, string? old, string? updated)
    : NpcFieldModification<string?>(target, old, updated, "SetHead")
{
    public override void Set(string? value) => Target.Head = value;
}

public sealed class NpcSetLeftHand(BaseNpc target, string? old, string? updated)
    : NpcFieldModification<string?>(target, old, updated, "SetLeftHand")
{
    public override void Set(string? value) => Target.LeftHand = value;
}

public sealed class NpcSetRightHand(BaseNpc target, string? old, string? updated)
    : NpcFieldModification<string?>(target, old, updated, "SetRightHand")
{
    public override void Set(string? value) => Target.RightHand = value;
}

public sealed class NpcSetAccessory(BaseNpc target, string? old, string? updated)
    : NpcFieldModification<string?>(target, old, updated, "SetAccessory")
{
    public override void Set(string? value) => Target.Accessory = value;
}

public sealed class NpcSetVoice(BaseNpc target, string? old, string? updated)
    : NpcFieldModification<string?>(target, old, updated, "SetVoice")
{
    public override void Set(string? value) => Target.Voice = value;
}

public sealed class NpcSetRoom(BaseNpc target, string? old, string? updated)
    : NpcFieldModification<string?>(target, old, updated, "SetRoom")
{
    public override void Set(string? value) => Target.Room = value;
}

public sealed class NpcSetIndividualName(BaseNpc target, int old, int updated)
    : NpcFieldModification<int>(target, old, updated, "SetIndividualName")
{
    public override void Set(int value) => Target.IndividualName = value;
}

public sealed class NpcSetAffiliation : EditorAction
{
    private readonly BaseNpc target;
    private readonly Faction? oldFaction;
    private readonly Faction? newFaction;
    private readonly string? oldNickname;
    private readonly string? newNickname;

    public NpcSetAffiliation(BaseNpc target, Faction? oldFaction, Faction? newFaction, string? oldNickname, string? newNickname)
    {
        this.target = target;
        this.oldFaction = oldFaction;
        this.newFaction = newFaction;
        this.oldNickname = oldNickname;
        this.newNickname = newNickname;
    }

    public override void Commit()
    {
        target.Affiliation = newFaction;
        target.AffiliationNickname = newNickname;
    }

    public override void Undo()
    {
        target.Affiliation = oldFaction;
        target.AffiliationNickname = oldNickname;
    }

    public override string ToString() => $"SetAffiliation: {oldNickname ?? oldFaction?.Nickname ?? "(none)"} → {newNickname ?? newFaction?.Nickname ?? "(none)"}";
}

public sealed class NpcSetMission(BaseNpc target, NpcMission? old, NpcMission? updated)
    : NpcFieldModification<NpcMission?>(target, old, updated, "SetMission")
{
    public override void Set(NpcMission? value) => Target.Mission = value;
}
