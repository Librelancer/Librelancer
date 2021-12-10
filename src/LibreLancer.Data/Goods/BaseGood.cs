// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package
    
using System;
using System.Collections.Generic;
using LibreLancer.Ini;
namespace LibreLancer.Data.Goods
{
    public class BaseGood : ICustomEntryHandler
    {
        [Entry("base", Required = true)]
        public string Base;

        public List<MarketGood> MarketGoods = new List<MarketGood>();

        
        private static readonly CustomEntry[] _custom = new CustomEntry[]
        {
            new("marketgood", (s,e) => ((BaseGood)s).MarketGoods.Add(new MarketGood(e)))
        };

        IEnumerable<CustomEntry> ICustomEntryHandler.CustomEntries => _custom;
    }
}
