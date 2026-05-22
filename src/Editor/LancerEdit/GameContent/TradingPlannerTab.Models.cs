#nullable enable

using System;
using System.Collections.Generic;
using System.Numerics;
using LibreLancer;
using LibreLancer.Data.GameData;
using LibreLancer.Data.GameData.Items;
using LibreLancer.ImUI;
using Base = LibreLancer.Data.GameData.World.Base;
using StarSystem = LibreLancer.Data.GameData.World.StarSystem;

namespace LancerEdit.GameContent;

public partial class TradingPlannerTab
{
    private sealed class PlannerSystem(StarSystem system, Vector2 position) : EditorSystem(system, position)
    {
    }

    private sealed class PlannerConnection(PlannerSystem source, PlannerSystem target, bool legal)
    {
        public PlannerSystem Source = source;
        public PlannerSystem Target = target;
        public bool Legal = legal;
    }

    private sealed class TradeMarketEntry
    {
        public required Base Base;
        public required PlannerSystem System;
        public required ResolvedGood Good;
        public required bool ForSale;
        public required ulong Price;
        public required double Multiplier;
        public required int Rank;
        public required float Rep;
        public Vector3? LocalPosition;
    }

    private sealed class TradeCommodity
    {
        public required ResolvedGood Good;
        public List<TradeMarketEntry> Markets = [];
        public List<TradeRoute> Routes = [];

        public string Nickname => Good.Nickname;
        public int BasePrice => Good.Ini.Price;
    }

    private sealed class TradeRoute
    {
        public required TradeCommodity Commodity;
        public required TradeMarketEntry Seller;
        public required TradeMarketEntry Buyer;
        public required List<PlannerSystem> Path;
        public required int Hops;
        public required double Distance;
        public required double ProfitPerUnit;
    }

    private sealed class MarketGoodAction(
        string name,
        Base b,
        Action commit,
        Action undo,
        Action<Base> changed) : EditorAction
    {
        public override void Commit()
        {
            commit();
            changed(b);
        }

        public override void Undo()
        {
            undo();
            changed(b);
        }

        public override string ToString() => name;
    }

    private sealed class TradingPlannerSaveStrategy(TradingPlannerTab tab) : ISaveStrategy
    {
        public void Save() => tab.SaveCommodityMarkets();

        public void DrawMenuOptions()
        {
            if (Theme.IconMenuItem(Icons.Save, "Save Commodities", tab.dirtyMarketBases.Count > 0))
                Save();
            Theme.IconMenuItem(Icons.Save, "Save As", false);
        }

        public bool ShouldSave => tab.dirtyMarketBases.Count > 0;
    }
}
