// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package
using System;
using System.Collections.Generic;
using LibreLancer.Data.Ini;
using LibreLancer.Data.IO;

namespace LibreLancer.Data.Schema.Fuses
{
    [ParsedIni]
    public partial class FuseIni
    {
        [Section("fuse")]
        [Section("start_effect", Type = typeof(FuseStartEffect), Child = true)]
        [Section("destroy_group", Type = typeof(FuseDestroyGroup), Child = true)]
        [Section("destroy_hp_attachment", Type = typeof(FuseDestroyHpAttachment), Child = true)]
        [Section("start_cam_particles", Type = typeof(FuseStartCamParticles), Child = true)]
        [Section("ignite_fuse", Type = typeof(FuseIgniteFuse), Child = true)]
        [Section("impulse", Type = typeof(FuseImpulse), Child = true)]
        [Section("destroy_root", Type = typeof(FuseDestroyRoot), Child = true)]
        public List<Fuse> Fuses = new();
        public void AddFuseIni(string path, FileSystem vfs, IniStringPool stringPool = null)
        {
            ParseIni(path, vfs, stringPool);
        }

    }
}
