function mainmenu:ctor()
	local scn = self.Elements;
	scn.newgame:Animate('flyinleft', 0, 0.6)
	scn.loadgame:Animate('flyinleft', 0.05, 0.6)
	scn.multiplayer:Animate('flyinleft', 0.1, 0.6)
	scn.options:Animate('flyinleft', 0.15, 0.6)
	scn.exit:Animate('flyinleft', 0.2, 0.6)
	
	scn.newgame:OnClick(function()
		self:ExitAnimation(function() 
			Game:NewGame()
		end)
	end)
	scn.loadgame:OnClick(function()
		self:ExitAnimation(function() 
			OpenScene("loadgame")
		end)
	end)
	scn.multiplayer:OnClick(function()
		self:ExitAnimation(function()
			OpenScene("serverlist")
		end)
	end)
	scn.options:OnClick(function()
		self:ExitAnimation(function()
            OpenScene("options")
        end)
	end)
	scn.exit:OnClick(function()
		self:ExitAnimation(function()
			Game:Exit()
		end)
	end)
end

function mainmenu:ExitAnimation(f)
	local e = self.Elements;
	e.exit:Animate('flyoutleft', 0, 0.6)
	e.options:Animate('flyoutleft', 0.05, 0.6)
	e.multiplayer:Animate('flyoutleft', 0.1, 0.6)
	e.loadgame:Animate('flyoutleft', 0.15, 0.6)
	e.newgame:Animate('flyoutleft', 0.2, 0.6)
	Timer(0.8, f)
end




