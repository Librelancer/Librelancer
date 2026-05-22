#nullable enable

using System;
using System.IO;
using System.Linq;
using ImGuiNET;
using LibreLancer;
using LibreLancer.ContentEdit;
using LibreLancer.Data.GameData;
using LibreLancer.Data.GameData.Items;
using LibreLancer.Data.Ini;
using LibreLancer.Data.Schema.Goods;
using LibreLancer.ImUI;
using Base = LibreLancer.Data.GameData.World.Base;

namespace LancerEdit.GameContent;

public partial class TradingPlannerTab
{
    private void DrawSelectedBaseMarket()
    {
        if (selectedBase == null)
        {
            ImGui.TextDisabled("No base selected.");
            return;
        }

        ImGui.Text(BaseDisplayName(selectedBase));
        ImGui.SameLine();
        ImGui.TextDisabled(selectedBase.System ?? "(no system)");

        ImGui.BeginDisabled(dirtyMarketBases.Count == 0);
        if (ImGui.Button($"{Icons.Save} Save commodities"))
            SaveCommodityMarkets();
        ImGui.EndDisabled();
        if (!string.IsNullOrWhiteSpace(commoditySaveStatus))
            ImGui.TextDisabled(commoditySaveStatus);

        DrawAddMarketGood(selectedBase);

        var entries = selectedBase.SoldGoods
            .Select((sold, index) => new { Sold = sold, Index = index })
            .Where(x => x.Sold.Good.Ini.Category == GoodCategory.Commodity)
            .OrderBy(x => x.Sold.Good.Nickname)
            .ToList();

        var size = ImGui.GetContentRegionAvail();
        size.Y -= 3 * ImGuiHelper.Scale;
        if (!ImGui.BeginTable("##basemarketgoods", 7,
                ImGuiTableFlags.Borders | ImGuiTableFlags.RowBg | ImGuiTableFlags.Resizable |
                ImGuiTableFlags.ScrollY | ImGuiTableFlags.NoHostExtendY,
                size))
            return;

        ImGui.TableSetupColumn("Commodity", ImGuiTableColumnFlags.WidthStretch, 2.0f);
        ImGui.TableSetupColumn("For sale", ImGuiTableColumnFlags.WidthFixed, 68 * ImGuiHelper.Scale);
        ImGui.TableSetupColumn("Price", ImGuiTableColumnFlags.WidthFixed, 80 * ImGuiHelper.Scale);
        ImGui.TableSetupColumn("Mult", ImGuiTableColumnFlags.WidthFixed, 70 * ImGuiHelper.Scale);
        ImGui.TableSetupColumn("Rank", ImGuiTableColumnFlags.WidthFixed, 55 * ImGuiHelper.Scale);
        ImGui.TableSetupColumn("Rep", ImGuiTableColumnFlags.WidthFixed, 70 * ImGuiHelper.Scale);
        ImGui.TableSetupColumn("", ImGuiTableColumnFlags.WidthFixed, 58 * ImGuiHelper.Scale);
        ImGui.TableSetupScrollFreeze(0, 1);
        ImGui.TableHeadersRow();

        for (var i = 0; i < entries.Count; i++)
        {
            var index = entries[i].Index;
            var original = selectedBase.SoldGoods[index];
            var sold = original;
            ImGui.PushID(i);
            ImGui.TableNextRow();
            ImGui.TableNextColumn();

            if (DrawCommodityGoodCombo("##marketgood", sold.Good, selected =>
                {
                    sold.Good = selected;
                    sold.Price = (ulong)selected.Ini.Price;
                }, g => g.Nickname.Equals(sold.Good.Nickname, StringComparison.OrdinalIgnoreCase) ||
                        !BaseHasGood(selectedBase, g, index)))
            {
                selectedCommodity = sold.Good.Nickname;
            }

            if (ImGui.IsItemHovered())
                ImGui.SetTooltip(CommodityDisplayName(sold.Good));
            if (ImGui.IsItemClicked())
                selectedCommodity = sold.Good.Nickname;

            ImGui.TableNextColumn();
            var forSale = sold.ForSale;
            if (ImGui.Checkbox("##forsale", ref forSale))
                sold.ForSale = forSale;

            ImGui.TableNextColumn();
            var price = sold.Price > int.MaxValue ? int.MaxValue : (int)sold.Price;
            ImGui.SetNextItemWidth(-1);
            if (ImGui.InputInt("##price", ref price, 0, 0))
                sold.Price = (ulong)Math.Clamp(price, 0, int.MaxValue);

            ImGui.TableNextColumn();
            var multiplier = sold.Good.Ini.Price <= 0 ? 0 : (float)(sold.Price / (double)sold.Good.Ini.Price);
            ImGui.SetNextItemWidth(-1);
            if (ImGui.InputFloat("##mult", ref multiplier, 0, 0, "%.2f"))
            {
                multiplier = Math.Clamp(multiplier, 0f, 100f);
                sold.Price = (ulong)Math.Round(sold.Good.Ini.Price * multiplier);
            }

            ImGui.TableNextColumn();
            var rank = sold.Rank;
            ImGui.SetNextItemWidth(-1);
            if (ImGui.InputInt("##rank", ref rank, 0, 0))
                sold.Rank = Math.Clamp(rank, 0, 100);

            ImGui.TableNextColumn();
            var rep = sold.Rep;
            ImGui.SetNextItemWidth(-1);
            if (ImGui.InputFloat("##rep", ref rep, 0, 0, "%.2f"))
                sold.Rep = Math.Clamp(rep, -1f, 1f);

            ImGui.TableNextColumn();
            if (ImGui.SmallButton("Remove"))
            {
                CommitMarketGoodRemove(selectedBase, index, original);
                ImGui.PopID();
                break;
            }

            if (!MarketGoodsEqual(original, sold))
                CommitMarketGoodSet(selectedBase, index, original, sold);
            if (ImGui.IsItemHovered())
                ImGui.SetTooltip("Remove commodity from this base");
            ImGui.PopID();
        }

        ImGui.EndTable();
    }

