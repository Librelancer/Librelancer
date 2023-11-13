require 'childwindow.lua'
require 'goods.lua'

local function get_max_amount(item)
{
    return Game.Trader.GetPurchaseLimit(item)
}

class commodity : commodity_Designer with ChildWindow
{
	commodity(kind)
	{
		base();
		this.ChildWindowInit();
		Game.Trader.OnUpdateInventory(() => this.construct_inventory());
		local e = this.Elements;
		this.categories = {
			weapons = e.category_weapons,
			ammo = e.category_ammo,
			internal = e.category_internal,
			external = e.category_external
		};

		if (kind == "commodity") {
			this.change_category("commodity")
			e.inv_categories.Visible = false
		} else {
			this.change_category("weapons")
		}

		e.category_weapons.OnClick(() => { PlaySound("ui_item_select"); this.change_category("weapons"); })
		e.category_ammo.OnClick(() => { PlaySound("ui_item_select"); this.change_category("ammo"); })
		e.category_internal.OnClick(() => { PlaySound("ui_item_select"); this.change_category("internal"); })
		e.category_external.OnClick(() => { PlaySound("ui_item_select"); this.change_category("external"); })

		e.inv_list.OnSelectedIndexChanged(() => {
			this.set_buysell("sell")
			this.set_preview(this.PlayerGoods[e.inv_list.SelectedIndex + 1], "sell")
			PlaySound('ui_item_select')
			e.tr_list.SelectedIndex = -1
			this.setinfocard(this.PlayerGoods[e.inv_list.SelectedIndex + 1])
		});
		e.tr_list.OnSelectedIndexChanged(() => {
			this.set_buysell("buy")
			this.set_preview(this.TraderGoods[e.tr_list.SelectedIndex + 1], "buy")
			PlaySound('ui_item_select')
			e.inv_list.SelectedIndex = -1
			this.setinfocard(this.TraderGoods[e.tr_list.SelectedIndex + 1])
		});

		e.close.OnClick(() => this.Close());

		e.btn_buysell.OnClick(() => {
			if (this.BuyState == "buy") {
				Game.Trader.Buy(this.TraderGoods[e.tr_list.SelectedIndex + 1].Good, e.quantitySlider.Value, () => {
					this.set_preview(this.TraderGoods[e.tr_list.SelectedIndex + 1], "buy")
					PlaySound('ui_buy_commodity')
				});
			} elseif (this.BuyState == "sell") {
				local doUpdate = e.quantitySlider.Value < this.PlayerGoods[e.inv_list.SelectedIndex + 1].Count;
				Game.Trader.Sell(this.PlayerGoods[e.inv_list.SelectedIndex + 1], e.quantitySlider.Value, () => {
					if (doUpdate) 
						this.set_preview(this.PlayerGoods[e.inv_list.SelectedIndex + 1], "sell");
					else
						this.set_buysell("hidden");
					PlaySound('ui_receive_money')
				});
			}
		});

		this.set_buysell("hidden")
		e.item_preview.OnUpdate(() => this.PreviewUpdate(delta));
		
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

	// Update the buy/sell button to be buy, sell, or invisible (no selection)
	set_buysell(state, strid)
	{
		local x = this.Elements.btn_buysell
		local container = this.Elements.buysell_controls
		local err = this.Elements.error_text
		local iprev = this.Elements.item_preview
		this.BuyState = state

		switch(state) {
			case "buy":
				x.Strid = STRID_BUY;
				x.Style = "trader_buy";
				x.ReloadStyle();
				container.Visible = true;
				err.Visible = false;
				iprev.Visible = true;
				break;
			case "sell":
				x.Strid = STRID_SELL;
				x.Style = "trader_sell";
				x.ReloadStyle();
				container.Visible = true;
				err.Visible = false;
				iprev.Visible = true;
				break;
			case "error":
				container.Visible = false;
				err.SetString(StringFromID(strid), "$Normal", 26)
				iprev.Visible = true
				err.Visible = true
				break;
			default:
				container.Visible = false;
				err.Visible = false;
				iprev.Visible = false;
				break;
		}
	}

	construct_inventory()
	{
		local e = this.Elements;
		this.PlayerGoods = Game.Trader.GetPlayerGoods(this.category, false);
		e.inv_list.Children.Clear();
		for (item in this.PlayerGoods) {
			local li = good_list_item(item, "sell", false, () => {
				Game.Trader.ProcessMount(item, (result) => {
					if (result == "mount") {
						PlaySound('ui_equip_mount01');
						PlayVoiceLine("NNVoice", "mounted");
					}
					if(result == "unmount") PlaySound("ui_equip_mount02");
				})
			});
			e.inv_list.Children.Add(li);
		}
		local str = StringFromID(STRID_CREDITS) + NumberToStringCS(Game.GetCredits(), "N0");
		e.credits_text.Text = str
		if (this.BuyState == "sell" && this.PlayerGoods.length == 0) {
			this.set_buysell("hidden");
		}
		e.inv_list.SelectedIndex = e.inv_list.SelectedIndex; //refresh after list change
		e.inv_list.ReloadStyle();
	}

	change_category(category)
	{
		local e = this.Elements
		this.category = category
		e.inv_list.SelectedIndex = -1
		e.tr_list.SelectedIndex = -1
		this.set_buysell("hidden")
		this.construct_inventory()
		this.TraderGoods = Game.Trader.GetTraderGoods(category)
		e.tr_list.Children.Clear()
		for (item in this.TraderGoods) {
			local item = good_list_item(item, "buy", false)
			e.tr_list.Children.Add(item)
		}
		for (cat, button in pairs(this.categories)) {
			button.Selected = (cat == category)
		}
	}

	PreviewUpdate(delta)
	{
		local e = this.Elements;
		e.quantity_label.Text = tostring(e.quantitySlider.Value);
		local prefix = this.BuyState == "sell" ? StringFromID(STRID_ADD_CREDITS) : StringFromID(STRID_NEG_CREDITS);
		e.price_total.Text = prefix + NumberToStringCS(math.floor(e.quantitySlider.Value) * this.PreviewPrice, "N0")
	}

	set_preview(item,state)
	{
	    local maxAmount = get_max_amount(item);

		if(state == "buy" && !Game.HasShip())
			this.set_buysell("error", STRID_NO_SHIP);
		elseif (state == "buy" && item.Price > Game.GetCredits())
			this.set_buysell("error", STRID_INSUFFICIENT_CREDITS);
		elseif (state == "buy" && maxAmount == 0)
			this.set_buysell("error", STRID_INSUFFICIENT_SPACE);
		elseif (item.Price == 0)
			this.set_buysell("hidden");

		local x = this.Elements.item_preview;
		local e = this.Elements;
		local preview = good_list_item(item, state, true)
		this.PreviewPrice = item.Price
		x.Children.Clear()
		preview.Width = x.Width
		preview.Height = x.Height
		preview.X = 0
		preview.Y = 0
		preview.Enabled = false
		x.Children.Add(preview)
		e.quantitySlider.Min = 1
		if(state == "sell") maxAmount = item.Count;
		else max = maxAmount;
		if(!item.Combinable) maxAmount = 1;
		e.quantitySlider.Visible = (maxAmount > 1)
		e.quantitySlider.Max = maxAmount
		e.quantitySlider.Value = e.quantitySlider.Max
		e.quantitySlider.Smooth = false
		e.unit_price.Text = StringFromID(STRID_CREDIT_SIGN) + NumberToStringCS(item.Price, "N0")
	}
}



