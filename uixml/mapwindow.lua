require 'childwindow.lua'

function mapwindow:ctor()
	MakeChildWindow(self)	
	self.Elements.exit:OnClick(function()
		self:Close()
	end)
end

function mapwindow:InitMap()
	Game:PopulateNavmap(self.Elements.navmap)
end

