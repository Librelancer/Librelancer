// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System.Collections.Generic;
using LibreLancer.Utf.Cmp;

namespace LibreLancer
{
    public class HardpointLookup
    {
        private Dictionary<uint, string> crcTable;
        static IEnumerable<string> HardpointList(IDrawable dr)
        {
            if(dr is ModelFile)
            {
                var mdl = (ModelFile)dr;
                foreach (var hp in mdl.Hardpoints)
                    yield return hp.Name;
            }
            else if (dr is CmpFile)
            {
                var cmp = (CmpFile)dr;
                foreach(var model in cmp.Models.Values)
                {
                    foreach (var hp in model.Hardpoints)
                        yield return hp.Name;
                }
            }
        }
        public HardpointLookup(IDrawable dr)
        {
            crcTable = new Dictionary<uint, string>();
            crcTable.Add(NetShipCargo.InternalCrc, "internal");
            foreach (var hp in HardpointList(dr))
                crcTable.Add(CrcTool.HardpointCrc(hp), hp);
        }

        public string GetHardpoint(uint crc)
        {
            if (crc == 0) return null;
            crcTable.TryGetValue(crc, out var hp);
            return hp;
        }
    }
}