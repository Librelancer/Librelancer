require 'childwindow'

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
	}
}

