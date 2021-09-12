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

local function good_list_item(icon, strid, count, price)
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
	tb3.X = 3
	tb3.Y = 2
	tb3.Anchor = AnchorKind.BottomRight;
	tb3.Width = 200
	tb3.Height = 13
	tb3.Text = "$" .. NumberToStringCS(price, "N0")
	li.ItemB.Children:Add(tb3)
	-- Price Icon good/bad
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
		local item = good_list_item(item.Icon, item.IdsName, item.Count, item.Price)
		e.tr_list.Children:Add(item)
	end
	e.inv_list:OnSelectedIndexChanged(function()
		self:set_buysell("sell")
		PlaySound('ui_item_select')
		e.tr_list.SelectedIndex = -1
		e.item_infocard.Infocard = GetInfocard(self.PlayerGoods[e.inv_list.SelectedIndex + 1].IdsInfo, 1)
	end)
	e.tr_list:OnSelectedIndexChanged(function()
		self:set_buysell("buy")
		PlaySound('ui_item_select')
		e.inv_list.SelectedIndex = -1
		e.item_infocard.Infocard = GetInfocard(self.TraderGoods[e.tr_list.SelectedIndex + 1].IdsInfo, 1)
	end)
	e.close:OnClick(function() 
		self:Close() 
	end)
	e.btn_buysell:OnClick(function()
		if self.BuyState == "buy" then
			Game.Trader:Buy(self.TraderGoods[e.tr_list.SelectedIndex + 1].Good, 1)
		elseif self.BuyState == "sell" then
			Game.Trader:Sell(self.PlayerGoods[e.inv_list.SelectedIndex + 1].ID, 1, function()
				PlaySound('ui_receive_money')
			end)
		end
	end)
	self:set_buysell("hidden")
end

function commodity:construct_inventory()
	local e = self.Elements
	self.PlayerGoods = Game.Trader:GetPlayerGoods("commodity")
	e.inv_list.Children:Clear()
	for index, item in ipairs(self.PlayerGoods) do
		local item = good_list_item(item.Icon, item.IdsName, item.Count, item.Price)
		e.inv_list.Children:Add(item)
	end
	local str = StringFromID(STRID_CREDITS) .. NumberToStringCS(Game:GetCredits(), "N0")
	e.credits_text.Text = str
	if self.BuyState == "sell" and #self.PlayerGoods == 0 then
		self:set_buysell("hidden")
	end
end

-- Update the buy/sell button to be buy, sell, or invisible (no selection)
function commodity:set_buysell(state)
	local x = self.Elements.btn_buysell
	self.BuyState = state
	if state == "buy" then
		x.Strid = STRID_BUY
		x.Style = "trader_buy"
		x:ReloadStyle()
		x.Visible = true
	elseif state == "sell" then
		x.Strid = STRID_SELL
		x.Style = "trader_sell"
		x:ReloadStyle()
		x.Visible = true
	else
		x.Visible = false
	end
end

















