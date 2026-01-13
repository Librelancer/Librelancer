// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package
using System;
using System.Collections.Generic;
using LibreLancer.Data.Ini;

namespace LibreLancer.Data.Schema.Missions;

[ParsedSection]
public partial class ObjList : IEntryHandler
{
    [Entry("nickname", Required = true)]
    public string Nickname = null!;
    [Entry("system")]
    public string? System;
    public List<ObjCmd> Commands = [];


    bool IEntryHandler.HandleEntry(Entry e)
    {
        if (!Enum.TryParse<ObjListCommands>(e.Name, true, out var c))
        {
            return false;
        }

        Commands.Add(new ObjCmd() { Command = c, Entry = e });
        return true;
    }
}

public class ObjCmd
{
    public ObjListCommands Command;
    public Entry? Entry;
}
