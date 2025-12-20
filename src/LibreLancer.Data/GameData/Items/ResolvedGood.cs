// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

namespace LibreLancer.Data.GameData.Items
{
    public class ResolvedGood : IdentifiableItem
    {
        public Schema.Goods.Good Ini;
        public Equipment Equipment;
        public override string ToString()
        {
            return Nickname ?? "Invalid";
        }
    }
}
