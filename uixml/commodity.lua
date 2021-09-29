require 'childwindow.lua'
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

local function good_list_item(good, purpose, preview, onmount)
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
		if good.MountIcon and not preview then
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
		tbh.Strid = good.IdsHardpoint
		li.ItemB.Children:Add(tbh)
	elseif good.Count > 0 then
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
	if good.Price > 0 then
		local tb3 = NewObject("TextBlock")
		tb3.HorizontalAlignment = HorizontalAlignment.Right
		tb3.TextSize = 9
		tb3.TextColor = GetColor("text")
		tb3.TextShadow = GetColor("black")
		local offset = 17
		if good.PriceRank == nil then
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


function commodity:setinfocard(good)
	local e = self.Elements
	if good.IdsInfo ~= 0 then
		e.item_infocard.Infocard = GetInfocard(good.IdsInfo, 1)
	elseif good.IdsHardpointDescription ~= 0 then
		e.item_infocard:SetString(StringFromID(good.IdsHardpointDescription))
	end
end

function commodity:change_category(category)
	local e = self.Elements
	self.category = category
	e.inv_list.SelectedIndex = -1
	e.tr_list.SelectedIndex = -1
	self:set_buysell("hidden")
	self:construct_inventory()
	self.TraderGoods = Game.Trader:GetTraderGoods(category)
	e.tr_list.Children:Clear()
	for index, item in ipairs(self.TraderGoods) do
		local item = good_list_item(item, "buy", false)
		e.tr_list.Children:Add(item)
	end
	for cat, button in pairs(self.categories) do
		button.Selected = (cat == category)
	end
end

function commodity:ctor(kind)
	MakeChildWindow(self)
	Game.Trader:OnUpdateInventory(function() 
		self:construct_inventory()
	end)
	local e = self.Elements
	self.categories = {
		weapons = e.category_weapons,
		ammo = e.category_ammo,
		internal = e.category_internal,
		external = e.category_external
	}
	if kind == "commodity" then
		self:change_category("commodity")
		e.inv_categories.Visible = false
	else
		self:change_category("weapons")
	end
	e.category_weapons:OnClick(function()
		PlaySound('ui_item_select')
		self:change_category("weapons")
	end)
	e.category_ammo:OnClick(function()
		PlaySound('ui_item_select')
		self:change_category("ammo")
	end)
	e.category_internal:OnClick(function()
		PlaySound('ui_item_select')
		self:change_category("internal")
	end)
	e.category_external:OnClick(function()
		PlaySound('ui_item_select')
		self:change_category("external")
	end)
	e.inv_list:OnSelectedIndexChanged(function()
		self:set_buysell("sell")
		self:set_preview(self.PlayerGoods[e.inv_list.SelectedIndex + 1], "sell")
		PlaySound('ui_item_select')
		e.tr_list.SelectedIndex = -1
		self:setinfocard(self.PlayerGoods[e.inv_list.SelectedIndex + 1])
	end)
	e.tr_list:OnSelectedIndexChanged(function()
		self:set_buysell("buy")
		self:set_preview(self.TraderGoods[e.tr_list.SelectedIndex + 1], "buy")
		PlaySound('ui_item_select')
		e.inv_list.SelectedIndex = -1
		self:setinfocard(self.TraderGoods[e.tr_list.SelectedIndex + 1])
	end)
	e.close:OnClick(function() 
		self:Close() 
	end)
	e.btn_buysell:OnClick(function()
		if self.BuyState == "buy" then
			Game.Trader:Buy(self.TraderGoods[e.tr_list.SelectedIndex + 1].Good, e.quantitySlider.Value, function()
				self:set_preview(self.TraderGoods[e.tr_list.SelectedIndex + 1], "buy")
				PlaySound('ui_buy_commodity')
			end)
		elseif self.BuyState == "sell" then
			local doUpdate = e.quantitySlider.Value < self.PlayerGoods[e.inv_list.SelectedIndex + 1].Count;
			Game.Trader:Sell(self.PlayerGoods[e.inv_list.SelectedIndex + 1], e.quantitySlider.Value, function()
				if doUpdate then 
					self:set_preview(self.PlayerGoods[e.inv_list.SelectedIndex + 1], "sell")
				else
					self:set_buysell("hidden")
				end
				PlaySound('ui_receive_money')
			end)
		end
	end)
	self:set_buysell("hidden")
	e.item_preview:OnUpdate(function(delta)
		self:PreviewUpdate(delta)
	end)
