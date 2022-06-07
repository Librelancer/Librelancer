local Navbox = { 
	OffsetY = 10,
	XSpacing = 40.6
}


function Navbox.GetStartX(btns) 
{
	local startX = { 0, -20.4, -40.4, -60.4, -80.4 }
	return startX[btns.length]
}

function Navbox.PositionAction(button, actionbox, index)
{
	button.Anchor = AnchorKind.CenterLeft
	button.X = (index - 1) * 23
	actionbox.AddChild(button)
}

function Navbox.GetNavbox(w, btns)
{
	local containers = { 'navbox1', 'navbox2', 'navbox3', 'navbox4', 'navbox5' }
	for (c in containers) w.GetElement(c).Visible = false;
	local ctrl = w.GetElement(containers[btns.length])
	ctrl.Visible = true
	return ctrl
}

local function clamp(x, min, max)
{
	if (x < min) return min;
	if (x > max) return max;
	return x
}

function Navbox.GetActionBox(w, navcontainer, btns, actions, index)
{
	local actionbox = { }
	local boxes = { 'actionbox1', 'actionbox2', 'actionbox3' }
	for (c in boxes) w.GetElement(c).Visible = false;
	if (actions.length > 0) 
	{
		actionbox = w.GetElement(boxes[actions.length])
		actionbox.Visible = true
		local minX = -(navcontainer.Width / 2) + actionbox.Width / 2 + 26
		local maxX = (navcontainer.Width / 2) - actionbox.Width / 2 - 26
		local newX = Navbox.GetStartX(btns) + ((index - 1) * Navbox.XSpacing)
		actionbox.X = clamp(newX, minX, maxX)
	}
	return actionbox
}

return Navbox
