ModalClass(alreadymapped)

function alreadymapped:ctor(key, button, callback)
    local e = self.Elements
	e.input.Text = key
    e.keyName.strid = button
    self:ModalInit()
	if callback ~= nil then
    	self:ModalCallback(callback)
	end
    e.continue:OnClick(function()
        self:Close('continue')
    end)
    e.cancel:OnClick(function()
        self:Close('cancel')
    end)
end








