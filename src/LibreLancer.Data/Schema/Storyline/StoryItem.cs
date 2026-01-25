using System;
using System.Collections.Generic;
using LibreLancer.Data.Ini;

namespace LibreLancer.Data.Schema.Storyline;

public enum StoryActionType
{
    AddRTC
}
public record StoryAction(StoryActionType Type, string Argument);

[ParsedSection]
public partial class StoryItem
{
    [Entry("nickname", Required = true)] public string Nickname = null!;
    [Entry("skip", Presence = true)] public bool Skip;
    [Entry("acceptance", Presence = true)] public bool Acceptance;
    [Entry("hide_gui", Presence = true)] public bool HideGui;
    [Entry("mission")] public string? Mission;
    [Entry("cash_up")] public int CashUp;

    public List<StoryAction> Actions = [];

    [EntryHandler("action", Multiline = true, MinComponents = 1)]
    private void HandleAction(Entry e)
    {
        if (!Enum.TryParse<StoryActionType>(e[0].ToString(), true, out var act)) {
            FLLog.Error("Ini", $"Unknown story action {act} at {e.Section.File}:{e.Line}");
        }
        else
        {
            var str = e.Count > 1 ? e[1].ToString() : "";
            Actions.Add(new(act, str));
        }
    }
}
