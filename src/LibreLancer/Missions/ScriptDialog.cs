using System.Collections.Generic;
using System.Linq;
using LibreLancer.Data;
using LibreLancer.Data.Schema.Missions;

namespace LibreLancer.Missions;

public class ScriptDialog : NicknameItem
{
    public string System;
    public List<DialogLine> Lines = new List<DialogLine>();

    public static ScriptDialog FromIni(MissionDialog dialog) => new()
    {
        Nickname = dialog.Nickname,
        System = dialog.System,
        Lines = dialog.Lines.Select(x => x.Clone()).ToList()
    };
}
