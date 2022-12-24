require 'childwindow.lua'
require 'goods.lua'
require 'ids.lua'

class shipdealer : shipdealer_Designer with ChildWindow
{
	shipdealer()
	{
		base();
		this.ChildWindowInit();
		LoadShipClassNames();
		local e = this.Elements;
		e.tship_list.OnSelectedIndexChanged(() => {
			PlaySound('ui_item_select');
			e.pship_list.SelectedIndex = -1
			e.start_buy.Visible = true
			this.PreviewShip(this.Ships[e.tship_list.SelectedIndex + 1])
		});
		e.pship_list.OnSelectedIndexChanged(() => {
			PlaySound('ui_item_select');
			e.tship_list.SelectedIndex = -1;
			e.start_buy.Visible = false;
			this.PreviewShip(Game.ShipDealer.PlayerShip());
		});
		e.close.OnClick(() => this.Close());
		e.start_buy.OnClick(() => {
				Game.ShipDealer.StartPurchase(this.Ships[e.tship_list.SelectedIndex + 1], () => this.OnShipPurchaseBegin());
		});
		this.categories = {
			weapons = e.category_weapons,
			ammo = e.category_ammo,
			internal = e.category_internal,
			external = e.category_external,
			commodity = e.category_commodity
		};

		e.category_weapons.OnClick(() => { PlaySound("ui_item_select"); this.change_category("weapons"); })
		e.category_ammo.OnClick(() => { PlaySound("ui_item_select"); this.change_category("ammo"); })
		e.category_internal.OnClick(() => { PlaySound("ui_item_select"); this.change_category("internal"); })
		e.category_external.OnClick(() => { PlaySound("ui_item_select"); this.change_category("external"); })
		e.category_commodity.OnClick(() => { PlaySound("ui_item_select"); this.change_category("commodity"); })

		e.inv_list.OnSelectedIndexChanged(() => {
			this.set_buysell("sell")
			this.set_preview(this.PlayerGoods[e.inv_list.SelectedIndex + 1], "sell")
			PlaySound('ui_item_select')
			e.tr_list.SelectedIndex = -1
			this.setinfocard(this.PlayerGoods[e.inv_list.SelectedIndex + 1])
		});
		e.tr_list.OnSelectedIndexChanged(() => {
			this.set_buysell("buy")
			this.set_preview(this.DealerGoods[e.tr_list.SelectedIndex + 1], "buy")
			PlaySound('ui_item_select')
			e.inv_list.SelectedIndex = -1
			this.setinfocard(this.DealerGoods[e.tr_list.SelectedIndex + 1])
		});
		e.btn_buysell.OnClick(() => {
			if (this.BuyState == "buy") {
				Game.ShipDealer.TransferToPlayer(this.DealerGoods[e.tr_list.SelectedIndex + 1], e.quantitySlider.Value, () => {
					this.change_category(this.category)
				})
			} elseif (this.BuyState == "sell") {
				Game.ShipDealer.SellToDealer(this.PlayerGoods[e.inv_list.SelectedIndex + 1], e.quantitySlider.Value, () => {
					this.change_category(this.category)
				});
			}
		});
		e.buy_ship.OnClick(() => {
            local loading = new asyncload();
            Timer(0.5, () => loading.showall());
			OpenModal(loading);
			Game.ShipDealer.Purchase((result) => {
				loading.Close();
				switch(result) {
					case "fail": PlaySound("ui_select_reject"); break;
					case "success": PlayVoiceLine("NNVoice", "ship_purchase_complete"); break;
					case "successprofit":
						PlayVoiceLine("NNVoice", "ship_purchase_complete");
						PlaySound("ui_receive_money");
						break;
				}
				this.Close();
			});
		});
	}

	OnChildOpen()
	{
		local e = this.Elements
		e.shipitems.Visible = false
		e.shiplist.Visible = true
		e.tship_list.Children.Clear()
		e.pship_list.Children.Clear()
		this.Ships = Game.ShipDealer.SoldShips()
		for (item in this.Ships) {
			local li = good_list_item({
				Icon = item.Icon,
				IdsName = item.IdsName,
				IdsHardpoint = ShipClassNames[item.ShipClass + 1],
				Price = item.Price
			}, "ship", false)
			e.tship_list.Children.Add(li)
		}
		local pship = Game.ShipDealer.PlayerShip()
		if(pship) {
			e.pship_list.Children.Add(good_list_item({
				Icon = pship.Icon,
				IdsName = pship.IdsName,
				IdsHardpoint = ShipClassNames[pship.ShipClass + 1],
				Price = pship.Price
			}, "ship", false))
		}
		e.item_infocard.Infocard = nil
	}

	OnShipPurchaseBegin()
	{
		local e = this.Elements
		e.shiplist.Visible = false
		e.item_infocard.Infocard = nil
		e.shipitems.Visible = true
		this.change_category("weapons")
		this.set_buysell("hidden", 0)
	}

