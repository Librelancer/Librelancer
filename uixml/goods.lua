require 'ids.lua'

// List Item construction

local function good_icon_3db(name, control)
{
	control.Background = NewObject("UiRenderable")
	local colorElem = NewObject("DisplayColor")
	colorElem.Color = GetColor("black")
	control.Background.AddElement(colorElem)
	local elem = NewObject("DisplayModel")
	elem.BaseRadius = 0.052
	elem.Clip = true
	local model = NewObject("InterfaceModel")
	model.Path = name
	model.XScale = 28
	model.YScale = 29
	elem.Model = model
	control.Background.AddElement(elem)
}

local function mount_icon(mounted, enabled, onclick) 
{
	local control = NewObject("Button")
	control.Style = mounted ? "inv_mount" : "inv_unmount";
	control.Enabled = enabled
	if (onclick) control.OnClick(onclick);
	return control
}


local icons_buy = {
	good = "inv_green_square",
	bad = "inv_red_square",
	neutral = "inv_yellow_square"
}

local icons_sell = {
	good = "inv_green_circle",
	bad = "inv_red_circle",
	neutral = "inv_yellow_circle"
}

function good_list_item(good, purpose, preview, onmount) 
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
	// Item
	li.ItemMarginX = 8
	li.ItemA = NewObject("Panel")
	if (good.Icon != nil) {
		good_icon_3db(good.Icon, li.ItemA)
		if (purpose != "ship" && good.MountIcon && !preview) {
			local mount_button = mount_icon(good.IdsHardpoint != 0, good.CanMount, onmount)
			mount_button.X = 3
			mount_button.Y = 2
			li.ItemA.Children.Add(mount_button)
		}
	}
	li.ItemA.Width = 32
	li.ItemB = NewObject("Panel")
	// Name
	local tb = NewObject("TextBlock")	
	tb.HorizontalAlignment = HorizontalAlignment.Left
	tb.TextSize = 9
	tb.TextColor = GetColor("text")
	tb.TextShadow = GetColor("black")
	tb.X = 0
	tb.Y = 2
	tb.Width = 200
	tb.Height = 13
	if (good.IdsName != 0) {
		tb.InfoId = good.IdsName
		tb.Strid = good.IdsName
	}
	tb.MarginX = 3
	li.ItemB.Children.Add(tb)
	// Hardpoint
	if (good.IdsHardpoint != 0 && !preview) {
		local tbh = NewObject("TextBlock")	
		tbh.Anchor = AnchorKind.BottomLeft
		tbh.HorizontalAlignment = HorizontalAlignment.Left
		tbh.TextSize = 9
		tbh.TextColor = GetColor("text")
		tbh.TextShadow = GetColor("black")
		tbh.X = 0
		tbh.MarginX = 3
		tbh.Y = 2
		tbh.Width = 200
		tbh.Height = 13
		if (purpose == "ship")
			tbh.Text = good.IdsHardpoint;
		else
			tbh.Strid = good.IdsHardpoint;
		li.ItemB.Children.Add(tbh)
	} elseif (good.Count && good.Count > 0) {
		// Amount
		local tb2 = NewObject("TextBlock")
		tb2.HorizontalAlignment = HorizontalAlignment.Left
		tb2.TextSize = 9
		tb2.TextColor = GetColor("text")
		tb2.TextShadow = GetColor("black")
		tb2.X = 0
		tb2.Y = 2
		tb2.Anchor = AnchorKind.BottomLeft;
		tb2.Width = 200
		tb2.Height = 13
		tb2.Text = tostring(good.Count)
		tb2.MarginX = 3
		li.ItemB.Children.Add(tb2)
	}
	// Price
	if (good.Price && good.Price > 0) {
		local tb3 = NewObject("TextBlock")
		tb3.HorizontalAlignment = HorizontalAlignment.Right
		tb3.TextSize = 9
		tb3.TextColor = GetColor("text")
		tb3.TextShadow = GetColor("black")
		local offset = 17
		if (purpose == "ship" || good.PriceRank == nil)
			offset = 3;
		tb3.X = offset
		tb3.Y = 2
		tb3.Anchor = AnchorKind.BottomRight;
		tb3.Width = 200
		tb3.Height = 13
		tb3.Text = StringFromID(STRID_CREDIT_SIGN) + NumberToStringCS(good.Price, "N0")
		li.ItemB.Children.Add(tb3)
	}
	// Price Icon good/bad
	if (good.PriceRank != nil) {
		local pIcon = NewObject("Panel")
		pIcon.Anchor = AnchorKind.BottomRight;
		pIcon.Width = 13
		pIcon.Height = 11
		pIcon.X = 3
		pIcon.Y = 2
		pIcon.Background = NewObject("UiRenderable")
		local wire2 = NewObject("DisplayModel")
		local mdl = "inv_red_square"
		if (purpose == "buy") mdl = icons_buy[good.PriceRank];
		if (purpose == "sell") mdl = icons_sell[good.PriceRank];
		wire2.Model = GetModel(mdl)
		pIcon.Background.AddElement(wire2)
		li.ItemB.Children.Add(pIcon)
	}
	return li
}

