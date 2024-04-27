// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package
using System;
using System.Collections.Generic;
using LibreLancer.Data.IO;
using LibreLancer.Ini;
namespace LibreLancer.Data.Fuses
{
    public class FuseIni : IniFile
    {
        public Dictionary<string, Fuse> Fuses = new Dictionary<string, Fuse>(StringComparer.OrdinalIgnoreCase);

        public void AddFuseIni(string path, FileSystem vfs)
        {
            Fuse current = null;
            foreach(var section in ParseFile(path, vfs))
            {
                switch(section.Name.ToLowerInvariant())
                {
                    case "fuse":
                        current = FromSection<Fuse>(section);
                        Fuses[current.Name] = current;
                        break;
                    case "start_effect":
                        current.Actions.Add(FromSection<FuseStartEffect>(section));
                        break;
                    case "destroy_group":
                        current.Actions.Add(FromSection<FuseDestroyGroup>(section));
                        break;
                    case "destroy_hp_attachment":
                        current.Actions.Add(FromSection<FuseDestroyHpAttachment>(section));
                        break;
                    case "start_cam_particles":
                        current.Actions.Add(FromSection<FuseStartCamParticles>(section));
                        break;
                    case "ignite_fuse":
                        current.Actions.Add(FromSection<FuseIgniteFuse>(section));
                        break;
                    case "impulse":
                        current.Actions.Add(FromSection<FuseImpulse>(section));
                        break;
                    case "destroy_root":
                        current.Actions.Add(FromSection<FuseDestroyRoot>(section));
                        break;
                }
            }
        }

    }
}
