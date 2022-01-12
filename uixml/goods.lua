require 'ids.lua'

-- List Item construction

local function good_icon_3db(name, control)
	control.Background = NewObject("UiRenderable")
	local colorElem = NewObject("DisplayColor")
	colorElem.Color = GetColor("black")
	control.Background:AddElement(colorElem)
	local elem = NewObject("DisplayModel")
	elem.BaseRadius = 0.052
	elem.Clip = true
	elem.Tint = GetColor("text")
	local model = NewObject("InterfaceModel")
	model.Path = name
	model.XScale = 28
	model.YScale = 29
	elem.Model = model
	control.Background:AddElement(elem)
end

local function mount_icon(mounted, enabled, onclick)
	local control = NewObject("Button")
	if mounted then
		control.Style = "inv_mount"
	else
		control.Style = "inv_unmount"
	end
	control.Enabled = enabled
	if onclick then
		control:OnClick(onclick)
	end
	return control
end


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
	local li = NewObject("ListItem")
	-- Border
	li.Border = NewObject("UiRenderable")
	local wire = NewObject("DisplayWireBorder")
	wire.Color = GetColor("text")
	li.Border:AddElement(wire)
	li.HoverBorder = NewObject("UiRenderable")
	wire = NewObject("DisplayWireBorder")
	wire.Color = GetColor("slow_blue_yellow")
	li.HoverBorder:AddElement(wire)
	li.SelectedBorder = NewObject("UiRenderable")
	wire = NewObject("DisplayWireBorder")
	wire.Color = GetColor("yellow")
	li.SelectedBorder:AddElement(wire)
	-- Item
	li.ItemMarginX = 8
	li.ItemA = NewObject("Panel")
	if good.Icon ~= nil then
		good_icon_3db(good.Icon, li.ItemA)
		if purpose ~= "ship" and good.MountIcon and not preview then
			local mount_button = mount_icon(good.IdsHardpoint ~= 0, good.CanMount, onmount)
			mount_button.X = 3
			mount_button.Y = 2
			li.ItemA.Children:Add(mount_button)
		end
	end
	li.ItemA.Width = 32
	li.ItemB = NewObject("Panel")
	-- Name
	local tb = NewObject("TextBlock")	
	tb.HorizontalAlignment = HorizontalAlignment.Left
	tb.TextSize = 9
	tb.TextColor = GetColor("text")
	tb.TextShadow = GetColor("black")
	tb.X = 0
	tb.Y = 2
	tb.Width = 200
	tb.Height = 13
	if good.IdsName ~= 0 then
		tb.InfoId = good.IdsName
		tb.Strid = good.IdsName
	end
	tb.MarginX = 3
	li.ItemB.Children:Add(tb)
	-- Hardpoint
	if good.IdsHardpoint ~= 0 and not preview then
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
		if purpose == "ship" then
			tbh.Text = good.IdsHardpoint
		else
			tbh.Strid = good.IdsHardpoint
		end
		li.ItemB.Children:Add(tbh)
	elseif good.Count and good.Count > 0 then
		-- Amount
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
		li.ItemB.Children:Add(tb2)
	end
	-- Price
	if good.Price and good.Price > 0 then
		local tb3 = NewObject("TextBlock")
		tb3.HorizontalAlignment = HorizontalAlignment.Right
		tb3.TextSize = 9
		tb3.TextColor = GetColor("text")
		tb3.TextShadow = GetColor("black")
		local offset = 17
		if purpose == "ship" or good.PriceRank == nil then
			offset = 3
		end
		tb3.X = offset
		tb3.Y = 2
		tb3.Anchor = AnchorKind.BottomRight;
		tb3.Width = 200
		tb3.Height = 13
		tb3.Text = StringFromID(STRID_CREDIT_SIGN) .. NumberToStringCS(good.Price, "N0")
		li.ItemB.Children:Add(tb3)
	end
	-- Price Icon good/bad
	if good.PriceRank ~= nil then
		local pIcon = NewObject("Panel")
		pIcon.Anchor = AnchorKind.BottomRight;
		pIcon.Width = 13
		pIcon.Height = 11
		pIcon.X = 3
		pIcon.Y = 2
		pIcon.Background = NewObject("UiRenderable")
		local wire2 = NewObject("DisplayModel")
		local mdl = "inv_red_square"
		if purpose == "buy" then mdl = icons_buy[good.PriceRank] end
		if purpose == "sell" then mdl = icons_sell[good.PriceRank] end
		wire2.Model = GetModel(mdl)
		pIcon.Background:AddElement(wire2)
		li.ItemB.Children:Add(pIcon)
	end
	return li
end