end

function commodity:set_preview(item, state)
	if state == "buy" and item.Price > Game:GetCredits() then
		self:set_buysell("error", STRID_INSUFFICIENT_CREDITS)
	elseif item.Price == 0 then
		self:set_buysell("hidden")
	end
	local x = self.Elements.item_preview
	local e = self.Elements
	local preview = good_list_item(item, state, true)
	self.PreviewPrice = item.Price
	x.Children:Clear()
	preview.Width = x.Width
	preview.Height = x.Height
	preview.X = 0
	preview.Y = 0
	preview.Enabled = false
	x.Children:Add(preview)
	e.quantitySlider.Min = 1
	local max = math.floor(Game:GetCredits() / item.Price)
	if state == "sell" then
		max = item.Count
	end
	if not item.Combinable then
		max = 1
	end
	e.quantitySlider.Visible = (max > 1)
	e.quantitySlider.Max = max
	e.quantitySlider.Value = e.quantitySlider.Max
	e.quantitySlider.Smooth = false
	e.unit_price.Text = StringFromID(STRID_CREDIT_SIGN) .. NumberToStringCS(item.Price, "N0")
end

function commodity:PreviewUpdate(delta)
	local e = self.Elements
	e.quantity_label.Text = tostring(e.quantitySlider.Value)
	local prefix = StringFromID(STRID_NEG_CREDITS)
	if self.BuyState == "sell" then
		prefix = StringFromID(STRID_ADD_CREDITS)
	end
	e.price_total.Text = prefix .. NumberToStringCS(math.floor(e.quantitySlider.Value) * self.PreviewPrice, "N0")
end

function commodity:construct_inventory()
	local e = self.Elements
	self.PlayerGoods = Game.Trader:GetPlayerGoods(self.category, false)
	e.inv_list.Children:Clear()
	for index, item in ipairs(self.PlayerGoods) do
		local item = good_list_item(item, "sell", false, function()
			Game.Trader:ProcessMount(item, function(result)
				if result == "mount" then PlaySound('ui_equip_mount01') end
				if result == "unmount" then PlaySound('ui_equip_mount02') end
			end)
		end)
		e.inv_list.Children:Add(item)
	end
	local str = StringFromID(STRID_CREDITS) .. NumberToStringCS(Game:GetCredits(), "N0")
	e.credits_text.Text = str
	if self.BuyState == "sell" and #self.PlayerGoods == 0 then
		self:set_buysell("hidden")
	end
	e.inv_list.SelectedIndex = e.inv_list.SelectedIndex -- Needs to update after changing list
	e.inv_list:ReloadStyle()
end

-- Update the buy/sell button to be buy, sell, or invisible (no selection)
function commodity:set_buysell(state, strid)
	local x = self.Elements.btn_buysell
	local container = self.Elements.buysell_controls
	local err = self.Elements.error_text
	local iprev = self.Elements.item_preview
	self.BuyState = state
	if state == "buy" then
		x.Strid = STRID_BUY
		x.Style = "trader_buy"
		x:ReloadStyle()
		container.Visible = true
		err.Visible = false
		iprev.Visible = true
	elseif state == "sell" then
		x.Strid = STRID_SELL
		x.Style = "trader_sell"
		x:ReloadStyle()
		container.Visible = true
		err.Visible = false
		iprev.Visible = true
	elseif state == "error" then
		container.Visible = false
		err:SetString(StringFromID(strid), "$Normal", 26)
		iprev.Visible = true
		err.Visible = true
	else
		container.Visible = false
		err.Visible = false
		iprev.Visible = false
	end
end




