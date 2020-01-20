local Navbox = { 
	OffsetY = 10,
	XSpacing = 40.6
}


function Navbox.GetStartX(btns)
	local startX = { 0, -20.4, -40.4, -60.4, -80.4 }
	return startX[btns.Length]
end

function Navbox.PositionAction(button, actionbox, index)
	button.Anchor = AnchorKind('CenterLeft')
	button.X = index * 23
	actionbox.Children:Add(button)
end

function Navbox.GetNavbox(btns)
	local containers = { 'navbox1', 'navbox2', 'navbox3', 'navbox4', 'navbox5' }
	for _, c in ipairs(containers) do GetElement(c).Visible = false end
	local ctrl = GetElement(containers[btns.Length])
	ctrl.Visible = true
	return ctrl
end

local function clamp(x, min, max)
	if x < min then return min end
	if x > max then return max end
	return x
end

function Navbox.GetActionBox(navcontainer, btns, actions, index)
	local actionbox = { }
	local boxes = { 'actionbox1', 'actionbox2', 'actionbox3' }
	for _, c in ipairs(boxes) do GetElement(c).Visible = false end
	if actions.Length > 0 then
		actionbox = GetElement(boxes[actions.Length])
		actionbox.Visible = true
		local minX = -(navcontainer.Width / 2) + actionbox.Width / 2 + 26
		local maxX = (navcontainer.Width / 2) - actionbox.Width / 2 - 26
		local newX = Navbox.GetStartX(btns) + (index * Navbox.XSpacing)
		actionbox.X = clamp(newX, minX, maxX)
	end
	return actionbox
end

return Navbox
