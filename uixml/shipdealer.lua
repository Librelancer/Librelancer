require 'childwindow.lua'
require 'goods.lua'

function shipdealer:ctor()
	MakeChildWindow(self)	
	LoadShipClassNames()
	local e = self.Elements
	e.tship_list:OnSelectedIndexChanged(function()
		PlaySound('ui_item_select')
		e.pship_list.SelectedIndex = -1
		e.start_buy.Visible = true
		self:PreviewShip(self.Ships[e.tship_list.SelectedIndex + 1])
	end)
	e.pship_list:OnSelectedIndexChanged(function()
		PlaySound('ui_item_select')
		e.tship_list.SelectedIndex = -1
		e.start_buy.Visible = false
		self:PreviewShip(Game.ShipDealer:PlayerShip())
	end)
	e.close:OnClick(function()
		self:Close()
	end)
	e.start_buy:OnClick(function()
		Game.ShipDealer:StartPurchase(self.Ships[e.tship_list.SelectedIndex + 1], function()
			self:OnShipPurchaseBegin()
		end)
	end)
	self.categories = {
		weapons = e.category_weapons,
		ammo = e.category_ammo,
		internal = e.category_internal,
		external = e.category_external,
		commodity = e.category_commodity
	}
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
	e.category_commodity:OnClick(function()
		PlaySound('ui_item_select')
		self:change_category("commodity")
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
		self:set_preview(self.DealerGoods[e.tr_list.SelectedIndex + 1], "buy")
		PlaySound('ui_item_select')
		e.inv_list.SelectedIndex = -1
		self:setinfocard(self.DealerGoods[e.tr_list.SelectedIndex + 1])
	end)
	e.btn_buysell:OnClick(function()
		if self.BuyState == "buy" then
			Game.ShipDealer:TransferToPlayer(self.DealerGoods[e.tr_list.SelectedIndex + 1], e.quantitySlider.Value, function()
				self:change_category(self.category)
			end)
		elseif self.BuyState == "sell" then
			Game.ShipDealer:SellToDealer(self.PlayerGoods[e.inv_list.SelectedIndex + 1], e.quantitySlider.Value, function()
				self:change_category(self.category)
			end)
		end
	end)
	e.buy_ship:OnClick(function()
		Game.ShipDealer:Purchase(function(result)
			if result == "fail" then
				PlaySound('ui_select_reject')
			end
			if result == "success" then
				PlayVoiceLine("NNVoice", "ship_purchase_complete")
			end
			if result == "successprofit" then
				PlayVoiceLine("NNVoice", "ship_purchase_complete")
				PlaySound('ui_receive_money')
			end
			self:Close()
		end)
	end)
end

function shipdealer:OnChildOpen()
	local e = self.Elements
	e.shipitems.Visible = false
	e.shiplist.Visible = true
	e.tship_list.Children:Clear()
	e.pship_list.Children:Clear()
	self.Ships = Game.ShipDealer:SoldShips()
	for index, item in ipairs(self.Ships) do
		local item = good_list_item({
			Icon = item.Icon,
			IdsName = item.IdsName,
			IdsHardpoint = ShipClassNames[item.ShipClass + 1],
			Price = item.Price
		}, "ship", false)
		e.tship_list.Children:Add(item)
	end
	local pship = Game.ShipDealer:PlayerShip()
	e.pship_list.Children:Add(good_list_item({
		Icon = pship.Icon,
		IdsName = pship.IdsName,
		IdsHardpoint = ShipClassNames[pship.ShipClass + 1],
		Price = pship.Price
	}, "ship", false))
	e.item_infocard.Infocard = nil
end

function shipdealer:OnShipPurchaseBegin()
	local e = self.Elements
	e.shiplist.Visible = false
	e.item_infocard.Infocard = nil
	e.shipitems.Visible = true
	self:change_category("weapons")
	self:set_buysell("hidden", 0)
end

