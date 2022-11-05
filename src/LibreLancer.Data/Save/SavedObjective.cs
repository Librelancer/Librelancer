using System;
using System.Collections.Generic;
using System.Numerics;
using LibreLancer.Ini;

namespace LibreLancer.Data.Save;

public class SavedObjective : ICustomEntryHandler
{
    [Entry("nickname")] public int Nickname;
    [Entry("state")] public int State;
    [Entry("hidden")] public bool Hidden;

    public int Type;
    public int ObjNickname;
    public int IdsOne;
    public int IdsTwo;
    public Vector3 ObjectivePosition;
    public string StringParam;
    
    private static readonly CustomEntry[] _custom = new CustomEntry[]
    {
        new("type", (h, e) =>
        {
            var o = ((SavedObjective) h);
            o.Type = e[0].ToInt32();
            if (o.Type == 3) {
                o.IdsOne = e[1].ToInt32();
            }
            else
            {
                o.ObjNickname = e[1].ToInt32();
                o.IdsOne = e[2].ToInt32();
                if (e.Count > 3)
                    o.IdsTwo = e[3].ToInt32();
                if(e.Count > 6)
                    o.ObjectivePosition = new Vector3(
                        e[4].ToSingle(),
                        e[5].ToSingle(),
                        e[6].ToSingle()
                    );
                if (e.Count > 7)
                    o.StringParam = e[7].ToString();
            }
        })
    };

    IEnumerable<CustomEntry> ICustomEntryHandler.CustomEntries => _custom;
}