    private void DrawAddMarketGood(Base b)
    {
        if (commodityGoods.Count == 0)
        {
            ImGui.TextDisabled("No commodity goods available.");
            return;
        }

        if (newMarketGood == null || BaseHasGood(b, newMarketGood))
            newMarketGood = commodityGoods.FirstOrDefault(x => !BaseHasGood(b, x));
        ImGui.SetNextItemWidth(Math.Min(320 * ImGuiHelper.Scale, ImGui.GetContentRegionAvail().X * 0.62f));
        DrawCommodityGoodCombo("##newmarketgood", newMarketGood, selected => newMarketGood = selected,
            g => !BaseHasGood(b, g));

        ImGui.SameLine();
        var canAdd = newMarketGood != null &&
                     !BaseHasGood(b, newMarketGood) &&
                     data.GameData.Items.Ini.Freelancer.MarketsPaths.Count > 0;
        ImGui.BeginDisabled(!canAdd);
        if (ImGui.Button("Add commodity") && newMarketGood != null)
        {
            var sold = new BaseSoldGood(
                0,
                newMarketGood,
                0,
                (ulong)newMarketGood.Ini.Price,
                true,
                DefaultCommodityMarketPath(b));
            CommitMarketGoodAdd(b, sold);
            selectedCommodity = newMarketGood.Nickname;
            newMarketGood = commodityGoods.FirstOrDefault(x => !BaseHasGood(b, x));
        }
        ImGui.EndDisabled();
    }

    private bool DrawCommodityGoodCombo(
        string id,
        ResolvedGood? current,
        Action<ResolvedGood> set,
        Func<ResolvedGood, bool>? allow = null)
    {
        var changed = false;
        var label = current == null ? "(none)" : current.Nickname;
        if (ImGui.BeginCombo(id, label))
        {
            foreach (var good in commodityGoods)
            {
                var enabled = allow?.Invoke(good) ?? true;
                ImGui.BeginDisabled(!enabled);
                var selected = current != null &&
                               current.Nickname.Equals(good.Nickname, StringComparison.OrdinalIgnoreCase);
                if (ImGui.Selectable(good.Nickname, selected) && enabled)
                {
                    set(good);
                    changed = true;
                }

                if (ImGui.IsItemHovered())
                    ImGui.SetTooltip(CommodityDisplayName(good));
                ImGui.EndDisabled();
            }
            ImGui.EndCombo();
        }

        return changed;
    }

    private static bool BaseHasGood(Base b, ResolvedGood good, int ignoreIndex = -1)
    {
        for (var i = 0; i < b.SoldGoods.Count; i++)
        {
            if (i == ignoreIndex)
                continue;
            if (b.SoldGoods[i].Good.Nickname.Equals(good.Nickname, StringComparison.OrdinalIgnoreCase))
                return true;
        }

        return false;
    }

    private void CommitMarketGoodSet(Base b, int index, BaseSoldGood oldValue, BaseSoldGood newValue)
    {
        undoBuffer.Commit(new MarketGoodAction(
            $"Market good edit {b.Nickname}",
            b,
            () =>
            {
                if (index < b.SoldGoods.Count)
                    b.SoldGoods[index] = newValue;
            },
            () =>
            {
                if (index < b.SoldGoods.Count)
                    b.SoldGoods[index] = oldValue;
            },
            ApplyMarketEdit));
    }