	change_category(category)
	{
		local e = this.Elements
		this.category = category
		e.inv_list.SelectedIndex = -1
		e.tr_list.SelectedIndex = -1
		this.set_buysell("hidden")
		this.construct_inventory()
		this.DealerGoods = Game.ShipDealer.GetDealerGoods(category)
		e.tr_list.Children.Clear()
		for (item in this.DealerGoods) {
			local item = good_list_item(item, "buy", false)
			e.tr_list.Children.Add(item)
		}
		for (cat, button in pairs(this.categories)) {
			button.Selected = (cat == category)
		}
	}

	setinfocard(good)
	{
		local e = this.Elements
		if (good.IdsInfo != 0) {
			e.item_infocard.Infocard = GetInfocard(good.IdsInfo, 1);
		} elseif (good.IdsHardpointDescription != 0) {
			e.item_infocard.SetString(StringFromID(good.IdsHardpointDescription));
		}
	}

	construct_inventory()
	{
		local e = this.Elements
		this.PlayerGoods = Game.ShipDealer.GetPlayerGoods(this.category, false)
		e.inv_list.Children.Clear()
		for (item in this.PlayerGoods) {
			local li = good_list_item(item, "sell", false,() => {
				Game.ShipDealer.ProcessMount(item, (result) => {
					if (result == "mount") { 
						PlaySound('ui_equip_mount01') 
						PlayVoiceLine("NNVoice", "mounted");
					}
					elseif (result == "unmount") PlaySound('ui_equip_mount02');
					this.change_category(this.category)
				});	
			});
			e.inv_list.Children.Add(li)
		}
		local str = StringFromID(STRID_CREDITS) + NumberToStringCS(Game.GetCredits(), "N0")
		e.credits_text.Text = str
		e.ship_price_text.Text = StringFromID(STRID_SHIP_PRICE) + NumberToStringCS(Game.ShipDealer.GetShipDisplayPrice(), "N0")
		if (this.BuyState == "sell" && this.PlayerGoods.length == 0)
			this.set_buysell("hidden");
		e.inv_list.SelectedIndex = e.inv_list.SelectedIndex // Needs to update after changing list
		e.inv_list.ReloadStyle()
	}

	set_preview(item, state)
	{
		local x = this.Elements.item_preview
		local e = this.Elements
		local preview = good_list_item(item, state, true)
		this.setinfocard(item)
		if (item.Price == -1) 
			this.set_buysell("hidden");
		this.PreviewPrice = item.Price
		x.Children.Clear()
		preview.Width = x.Width
		preview.Height = x.Height
		preview.X = 0
		preview.Y = 0
		preview.Enabled = false
		x.Children.Add(preview)
		e.quantitySlider.Min = 1
		local max = item.Count
		e.quantitySlider.Visible = (max > 1)
		e.quantitySlider.Max = max
		e.quantitySlider.Value = e.quantitySlider.Max
		e.quantitySlider.Smooth = false
	}

	// Update the buy/sell button to be buy, sell, or invisible (no selection)
	set_buysell(state, strid)
	{
		local x = this.Elements.btn_buysell
		local container = this.Elements.buysell_controls
		local err = this.Elements.error_text
		local iprev = this.Elements.item_preview
		this.BuyState = state
		local e = this.Elements
		e.buy_ship.Visible = false
		e.credits_needed_text.Visible = false
		if (state == "buy") {
			x.Strid = STRID_BUY
			x.Style = "trader_buy"
			x.ReloadStyle()
			container.Visible = true
			err.Visible = false
			iprev.Visible = true
		} elseif (state == "sell") {
			x.Strid = STRID_SELL
			x.Style = "trader_sell"
			x.ReloadStyle()
			container.Visible = true
			err.Visible = false
			iprev.Visible = true
		} else {
			container.Visible = false
			err.Visible = false
			iprev.Visible = false
			local cneed = Game.ShipDealer.GetRequiredCredits()
			if (cneed > 0) {
				e.credits_needed_text.SetString(string.format(StringFromID(STRID_SHIP_NEEDMONEY), StringFromID(STRID_CREDIT_SIGN) + cneed), "$Normal", 22)
				e.credits_needed_text.Visible = true
			} else {
				e.buy_ship.Visible = true
			}
		}
	}

	PreviewShip(ship)
	{
		local e = this.Elements;
		e.ship_preview_panel.Visible = true
		e.ship_preview.ModelPath = ship.Model
		e.ship_name.Strid = ship.IdsName
		e.ship_class.Text = ShipClassNames[ship.ShipClass + 1]
		if (ship.IdsInfo != 0)
			e.item_infocard.Infocard = GetInfocard(ship.IdsInfo, 1);
		else
			e.item_infocard.Infocard = nil;
	}
}




