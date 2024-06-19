using System;
using System.Collections.Generic;
using LibreLancer.Ini;

namespace LibreLancer.Data.Storyline;

public enum StoryActionType
{
    AddRTC
}
public record StoryAction(StoryActionType Type, string Argument);
public class StoryItem
{
    [Entry("nickname", Required = true)] public string Nickname;
    [Entry("skip", Presence = true)] public bool Skip;
    [Entry("acceptance", Presence = true)] public bool Acceptance;
    [Entry("mission")] public string Mission;

    public List<StoryAction> Actions = new List<StoryAction>();

    [EntryHandler("action", Multiline = true, MinComponents = 1)]
    void HandleAction(Entry e)
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
