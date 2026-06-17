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
			external = e.category_external
		};
		
		this.change_category("weapons");
		
        e.category_weapons.OnClick(() => this.change_category("weapons"))
		e.category_ammo.OnClick(() => this.change_category("ammo"))
		e.category_internal.OnClick(() => this.change_category("internal"))
		e.category_external.OnClick(() => this.change_category("external"))
		e.category_commodity.OnClick(() => this.change_category("commodity"))
		
		e.inv_list.OnSelectedIndexChanged(() => {
			local good = e.inv_list.SelectedIndex == -1 ? nil : this.PlayerGoods[e.inv_list.SelectedIndex + 1];
			if(good != nil) {
				this.set_item_infocard(good);
			} else {
				this.set_ship_infocard();
			}
			this.update_jettison(good);
		});

		e.btn_jettison.OnClick(() => {
			local good = e.inv_list.SelectedIndex == -1 ? nil : this.PlayerGoods[e.inv_list.SelectedIndex + 1];
			if(good != nil && good.CanJettison) {
				Game.JettisonInventoryItem(good);
			}
		});

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
		e.inv_list.SelectedIndex = -1
		this.construct_inventory()
		this.set_ship_infocard()
		for (cat, button in pairs(this.categories)) {
			button.Selected = (cat == category)
		}
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
		if(e.inv_list.SelectedIndex != -1)
		{
			local good = this.PlayerGoods[e.inv_list.SelectedIndex + 1];
			this.set_item_infocard(good);
			this.update_jettison(good);
		}
		else
		{
			this.update_jettison(nil);
		}
	}

	update_jettison(good)
	{
		this.Elements.btn_jettison.Visible = this.Mode == "player" && good != nil && good.CanJettison;
	}
	
	set_item_infocard(good)
	{
		local e = this.Elements
		if (good.IdsInfo != 0) {
			e.ship_infocard_panel.Visible = false;
			e.item_infocard_panel.Visible = true;
			e.item_infocard.Infocard = GetInfocard(good.IdsInfo, 1);
		} else {
			this.set_ship_infocard();
		}
	}
	
	set_ship_infocard()
	{
		local e = this.Elements;
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
