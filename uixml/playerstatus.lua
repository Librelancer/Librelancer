require 'childwindow'
require 'ids'

local function faction_gauge(negative, reputation)
{
	local g = NewObject("Gauge")
	g.Width = 60
	g.Height = 10
	g.X = negative ? -30 : 30;
	g.Y = 4
	g.Reverse = negative
	g.Anchor = AnchorKind.BottomCenter;
	if(reputation < 0 && negative) g.PercentFilled = -reputation;
	if(reputation > 0 && !negative) g.PercentFilled = reputation;
	// Background
	g.Background = NewObject("UiRenderable")
	local wireBkg = NewObject("DisplayModel")
	wireBkg.Model = GetModel(negative ? "relation_hostile" : "relation_friendly")
	wireBkg.VMeshWire = true
	wireBkg.DrawModel = false
	
	wireBkg.WireframeColor = GetColor("text");
	g.Background.AddElement(wireBkg)

	g.Fill = NewObject("UiRenderable")
	local fill = NewObject("DisplayModel")
	fill.Model = GetModel(negative ? "relation_hostile" : "relation_friendly")
	g.Fill.AddElement(fill)
	return g;
}
local function faction_list_item(ids, reputation)
{
	local li = NewObject("ListItem")
	// Border
	li.Border = NewObject("UiRenderable")
	local wire = NewObject("DisplayWireBorder")
	wire.Color = GetColor("text")
	li.Border.AddElement(wire)
	li.HoverBorder = NewObject("UiRenderable")
	wire = NewObject("DisplayWireBorder")
	wire.Color = GetColor("slow_blue_yellow")
	li.HoverBorder.AddElement(wire)
	li.SelectedBorder = NewObject("UiRenderable")
	wire = NewObject("DisplayWireBorder")
	wire.Color = GetColor("yellow")
	li.SelectedBorder.AddElement(wire)
	//Contents
	li.ItemA = NewObject("Panel")
	local ta = NewObject("TextBlock")
	ta.TextSize = 9
	ta.HorizontalAlignment = HorizontalAlignment.Center
	ta.VerticalAlignment = VerticalAlignment.Top
	local color = "color_neutral";
	if(reputation <= -0.4) color = "color_hostile";
	if(reputation >= 0.4) color = "color_friendly";
	ta.TextColor = GetColor(color)
	ta.TextShadow = GetColor("black")
	ta.Fill = true
	ta.Strid = ids
	li.ItemA.Children.Add(ta)
	li.ItemA.Children.Add(faction_gauge(false, reputation))
	li.ItemA.Children.Add(faction_gauge(true, reputation))
	return li;
}

class StatusData
{
    StatusData(data)
    {
        this.data = data;
    }
    GetCount()
    {
        return this.data.length;
    }
    GetString(row, column)
    {
        local row = this.data[row + 1];
        if (row == nil)
        {
            return nil;
        }
        local getter = row[column];
        if(getter == nil)
        {
            return nil;
        }
        return type(getter) == 'function'
            ? getter()
            : getter;
    }
    GetSelected()
    {
        return -1;
    }
    SetSelected(selected)
    {
    }
    ValidSelection()
    {
        return false;
    }
}

class playerstatus : playerstatus_Designer with ChildWindow
{

    playerstatus()
    {
        base();
        this.ChildWindowInit();
        this.Elements.close.OnClick(() => this.Close());
		this.OnChildOpen();
    }

	OnChildOpen()
	{
		local e = this.Elements;
		local facs = Game.GetPlayerRelations();
		e.factions.Children.Clear();
		for(f in facs) {
			e.factions.Children.Add(faction_list_item(f.IdsName, f.Relationship));
		}
		local missionString = StringFromID(1568);
        local creditString = StringFromID(STRID_CREDIT_SIGN);
        local formatMoney = (credits) => creditString + NumberToStringCS(credits, "N0");

		local playerData = {
            { "name": StringFromID(1565), "value": () => Game.CurrentRank }, // current level
            { "name": StringFromID(1566), "value": () => formatMoney(Game.NetWorth) }, // current worth
            { "name": StringFromID(1567), "value": () => Game.NextLevelWorth > 0 ? formatMoney(Game.NextLevelWorth) : missionString }, // next level requirement
            { "name": "", "value": "" }, // blank line
            { "name": StringFromID(1611), "value": () => Game.Statistics.TotalMissions }, // total missions
            { "name": StringFromID(1601), "value": () => Game.Statistics.TotalKills }, // total kills
            { "name": StringFromID(1606), "value": "0" }, // total time
            { "name": StringFromID(1607), "value": () => Game.Statistics.SystemsVisited }, // systems visited
            { "name": StringFromID(1608), "value": () => Game.Statistics.BasesVisited }, // bases visited
            { "name": StringFromID(1609), "value": () => Game.Statistics.JumpHolesFound }, // jump holes found
            { "name": "", "value": "" }, // blank line
            { "name": StringFromID(1610), "value": () => Game.Statistics.FightersKilled }, // fighters killed
            { "name": StringFromID(1612), "value": () => Game.Statistics.FreightersKilled }, // freighters killed
            { "name": StringFromID(1613), "value": () => Game.Statistics.TransportsKilled }, // transports killed
            { "name": StringFromID(1614), "value": () => Game.Statistics.BattleshipsKilled }, // battleships killed
        }
        e.stats.SetData(new StatusData(playerData));
	}
}

