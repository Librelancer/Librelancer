// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Collections.Generic;
using System.Text;
using LibreLancer.Data.Ini;

namespace LibreLancer.Data.Schema.Save;

[ParsedSection]
public partial class SaveGroup : IWriteSection
{
    [Entry("nickname", Required = true)]
    public string Nickname = null!;

    public List<SaveRep> Rep = [];

    [EntryHandler("rep", Multiline = true, MinComponents = 2)]
    private void HandleRep(Entry e) => Rep.Add(new SaveRep(e));

    public void WriteTo(IniBuilder builder)
    {
        var section = builder.Section("Group")
            .Entry("nickname", Nickname);
        foreach (var rep in Rep)
        {
            section.Entry("rep", rep.Reputation, rep.Group!);
        }
    }
}
