// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Collections.Generic;
using System.Text;
using LibreLancer.Ini;
namespace LibreLancer.Data.Save
{
    public class SaveGroup : ICustomEntryHandler, IWriteSection
    {
        [Entry("nickname")]
        public string Nickname;

        public List<SaveRep> Rep = new List<SaveRep>();

        private static readonly CustomEntry[] _custom = new CustomEntry[]
        {
            new("rep", (s,e) => ((SaveGroup)s).Rep.Add(new SaveRep(e)))
        };

        IEnumerable<CustomEntry> ICustomEntryHandler.CustomEntries => _custom;
        public void WriteTo(StringBuilder builder)
        {
            builder.AppendLine("[Group]")
                .AppendEntry("nickname", Nickname);
            foreach (var rep in Rep)
            {
                builder.AppendEntry("rep", rep.Reputation, rep.Group);
            }
            builder.AppendLine();
        }
    }
}
