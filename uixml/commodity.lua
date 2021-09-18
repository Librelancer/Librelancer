require 'childwindow.lua'
require 'ids.lua'

-- List Item construction

local function good_icon_3db(name, control)
	control.Background = NewObject("UiRenderable")
	local colorElem = NewObject("DisplayColor")
	colorElem.Color = GetColor("black")
	control.Background:AddElement(colorElem)
	local elem = NewObject("DisplayModel")
	elem.Tint = GetColor("text")
	local model = NewObject("InterfaceModel")
	model.Path = name
	model.XScale = 28
	model.YScale = 29
	elem.Model = model
	control.Background:AddElement(elem)
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

local function good_list_item(icon, strid, count, price, rank, purpose)
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
	good_icon_3db(icon, li.ItemA)
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
	tb.InfoId = strid
	tb.Strid = strid
	tb.MarginX = 3
	li.ItemB.Children:Add(tb)
	-- Amount
	if count > 0 then
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
		tb2.Text = tostring(count)
		tb2.MarginX = 3
		li.ItemB.Children:Add(tb2)
	end
	-- Price
	local tb3 = NewObject("TextBlock")
	tb3.HorizontalAlignment = HorizontalAlignment.Right
	tb3.TextSize = 9
	tb3.TextColor = GetColor("text")
	tb3.TextShadow = GetColor("black")
	tb3.X = 17
	tb3.Y = 2
	tb3.Anchor = AnchorKind.BottomRight;
	tb3.Width = 200
	tb3.Height = 13
	tb3.Text = "$" .. NumberToStringCS(price, "N0")
	li.ItemB.Children:Add(tb3)
	-- Price Icon good/bad
	local pIcon = NewObject("Panel")
	pIcon.Anchor = AnchorKind.BottomRight;
	pIcon.Width = 13
	pIcon.Height = 11
	pIcon.X = 3
	pIcon.Y = 2
	pIcon.Background = NewObject("UiRenderable")
	local wire2 = NewObject("DisplayModel")
	local mdl = "inv_red_square"
	if purpose == "buy" then mdl = icons_buy[rank] end
	if purpose == "sell" then mdl = icons_sell[rank] end
	wire2.Model = GetModel(mdl)
	pIcon.Background:AddElement(wire2)
	li.ItemB.Children:Add(pIcon)
	return li
end



function commodity:ctor()
	MakeChildWindow(self)
	self.TraderGoods = Game.Trader:GetTraderGoods("commodity")
	Game.Trader:OnUpdateInventory(function() 
		self:construct_inventory()
	end)
	local e = self.Elements
	self:construct_inventory()
	for index, item in ipairs(self.TraderGoods) do
		local item = good_list_item(item.Icon, item.IdsName, item.Count, item.Price, item.PriceRank, "buy")
		e.tr_list.Children:Add(item)
	end
	e.inv_list:OnSelectedIndexChanged(function()
		self:set_buysell("sell")
		self:set_preview(self.PlayerGoods[e.inv_list.SelectedIndex + 1], "sell")
		PlaySound('ui_item_select')
		e.tr_list.SelectedIndex = -1
		e.item_infocard.Infocard = GetInfocard(self.PlayerGoods[e.inv_list.SelectedIndex + 1].IdsInfo, 1)
	end)
	e.tr_list:OnSelectedIndexChanged(function()
		self:set_buysell("buy")
		self:set_preview(self.TraderGoods[e.tr_list.SelectedIndex + 1], "buy")
		PlaySound('ui_item_select')
		e.inv_list.SelectedIndex = -1
		e.item_infocard.Infocard = GetInfocard(self.TraderGoods[e.tr_list.SelectedIndex + 1].IdsInfo, 1)
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
			Game.Trader:Sell(self.PlayerGoods[e.inv_list.SelectedIndex + 1].ID, e.quantitySlider.Value, function()
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
	end
	local x = self.Elements.item_preview
	local e = self.Elements
	local preview = good_list_item(item.Icon, item.IdsName, item.Count, item.Price, item.PriceRank, state)
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
	e.quantitySlider.Max = max
	e.quantitySlider.Value = e.quantitySlider.Max
	e.quantitySlider.Smooth = false
	e.unit_price.Text = '$' .. NumberToStringCS(item.Price, "N0")
end

function commodity:PreviewUpdate(delta)
	local e = self.Elements
	e.quantity_label.Text = tostring(e.quantitySlider.Value)
	e.price_total.Text = '$' .. NumberToStringCS(math.floor(e.quantitySlider.Value) * self.PreviewPrice, "N0")
end

function commodity:construct_inventory()
	local e = self.Elements
	self.PlayerGoods = Game.Trader:GetPlayerGoods("commodity")
	e.inv_list.Children:Clear()
	for index, item in ipairs(self.PlayerGoods) do
		local item = good_list_item(item.Icon, item.IdsName, item.Count, item.Price, item.PriceRank, "sell")
		e.inv_list.Children:Add(item)
	end
	local str = StringFromID(STRID_CREDITS) .. NumberToStringCS(Game:GetCredits(), "N0")
	e.credits_text.Text = str
	if self.BuyState == "sell" and #self.PlayerGoods == 0 then
		self:set_buysell("hidden")
	end
	e.inv_list.SelectedIndex = e.inv_list.SelectedIndex -- Needs to update after changing list
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



























