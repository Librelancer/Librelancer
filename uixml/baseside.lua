require 'objects'

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

local function NavbarButton(hotspot, active)
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
