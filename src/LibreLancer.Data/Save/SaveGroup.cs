// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Collections.Generic;
using System.Text;
using LibreLancer.Ini;
namespace LibreLancer.Data.Save
{
    public class SaveGroup : IWriteSection
    {
        [Entry("nickname")]
        public string Nickname;

        public List<SaveRep> Rep = new List<SaveRep>();

        [EntryHandler("rep", Multiline = true, MinComponents = 2)]
        void HandleRep(Entry e) => Rep.Add(new SaveRep(e));
        
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
