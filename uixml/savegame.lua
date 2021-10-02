ModalClass(savegame)

function savegame:ctor()
	local e = self.Elements
	self:ModalInit()
	e.listtable:SetData(Game:SaveGames())
	e.listtable:OnItemSelected(function()
		e.content.CurrentText = Game:SaveGames():CurrentDescription()
	end)
	e.content.MaxChars = 32
	e.resume:OnClick(function()
		Game:Resume()
		self:Close()
	end)
	e.goback:OnClick(function()
		Game:QuitToMenu()
		self:Close()
	end)	
	e.save:OnClick(function()
		Game:SaveGame(e.content.CurrentText)
	end)
	e.content:OnTextEntered(function(name)
		Game:SaveGame(name)
	end)
	e.delete:OnClick(function()
		Game:DeleteSelectedGame()
	end)
end

function savegame:Update()
	local scn = self.Elements
	local sv = Game:SaveGames()	
	scn.delete.Enabled = sv:ValidSelection()
end





