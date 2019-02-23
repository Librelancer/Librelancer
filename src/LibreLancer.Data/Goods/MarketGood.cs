// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package
    
using System;
using LibreLancer.Ini;
namespace LibreLancer.Data.Goods
{
    public class MarketGood
    {
        public string Good;
        public MarketGood() { }
        public MarketGood(Entry e)
        {
            Good = e[0].ToString();
        }
    }
}
