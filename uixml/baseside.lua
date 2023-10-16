require 'objects'
require 'childwindowmanager'

local function ModelRenderable(model, tint)
{
	local renderable = NewObject('UiRenderable')
	local modelElem = NewObject('DisplayModel')
	modelElem.Model = model
	if (tint != nil) modelElem.Tint = tint;
	renderable.AddElement(modelElem)
	return renderable
}

local function NavbarButton(hotspot, active)
{
	// State
	local normalTint = active ? GetColor('yellow') : GetColor('text')
	// Construct Appearance
	local model = NewObject('InterfaceModel')
	model.Path = GetNavbarIconPath(hotspot)
	model.X = 0
	model.Y = 0
	model.XScale = 35.0
	model.YScale = 35.0
	local button = NewObject('Button')
	local style = NewObject('ButtonStyle')
	style.Width = 38
	style.Height = 38
	local regAppearance = NewObject('ButtonAppearance')
	regAppearance.Background = ModelRenderable(model, normalTint)
	style.Normal = regAppearance
	local hoverAppearance = NewObject('ButtonAppearance')
	hoverAppearance.Background = active ? regAppearance.Background : ModelRenderable(model, GetColor('text_hover'));
	style.Hover = hoverAppearance
	// Set Appearance
	button.ID = hotspot
	button.SetStyle(style)
	return button
}

local function NavbarAction(hotspot)
{
	local obj = NavbarButton(hotspot, false)
	obj.Width = 33
	obj.Height = 33
	return obj
}

local navbox = require 'navbox.lua'

class baseside : baseside_Designer
{
    baseside()
    {
        base()
        local btns = Game.GetNavbarButtons()
        local actions = Game.GetActionButtons()
        local activeids = Game.ActiveNavbarButton()
        local container = navbox.GetNavbox(this.Widget, btns)
        local locX = navbox.GetStartX(btns)
        local activeIDS = 0

        for (index, button in ipairs(btns)) {
            local obj = NavbarButton(button.IconName, button.IDS == activeids)
            obj.Anchor = AnchorKind.TopCenter
            obj.X = locX
            locX += navbox.XSpacing
            obj.Y = navbox.OffsetY
            if (button.IDS != activeids) {
                obj.OnClick(() => Game.HotspotPressed(button.IDS));
            } else {
                activeIDS = index
            }
            container.AddChild(obj)
        }

	    local has_news = false
	    local news_button = {}

	    local has_commodity = false
	    local has_equipment = false
	    local commodity_button = {}

	    local has_shipdealer = false
	    local shipdealer_button = {}
	
        local actionbox = navbox.GetActionBox(this.Widget, container, btns, actions, activeIDS)
        for (index, action in ipairs(actions)) {
            local obj = NavbarAction(action.IconName)
            switch(action.IconName) {
                case "IDS_HOTSPOT_NEWSVENDOR":
                    has_news = true;
                    news_button = obj;
                    break;
                case "IDS_HOTSPOT_COMMODITYTRADER":
                    has_commodity = true;
                    commodity_button = obj;
                    break;
                case "IDS_HOTSPOT_EQUIPMENTDEALER":
                    has_equipment = true;
                    commodity_button = obj;
                    break;
                case "IDS_HOTSPOT_SHIPDEALER":
                    has_shipdealer = true;
                    shipdealer_button = obj;
                    break;
                default:
                    obj.OnClick(() => Game.HotspotPressed(action.IDS));
                    break;
            }
            navbox.PositionAction(obj, actionbox, index)
        }

		if(Game.IsMultiplayer()) {
			this.Elements.nn_chat.Visible = true;
			this.Elements.nn_request.Visible = true;
			this.Elements.nnbox5.Visible = false;
			this.Elements.nnbox7.Visible = true;
		} else {
			this.Elements.nn_chat.Visible = false;
			this.Elements.nn_request.Visible = false;
			this.Elements.nnbox5.Visible = true;
			this.Elements.nnbox7.Visible = false;
		}
    
        this.Elements.chatbox.OnTextEntered((category, text) => Game.ChatEntered(category, text));
	    this.InfoWindow = new infowindow()
	    this.Map = new mapwindow()
		this.PlayerStatus = new playerstatus()
	    this.Map.InitMap()
	    this.CommodityTrader = new commodity()
		this.ChatHistory = new chathistory()
	    local windows = {
		    { this.Elements.nn_map, this.Map },
		    { this.Elements.nn_info, this.InfoWindow },
			{ this.Elements.nn_playerstatus, this.PlayerStatus },
			{ this.Elements.nn_chat, this.ChatHistory }
	    }
	    if (has_news) {
		    this.News = new news();
		    table.insert(windows, { news_button, this.News })
	    }
	    if (has_commodity) {
		    this.CommodityTrader = new commodity("commodity")
		    table.insert(windows, { commodity_button, this.CommodityTrader })
	    } elseif (has_equipment) {
		    this.CommodityTrader = new commodity("equipment")
		    table.insert(windows, { commodity_button, this.CommodityTrader })
	    } elseif (has_shipdealer) {
		    this.ShipDealer = new shipdealer()
		    table.insert(windows, { shipdealer_button, this.ShipDealer })
	    }
	    this.WindowManager = new childwindowmanager(this.Widget, windows)
	    this.Elements.chat.Chat = Game.GetChats()
		this.Elements.nnobj.Visible = false;
    }

	ObjectiveUpdate(nnids)
	{
		if(nnids > 0) {
			PlaySound("ui_new_story_star");
			local e = this.Elements
			e.nnobj.FadeIn(1.0);
			e.nnobj.Strid = nnids;
			Timer(4, () => e.nnobj.FadeOut(1.0));
		} else {
			e.nnobj.FadeOut(1.0);
		}
	}
    
    Pause() => OpenModal(new pausemenu());
    
    Chatbox() => this.Elements.chatbox.Visible = true;
    
    Popup(title,contents,id) => OpenModal(new popup(title, contents, 'ok', () => Game.PopupFinish(id)));
    
    MissionOffer(mission) => OpenModal(new popup(STRID_MISSION, mission, 'accept', (result) => Game.MissionResponse(result)));
}





