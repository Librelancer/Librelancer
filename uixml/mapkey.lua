ModalClass(mapkey)

function mapkey:ctor(button, callback)
    local e = self.Elements
    e.keyName.strid = button
    self:ModalInit()
	if callback ~= nil then
    	self:ModalCallback(callback)
	end
    e.clear:OnClick(function()
        self:Close('clear')
    end)
    e.cancel:OnClick(function()
        self:Close('cancel')
    end)
end