function shipdealer:change_category(category)
	local e = self.Elements
	self.category = category
	e.inv_list.SelectedIndex = -1
	e.tr_list.SelectedIndex = -1
	self:set_buysell("hidden")
	self:construct_inventory()
	self.DealerGoods = Game.ShipDealer:GetDealerGoods(category)
	e.tr_list.Children:Clear()
	for index, item in ipairs(self.DealerGoods) do
		local item = good_list_item(item, "buy", false)
		e.tr_list.Children:Add(item)
	end
	for cat, button in pairs(self.categories) do
		button.Selected = (cat == category)
	end
end


function shipdealer:setinfocard(good)
	local e = self.Elements
	if good.IdsInfo ~= 0 then
		e.item_infocard.Infocard = GetInfocard(good.IdsInfo, 1)
	elseif good.IdsHardpointDescription ~= 0 then
		e.item_infocard:SetString(StringFromID(good.IdsHardpointDescription))
	end
end

function shipdealer:construct_inventory()
	local e = self.Elements
	self.PlayerGoods = Game.ShipDealer:GetPlayerGoods(self.category, false)
	e.inv_list.Children:Clear()
	for index, item in ipairs(self.PlayerGoods) do
		local item = good_list_item(item, "sell", false, function()
			Game.ShipDealer:ProcessMount(item, function(result)
				if result == "mount" then 
					PlaySound('ui_equip_mount01') 
					PlayVoiceLine("NNVoice", "mounted")
				end
				if result == "unmount" then PlaySound('ui_equip_mount02') end
				self:change_category(self.category)
			end)
		end)
		e.inv_list.Children:Add(item)
	end
	local str = StringFromID(STRID_CREDITS) .. NumberToStringCS(Game:GetCredits(), "N0")
	e.credits_text.Text = str
	e.ship_price_text.Text = StringFromID(STRID_SHIP_PRICE) .. NumberToStringCS(Game.ShipDealer:GetShipDisplayPrice(), "N0")
	if self.BuyState == "sell" and #self.PlayerGoods == 0 then
		self:set_buysell("hidden")
	end
	e.inv_list.SelectedIndex = e.inv_list.SelectedIndex -- Needs to update after changing list
	e.inv_list:ReloadStyle()
end

function shipdealer:set_preview(item, state)
	local x = self.Elements.item_preview
	local e = self.Elements
	local preview = good_list_item(item, state, true)
	self:setinfocard(item)
	if item.Price == -1 then
		self:set_buysell("hidden")
	end
	self.PreviewPrice = item.Price
	x.Children:Clear()
	preview.Width = x.Width
	preview.Height = x.Height
	preview.X = 0
	preview.Y = 0
	preview.Enabled = false
	x.Children:Add(preview)
	e.quantitySlider.Min = 1
	local max = item.Count
	e.quantitySlider.Visible = (max > 1)
	e.quantitySlider.Max = max
	e.quantitySlider.Value = e.quantitySlider.Max
	e.quantitySlider.Smooth = false
end


-- Update the buy/sell button to be buy, sell, or invisible (no selection)
function shipdealer:set_buysell(state, strid)
	local x = self.Elements.btn_buysell
	local container = self.Elements.buysell_controls
	local err = self.Elements.error_text
	local iprev = self.Elements.item_preview
	self.BuyState = state
	local e = self.Elements
	e.buy_ship.Visible = false
	e.credits_needed_text.Visible = false
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
	else
		container.Visible = false
		err.Visible = false
		iprev.Visible = false
		local cneed = Game.ShipDealer:GetRequiredCredits()
		if cneed > 0 then
			e.credits_needed_text:SetString(string.format(StringFromID(STRID_SHIP_NEEDMONEY), StringFromID(STRID_CREDIT_SIGN) .. cneed), "$Normal", 22)
			e.credits_needed_text.Visible = true
		else
			e.buy_ship.Visible = true
		end
	end
end


function shipdealer:PreviewShip(ship)
	local e = self.Elements
	e.ship_preview_panel.Visible = true
	e.ship_preview.ModelPath = ship.Model
	e.ship_name.Strid = ship.IdsName
	e.ship_class.Text = ShipClassNames[ship.ShipClass + 1]
	if ship.IdsInfo ~= 0 then
		e.item_infocard.Infocard = GetInfocard(ship.IdsInfo, 1)
	else
		e.item_infocard.Infocard = nil
	end
end

























