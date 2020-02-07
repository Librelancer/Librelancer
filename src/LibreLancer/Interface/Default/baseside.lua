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

function NavbarButton(hotspot, active)
	-- State
	local normalTint = Color('white')
	if active == true then
		normalTint = Color('yellow')
	end
	-- Construct Appearance
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
	if active == true then
		hoverAppearance.Background = regAppearance.Background
	else
		hoverAppearance.Background = ModelRenderable(model, Color('white_hover'))
	end
	style.Hover = hoverAppearance
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

navbox = require 'navbox.lua'

local btns = Game:GetNavbarButtons()
local actions = Game:GetActionButtons()
local activeids = Game:ActiveNavbarButton()
local container = navbox.GetNavbox(btns)
local locX = navbox.GetStartX(btns)
local activeIDS = 0

for index, button in ipairs(btns) do
	local obj = NavbarButton(button.IconName, button.IDS == activeids)
	obj.Anchor = AnchorKind('TopCenter')
	obj.X = locX
	locX = locX + navbox.XSpacing
	obj.Y = navbox.OffsetY
	if button.IDS ~= activeids then
		obj.Clicked:Add(function()
			Game:HotspotPressed(button.IDS)
		end)
	else
		activeIDS = index
	end
	container.Children:Add(obj)
end

local actionbox = navbox.GetActionBox(container, btns, actions, activeIDS)
for index, action in ipairs(actions) do
	local obj = NavbarAction(action.IconName)
	obj.Clicked:Add(function()
		Game:HotspotPressed(action.IDS)
	end)
	navbox.PositionAction(obj, actionbox, index)
end
GetElement('chatbox').TextEntered:Add(function (text)
    Game:TextEntered(text)
end)
function Events.Chatbox()
	GetElement('chatbox').Visible = true
end