    private void CommitMarketGoodAdd(Base b, BaseSoldGood sold)
    {
        undoBuffer.Commit(new MarketGoodAction(
            $"Market good add {sold.Good.Nickname}",
            b,
            () => b.SoldGoods.Add(sold),
            () =>
            {
                var index = b.SoldGoods.FindLastIndex(x =>
                    x.Good.Nickname.Equals(sold.Good.Nickname, StringComparison.OrdinalIgnoreCase));
                if (index >= 0)
                    b.SoldGoods.RemoveAt(index);
            },
            ApplyMarketEdit));
    }

    private void CommitMarketGoodRemove(Base b, int index, BaseSoldGood sold)
    {
        undoBuffer.Commit(new MarketGoodAction(
            $"Market good remove {sold.Good.Nickname}",
            b,
            () =>
            {
                if (index < b.SoldGoods.Count)
                    b.SoldGoods.RemoveAt(index);
            },
            () =>
            {
                var insertIndex = Math.Clamp(index, 0, b.SoldGoods.Count);
                b.SoldGoods.Insert(insertIndex, sold);
            },
            ApplyMarketEdit));
    }

    private static bool MarketGoodsEqual(BaseSoldGood a, BaseSoldGood b) =>
        a.Good.Nickname.Equals(b.Good.Nickname, StringComparison.OrdinalIgnoreCase) &&
        a.ForSale == b.ForSale &&
        a.Price == b.Price &&
        a.Rank == b.Rank &&
        Math.Abs(a.Rep - b.Rep) < 0.0001f;

    private void ApplyMarketEdit(Base? dirtyBase = null)
    {
        if (dirtyBase != null)
            dirtyMarketBases.Add(dirtyBase.Nickname);
        commoditySaveStatus = dirtyMarketBases.Count == 0
            ? ""
            : $"{dirtyMarketBases.Count} base market(s) modified";
        BuildMarketEntries();
        RecalculateRoutes();
    }

    private void SaveCommodityMarkets()
    {
        if (dirtyMarketBases.Count == 0)
        {
            commoditySaveStatus = "No commodity edits to save.";
            return;
        }

        var marketPaths = data.GameData.Items.Ini.Freelancer.MarketsPaths;
        if (marketPaths.Count == 0)
        {
            commoditySaveStatus = "No market files are configured.";
            return;
        }

        var vfs = data.GameData.Items.VFS;
        var dirtyBases = dirtyMarketBases
            .Select(x => allBases.FirstOrDefault(b => b.Nickname.Equals(x, StringComparison.OrdinalIgnoreCase)))
            .Where(x => x != null)
            .Cast<Base>()
            .ToDictionary(x => x.Nickname, StringComparer.OrdinalIgnoreCase);

        var savedFiles = 0;
        foreach (var marketPath in marketPaths)
        {
            var sections = IniFile.ParseFile(marketPath, vfs).ToList();
            var changed = false;
            foreach (var b in dirtyBases.Values)
            {
                var soldGoods = b.SoldGoods
                    .Where(x => string.Equals(x.SourceFile, marketPath, StringComparison.OrdinalIgnoreCase))
                    .ToList();
                changed |= MarketIniSerializer.ReplaceCommodityMarketGoods(
                    sections,
                    b,
                    soldGoods,
                    IsCommodityGood);
            }

            if (!changed)
                continue;

            var backingFile = vfs.GetBackingFileName(marketPath);
            if (string.IsNullOrWhiteSpace(backingFile))
            {
                commoditySaveStatus = $"Cannot save {marketPath}: no writable backing file.";
                return;
            }

            IniWriter.WriteIniFile(backingFile, sections);
            savedFiles++;
        }

        dirtyMarketBases.Clear();
        commoditySaveStatus = savedFiles switch
        {
            0 => "No commodity market file changes were needed.",
            1 => "Saved commodity markets to 1 file.",
            _ => $"Saved commodity markets to {savedFiles} files."
        };
        window.OnSaved();
    }

    private bool IsCommodityGood(string goodName) =>
        data.GameData.Items.Goods.TryGetValue(goodName, out var good) &&
        good.Ini.Category == GoodCategory.Commodity;

    private string DefaultCommodityMarketPath(Base b) =>
        b.SoldGoods
            .Where(x => x.Good.Ini.Category == GoodCategory.Commodity)
            .Select(x => x.SourceFile)
            .FirstOrDefault(x => !string.IsNullOrWhiteSpace(x)) ??
        data.GameData.Items.Ini.Freelancer.MarketsPaths.FirstOrDefault(x =>
            Path.GetFileName(x.Replace('\\', Path.DirectorySeparatorChar))
                .Equals("market_commodities.ini", StringComparison.OrdinalIgnoreCase)) ??
        data.GameData.Items.Ini.Freelancer.MarketsPaths.First();
}
