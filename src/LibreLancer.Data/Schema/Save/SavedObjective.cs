using System;
using System.Collections.Generic;
using System.Numerics;
using LibreLancer.Data.Ini;

namespace LibreLancer.Data.Schema.Save;

[ParsedSection]
public partial class SavedObjective
{
    [Entry("nickname")] public int Nickname;
    [Entry("state")] public int State;
    [Entry("hidden")] public bool Hidden;

    public int Type;
    public int ObjNickname;
    public int IdsOne;
    public int IdsTwo;
    public Vector3 ObjectivePosition;
    public string? StringParam;

    [EntryHandler("type", MinComponents = 2)]
    private void HandleType(Entry e)
    {
        Type = e[0].ToInt32();
        if (Type == 3) {
            IdsOne = e[1].ToInt32();
        }
        else
        {
            ObjNickname = e[1].ToInt32();
            IdsOne = e[2].ToInt32();
            if (e.Count > 3)
                IdsTwo = e[3].ToInt32();
            if(e.Count > 6)
                ObjectivePosition = new Vector3(
                    e[4].ToSingle(),
                    e[5].ToSingle(),
                    e[6].ToSingle()
                );
            if (e.Count > 7)
                StringParam = e[7].ToString();
        }
    }
}
