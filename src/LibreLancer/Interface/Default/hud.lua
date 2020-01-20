ManeuverButtons = {}

function ModelRenderable(model, tint)
	local renderable = NewObject('UiRenderable')
	local modelElem = NewObject('DisplayModel')
	modelElem.Model = model
	if tint ~= nil then
		modelElem.Tint = tint
	end
	renderable.Elements:Add(modelElem)
	
	return renderable
end

function HudButton(modelPath, disabledPath)
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
	hoverAppearance.Background = ModelRenderable(model, Color('white_hover'))
	style.Hover = hoverAppearance
	local selectedAppearance = NewObject('ButtonAppearance')
	selectedAppearance.Background = ModelRenderable(disabledModel, Color('yellow'))
	style.Selected = selectedAppearance
	local disabledAppearance = NewObject('ButtonAppearance')
	disabledAppearance.Background = ModelRenderable(disabledModel)
	style.Disabled = disabledAppearance
	-- Set Appearance
	button:SetStyle(style)
	return button
end

function NavbarAction(hotspot)
	local obj = NavbarButton(hotspot, false)
	obj.Width = 33
	obj.Height = 33
	return obj
end

function UpdateManeuverState()
	local activeManeuver = Game:GetActiveManeuver()
	local maneuversEnabled = Game:GetManeuversEnabled()
	for action, button in pairs(ManeuverButtons) do
		button.Selected = (activeManeuver == action)
		button.Enabled = maneuversEnabled[action]
	end
end

navbox = require 'navbox.lua'

local btns = Game:GetManeuvers()
local container = navbox.GetNavbox(btns)
local locX = navbox.GetStartX(btns) - 15
local activeIDS = 0


for index, button in ipairs(btns) do
	local obj = HudButton(button.ActiveModel, button.InactiveModel)
	ManeuverButtons[button.Action] = obj
	obj.Anchor = AnchorKind('TopCenter')
	obj.X = locX
	locX = locX + navbox.XSpacing + 10
	obj.Y = navbox.OffsetY
	if button.Action ~= activeids then
		obj.Clicked:Add(function()
			Game:HotspotPressed(button.Action)
		end)
	else
		activeIDS = index
	end
	container.Children:Add(obj)
end
UpdateManeuverState()







