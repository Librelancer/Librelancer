class scancargo : scancargo_Designer with ChildWindow
{
	scancargo()
    {
        base();
        this.ChildWindowInit();
        this.Elements.close.OnClick(() => this.Close());
		this.Mode = "scan";
                
        local e = this.Elements;
		this.categories = {
			weapons = e.category_weapons,
			ammo = e.category_ammo,
			internal = e.category_internal,
			external = e.category_external,
			commodity = e.category_commodity
		};
		
		this.change_category("weapons");
		
        e.category_weapons.OnClick(() => this.change_category("weapons"))
		e.category_ammo.OnClick(() => this.change_category("ammo"))
		e.category_internal.OnClick(() => this.change_category("internal"))
		e.category_external.OnClick(() => this.change_category("external"))
		e.category_commodity.OnClick(() => this.change_category("commodity"))
		
		e.inv_list.OnSelectedIndexChanged(() => {
			local good = this.selected_good();
			if(good != nil) {
				this.set_item_infocard(good);
			} else {
				this.set_ship_infocard();
			}
			this.update_jettison(good);
		});
	
		e.btn_jettison.OnClick(() => this.show_jettison());

		e.btn_jettison_cancel.OnClick(() => this.hide_jettison());
		e.btn_jettison_confirm.OnClick(() => {
			local good = this.selected_good();
			if(good != nil && good.CanJettison == true) {
				Game.JettisonInventoryItem(good, this.jettison_count());
				this.hide_jettison();
			}
		});
		e.jettison_panel.OnUpdate(() => this.update_jettison_quantity());
	
		Game.OnUpdatePlayerInventory(() => {
			if(this.Opened && this.Mode == "player") {
				this.construct_inventory();
			}
		});
    }

	OpenForPlayer()
	{
		this.Mode = "player";
	}

	OpenForScan()
	{
		this.Mode = "scan";
	}
    
    OnChildOpen()
    {
    	this.change_category("weapons");
    }
    
	change_category(category)
	{
		local e = this.Elements
		this.category = category
		e.inv_list.SelectedIndex = 0 - 1
		this.hide_jettison()
		this.construct_inventory()
		this.set_ship_infocard()
		for (cat, button in pairs(this.categories)) {
			button.Selected = (cat == category)
		}
	}

	selected_good()
	{
		local index = this.Elements.inv_list.SelectedIndex;
		if (this.PlayerGoods == nil || index == nil || index < 0 || index >= this.PlayerGoods.length)
			return nil;
		return this.PlayerGoods[index + 1];
	}
	
	construct_ship_infocard()
	{
		local e = this.Elements;
		if(this.Mode == "player") {
			e.title.Strid = 0;
			e.title.Text = "Cargo";
			e.ship_infocard_title.Strid = 0;
			e.ship_infocard_title.Text = "Selected Item";
			e.ship_infocard.Infocard = nil;
		} else {
			e.title.Text = nil;
			e.title.Strid = 3019;
			e.ship_infocard_title.Text = nil;
			e.ship_infocard_title.Strid = 903;
			e.ship_infocard.Infocard = Game.GetScannedShipInfocard();
		}
	}
    
    construct_inventory()
	{
		local e = this.Elements;
		if(this.Mode == "player")
			this.PlayerGoods = Game.GetPlayerInventory(this.category);
		else
			this.PlayerGoods = Game.GetScannedInventory(this.category);
		e.inv_list.Children.Clear();
		for (item in this.PlayerGoods) {
			local li = good_list_item(item, "inventory", false, nil);
			e.inv_list.Children.Add(li);
		}
		e.inv_list.SelectedIndex = e.inv_list.SelectedIndex; //refresh after list change
		this.construct_ship_infocard();
		local good = this.selected_good();
		if(good != nil) {
			this.set_item_infocard(good);
			this.update_jettison(good);
		} else {
			this.update_jettison(nil);
		}
	}

	update_jettison(good)
	{
		local canJettison = this.Mode == "player" && good != nil && good.CanJettison == true;
		this.Elements.btn_jettison.Visible = canJettison;
		if (!canJettison)
			this.hide_jettison();
	}

	show_jettison()
	{
		local good = this.selected_good();
		if (this.Mode != "player" || good == nil || good.CanJettison != true)
			return;
		local e = this.Elements;
		local count = good.Count == nil ? 1 : good.Count;
		if (count < 1)
			count = 1;
		e.ship_infocard_panel.Visible = false;
		e.item_infocard_panel.Visible = false;
		e.jettison_panel.Visible = true;
		e.jettison_quantity.Min = 1;
		e.jettison_quantity.Max = count > 1 ? count : 2;
		e.jettison_quantity.Value = count;
		e.jettison_quantity.Visible = count > 1;
		e.jettison_quantity.Smooth = false;
		this.update_jettison_quantity();
	}

	hide_jettison()
	{
		this.Elements.jettison_panel.Visible = false;
	}

	jettison_count()
	{
		local value = this.Elements.jettison_quantity.Value;
		if (value == nil)
			return 1;
		return math.floor(value);
	}

	update_jettison_quantity()
	{
		if (!this.Elements.jettison_panel.Visible)
			return;
		this.Elements.jettison_quantity_label.Text = tostring(this.jettison_count());
	}
	
	set_item_infocard(good)
	{
		local e = this.Elements
		local idsInfo = good.IdsInfo == nil ? 0 : good.IdsInfo;
		if (idsInfo != 0) {
			e.jettison_panel.Visible = false;
			e.ship_infocard_panel.Visible = false;
			e.item_infocard_panel.Visible = true;
			e.item_infocard.Infocard = GetInfocard(idsInfo, 1);
		} else {
			this.set_ship_infocard();
		}
	}
	
	set_ship_infocard()
	{
		local e = this.Elements;
		e.jettison_panel.Visible = false;
		e.item_infocard_panel.Visible = false;
		e.ship_infocard_panel.Visible = true;
	}
	
	Closing()
	{
		if(this.Mode == "scan") {
			Game.StopScan();
		}
	}
}
