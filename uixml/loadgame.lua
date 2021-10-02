ModalClass(loadgame)

function loadgame:asmodal()
	self:ModalInit()
	self.Elements.fllogo.Visible = false
	self.Elements.resume.Visible = true
	self.Elements.backdrop.Visible = true
	self.isModal = true
end

function loadgame:ctor()
	local e = self.Elements
	self.isModal = false
	e.listtable:SetData(Game:SaveGames())
	e.resume:OnClick(function()
		Game:Resume()
		self:Close()
	end)
	self.Elements.goback:OnClick(function()
		if self.isModal then
			Game:QuitToMenu()
			self:Close()
		else
			self:ExitAnimation(function()
				OpenScene("mainmenu")
			end)
		end
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


