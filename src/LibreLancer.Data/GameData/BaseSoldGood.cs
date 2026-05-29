// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using LibreLancer.Data.GameData.Items;

namespace LibreLancer.Data.GameData;

public record struct BaseSoldGood(int Rank, ResolvedGood Good, float Rep, ulong Price, bool ForSale, string SourceFile);
