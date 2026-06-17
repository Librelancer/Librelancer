class playercargo : playercargo_Designer with ChildWindow
{
	playercargo()
    {
        base();
        this.ChildWindowInit();
        this.Elements.close.OnClick(() => this.Close());

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
			local selected = e.inv_list.SelectedIndex == -1 ? nil : this.PlayerGoods[e.inv_list.SelectedIndex + 1];
			this.update_jettison(selected);
			if(selected != nil) {
				this.set_item_infocard(selected);
			} else {
				this.set_ship_infocard();
			}
		});

		e.btn_jettison.OnClick(() => {
			local selected = e.inv_list.SelectedIndex == -1 ? nil : this.PlayerGoods[e.inv_list.SelectedIndex + 1];
			if(selected != nil && selected.CanJettison) {
				Game.JettisonInventoryItem(selected);
				e.btn_jettison.Visible = false;
			}
		});

		Game.OnUpdatePlayerInventory(() => {
			if(this.Opened) {
				this.construct_inventory();
			}
		});
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
		e.ship_infocard.Infocard = nil;
	}

    construct_inventory()
	{
		local e = this.Elements;
		this.PlayerGoods = Game.GetPlayerInventory(this.category);
		e.inv_list.Children.Clear();
		for (item in this.PlayerGoods) {
			local li = good_list_item(item, "inventory", false, nil);
			e.inv_list.Children.Add(li);
		}
		e.inv_list.SelectedIndex = e.inv_list.SelectedIndex;
		this.construct_ship_infocard();
		if(e.inv_list.SelectedIndex != -1)
		{
			local selected = this.PlayerGoods[e.inv_list.SelectedIndex + 1];
			this.set_item_infocard(selected);
			this.update_jettison(selected);
		} else {
			this.update_jettison(nil);
		}
	}

	update_jettison(good)
	{
		this.Elements.btn_jettison.Visible = good != nil && good.CanJettison;
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
}
