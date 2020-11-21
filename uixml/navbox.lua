local Navbox = { 
	OffsetY = 10,
	XSpacing = 40.6
}


function Navbox.GetStartX(btns)
	local startX = { 0, -20.4, -40.4, -60.4, -80.4 }
	return startX[#btns]
end

function Navbox.PositionAction(button, actionbox, index)
	button.Anchor = AnchorKind.CenterLeft
	button.X = (index - 1) * 23
	actionbox:AddChild(button)
end

function Navbox.GetNavbox(w, btns)
	local containers = { 'navbox1', 'navbox2', 'navbox3', 'navbox4', 'navbox5' }
	for _, c in ipairs(containers) do w:GetElement(c).Visible = false end
	local ctrl = w:GetElement(containers[#btns])
	ctrl.Visible = true
	return ctrl
end

local function clamp(x, min, max)
	if x < min then return min end
	if x > max then return max end
	return x
end

function Navbox.GetActionBox(w, navcontainer, btns, actions, index)
	local actionbox = { }
	local boxes = { 'actionbox1', 'actionbox2', 'actionbox3' }
	for _, c in ipairs(boxes) do w:GetElement(c).Visible = false end
	if #actions > 0 then
		actionbox = w:GetElement(boxes[#actions])
		actionbox.Visible = true
		local minX = -(navcontainer.Width / 2) + actionbox.Width / 2 + 26
		local maxX = (navcontainer.Width / 2) - actionbox.Width / 2 - 26
		local newX = Navbox.GetStartX(btns) + ((index - 1) * Navbox.XSpacing)
		actionbox.X = clamp(newX, minX, maxX)
	end
	return actionbox
end

return Navbox
