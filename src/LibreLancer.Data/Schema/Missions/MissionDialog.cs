// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package
using System;
using System.Collections.Generic;
using LibreLancer.Data.Ini;

namespace LibreLancer.Data.Schema.Missions;

[ParsedSection]
public partial class MissionDialog
{
    [Entry("nickname", Required = true)]
    public string Nickname = null!;
    [Entry("system", Required = true)]
    public string System = null!;

    public List<DialogLine> Lines = [];

    [EntryHandler("line", MinComponents = 3, Multiline = true)]
    private void HandleLine(Entry e)
    {
        var ln = new DialogLine()
        {
            Source = e[0].ToString(),
            Target = e[1].ToString(), Line = e[2].ToString()
        };

        if (e.Count > 3)
            ln.Unknown1 = e[3].ToInt32();
        if(e.Count > 4)
            ln.Unknown2 = e[4].ToInt32();
        Lines.Add(ln);
    }
}
public class DialogLine
{
    public string? Source;
    public string? Target;
    public string? Line;
    public OptionalArgument<int> Unknown1;
    public OptionalArgument<int> Unknown2;

    public DialogLine Clone() => (DialogLine)MemberwiseClone();
}
