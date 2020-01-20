-- Entry Animation
GetElement('newgame'):Animate('flyinleft', 0, 0.6)
GetElement('loadgame'):Animate('flyinleft', 0.05, 0.6)
GetElement('multiplayer'):Animate('flyinleft', 0.1, 0.6)
GetElement('options'):Animate('flyinleft', 0.15, 0.6)
GetElement('exit'):Animate('flyinleft', 0.2, 0.6)
-- Exit Animation
function ExitAnimation(f)
	GetElement('exit'):Animate('flyoutleft', 0, 0.6)
	GetElement('options'):Animate('flyoutleft', 0.05, 0.6)
	GetElement('multiplayer'):Animate('flyoutleft', 0.1, 0.6)
	GetElement('loadgame'):Animate('flyoutleft', 0.15, 0.6)
	GetElement('newgame'):Animate('flyoutleft', 0.2, 0.6)
	Timer(0.8, f)
end
-- Event Handling
GetElement('newgame').Clicked:Add(function ()
	ExitAnimation(function ()
		Game:NewGame()
	end)
end)
GetElement('loadgame').Clicked:Add(function ()

end)
GetElement('multiplayer').Clicked:Add(function ()
	ExitAnimation(function ()
		SwitchTo('multiplayer')
	end)
end)
GetElement('options').Clicked:Add(function ()
	ExitAnimation(function ()
		SwitchTo('options')
	end)
end)
GetElement('exit').Clicked:Add(function ()
	ExitAnimation(function ()
		Game:Exit()
	end)
end)
















