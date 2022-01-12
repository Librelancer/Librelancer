require 'childwindow.lua'
require 'goods.lua'

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
				if result == "mount" then 
					PlaySound('ui_equip_mount01') 
					PlayVoiceLine("NNVoice", "mounted")
				end
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






