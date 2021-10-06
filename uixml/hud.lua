require 'ids.lua'

local function ModelRenderable(model, tint)
	local renderable = NewObject('UiRenderable')
	local modelElem = NewObject('DisplayModel')
	modelElem.Model = model
	if tint ~= nil then
		modelElem.Tint = tint
	end
	renderable:AddElement(modelElem)
	
	return renderable
end

local function HudButton(modelPath, disabledPath)
	-- Construct Appearance
	local model = NewObject('InterfaceModel')
	model.Path = modelPath
	model.X = 0
	model.Y = 0
	model.XScale = 70.0
	model.YScale = 77.0
	local disabledModel = NewObject('InterfaceModel')
	disabledModel.Path = disabledPath
	disabledModel.X = 0
	disabledModel.Y = 0
	disabledModel.XScale = 70.0
	disabledModel.YScale = 77.0
	local button = NewObject('Button')
	local style = NewObject('ButtonStyle')
	style.Width = 38
	style.Height = 38
	local regAppearance = NewObject('ButtonAppearance')
	regAppearance.Background = ModelRenderable(model)
	style.Normal = regAppearance
	local hoverAppearance = NewObject('ButtonAppearance')
	hoverAppearance.Background = ModelRenderable(model, GetColor('white_hover'))
	style.Hover = hoverAppearance
	local selectedAppearance = NewObject('ButtonAppearance')
	selectedAppearance.Background = ModelRenderable(disabledModel, GetColor('yellow'))
	style.Selected = selectedAppearance
	local disabledAppearance = NewObject('ButtonAppearance')
	disabledAppearance.Background = ModelRenderable(disabledModel)
	style.Disabled = disabledAppearance
	-- Set Appearance
	button:SetStyle(style)
	return button
end

local function NavbarAction(hotspot)
	local obj = NavbarButton(hotspot, false)
	obj.Width = 33
	obj.Height = 33
	return obj
end

function hud:UpdateManeuverState()
	local activeManeuver = Game:GetActiveManeuver()
	local maneuversEnabled = Game:GetManeuversEnabled()
	for action, button in pairs(self.ManeuverButtons) do
		button.Selected = (activeManeuver == action)
		button.Enabled = maneuversEnabled:Get(action)
	end
end

local navbox = require 'navbox'

function hud:ctor()
    self.ManeuverButtons = {}
    local btns = Game:GetManeuvers()
    local container = navbox.GetNavbox(self.Widget, btns)
    local locX = navbox.GetStartX(btns) - 15
    local activeIDS = 0
    for index, button in ipairs(btns) do
        local obj = HudButton(button.ActiveModel, button.InactiveModel)
        self.ManeuverButtons[button.Action] = obj
        obj.Anchor = AnchorKind.TopCenter
        obj.X = locX
        locX = locX + navbox.XSpacing + 10
        obj.Y = navbox.OffsetY
        if button.Action ~= activeids then
            obj:OnClick(function()
                Game:HotspotPressed(button.Action)
            end)
        else
            activeIDS = index
        end
        container:AddChild(obj)
    end
    self:UpdateManeuverState()
    
    self.Elements.chatbox.OnTextEntered(function (text)
                                            Game:TextEntered(text)
                                        end)
	self.Elements.chat.Chat = Game:GetChats()
end

function hud:Update()
    self:UpdateManeuverState()
	local e = self.Elements
    e.speedText.Text = Game:Speed() .. ""
    e.thrustText.Text = Game:ThrustPercent() .. "%"
	e.hullgauge.PercentFilled = Game:GetPlayerHealth()
	e.powergauge.PercentFilled = Game:GetPlayerPower()
	e.shieldgauge.PercentFilled = Game:GetPlayerShield()
	local cruise = Game:CruiseCharge()
	if cruise >= 0 then
		e.cruisecharge.Text = StringFromID(STRID_CRUISE_CHARGING) .. " - " .. cruise .. "%"
		e.cruisecharge.Visible = true
	else
		e.cruisecharge.Visible = false
	end
	if Game:SelectionVisible() then
		local pos = Game:SelectionPosition()
		e.selection.Visible = true
		e.selection.X = pos.X - (e.selection.Width / 2.0)
		e.selection.Y = pos.Y - (e.selection.Height / 2.0)
		e.selection_name.Text = Game:SelectionName()
		local health = Game:SelectionHealth()
		local shield = Game:SelectionShield()
		if health >= 0 then
			e.selection_health.Visible = true
			e.selection_health.PercentFilled = health
		else
			e.selection_health.Visible = false
		end
		if shield >= 0 then
			e.selection_shield.Visible = true
			e.selection_shield.PercentFilled = shield
		else
			e.selection_shield.Visible = false
		end
	else
		e.selection.Visible = false
	end
end

function hud:Pause()
	OpenModal(pausemenu())
end

function hud:Chatbox()
   self.Elements.chatbox.Visible = true 
end

function hud:Popup(title, contents, id)
	OpenModal(popup(title,contents, function()
		Game:PopupFinish(id)
	end))
end









