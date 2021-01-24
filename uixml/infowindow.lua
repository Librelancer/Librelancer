require 'childwindow.lua'

function infowindow:ctor()
	MakeChildWindow(self)
	self.Elements.info.Infocard = Game:CurrentInfocard()
	self.Elements.close:OnClick(function() 
		self:Close() 
	end)
end

