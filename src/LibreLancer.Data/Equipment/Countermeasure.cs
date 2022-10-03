// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using LibreLancer.Ini;

namespace LibreLancer.Data.Equipment
{
    public class Countermeasure : Munition
    {
        [Entry("range")] 
        public float Range;
        [Entry("owner_safe_time")] 
        public float OwnerSafeTime;
        [Entry("diversion_pctg")] 
        public float DiversionPercentage;
        [Entry("linear_drag")] 
        public float LinearDrag;
    }
}