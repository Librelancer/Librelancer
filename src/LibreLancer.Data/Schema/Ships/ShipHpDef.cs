// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System.Linq;
using LibreLancer.Data.Ini;

namespace LibreLancer.Data.Schema.Ships
{
    public class ShipHpDef
    {
        public string Type;
        public string[] Hardpoints;

        public ShipHpDef()
        {
        }

        public ShipHpDef(Entry e)
        {
            Type = e[0].ToString();
            Hardpoints = e.Skip(1).Select(x => x.ToString()).ToArray();
        }
    }
}
