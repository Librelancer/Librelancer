// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using LibreLancer.Data.GameData.Items;

namespace LibreLancer.Data.GameData;

public struct BaseSoldGood
{
    public required int Rank;
    public required ResolvedGood Good;
    public required float Rep;
    public required ulong Price;
    public required bool ForSale;
}
