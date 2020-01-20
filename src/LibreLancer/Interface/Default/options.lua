-- Entry Animation
GetElement('general'):Animate('flyinleft', 0, 0.6)
GetElement('controls'):Animate('flyinleft', 0.05, 0.6)
GetElement('performance'):Animate('flyinleft', 0.1, 0.6)
GetElement('audio'):Animate('flyinleft', 0.15, 0.6)
GetElement('credits'):Animate('flyinleft', 0.2, 0.6)
GetElement('return'):Animate('flyinleft', 0.25, 0.6)
-- Left Buttons
GetElement('return').Clicked:Add(function()
	SwitchTo('mainmenu')
end)



