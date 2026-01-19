using LibreLancer.Data;
using LibreLancer.Data.Schema.Missions;

namespace LancerEdit.GameContent.MissionEditor;

public class TriggerHeader : NicknameItem
{
    public string System = string.Empty;
    public bool Repeatable;
    public TriggerInitState InitState = TriggerInitState.INACTIVE;
}
