ModalClass(pausemenu)

function pausemenu:ctor()
	self:ModalInit()
	local e = self.Elements
	e.loadgame.Enabled = not Game:IsMultiplayer()
	e.loadgame:OnClick(function()
		local lg = loadgame()
		lg:asmodal()
		SwapModal(self, lg) 
	end)
	e.savegame.Enabled = not Game:IsMultiplayer()
	e.savegame:OnClick(function()
		local sg = savegame()
		SwapModal(self,sg)
	end)
	e.resume:OnClick(function()
		Game:Resume()
		self:Close()
	end)
	e.quittomenu:OnClick(function()
		Game:QuitToMenu()
		self:Close()
	end)
end



