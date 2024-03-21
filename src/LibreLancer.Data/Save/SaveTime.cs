// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Text;
using LibreLancer.Ini;
namespace LibreLancer.Data.Save
{
    public class SaveTime : IWriteSection
    {
        [Entry("seconds")]
        public float Seconds;

        public void WriteTo(IniBuilder builder)
        {
            builder.Section("SaveTime")
                .Entry("seconds", Seconds);
        }
    }
}
