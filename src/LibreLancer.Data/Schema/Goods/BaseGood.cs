// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Collections.Generic;
using LibreLancer.Data.Ini;

namespace LibreLancer.Data.Schema.Goods;

[ParsedSection]
public partial class BaseGood
{
    [Entry("base", Required = true)]
    public string? Base = null!;

    public List<MarketGood> MarketGoods = [];

    [EntryHandler("marketgood", MinComponents = 7, Multiline = true)]
    private void HandleMarketGood(Entry e) => MarketGoods.Add(new MarketGood(e));

}
