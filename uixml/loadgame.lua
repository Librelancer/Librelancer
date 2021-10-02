function loadgame:ctor()
	local e = self.Elements
	e.listtable:SetData(Game:SaveGames())
	self.Elements.goback:OnClick(function()
		self:ExitAnimation(function()
			OpenScene("mainmenu")
		end)
	end)	
	self.Elements.load:OnClick(function()
		self:ExitAnimation(function()
			Game:LoadSelectedGame()
		end)
	end)
	self.Elements.delete:OnClick(function()
		Game:DeleteSelectedGame()
	end)
end

function loadgame:Update()
	local scn = self.Elements
	local sv = Game:SaveGames()	
	scn.load.Enabled = sv:ValidSelection()
	scn.delete.Enabled = sv:ValidSelection()
end

function loadgame:ExitAnimation(f)
	f()
end
