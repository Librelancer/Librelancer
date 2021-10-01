ModalClass(textentry)

function textentry:ctor(cb, title)
	self:ModalInit()
	self:ModalCallback(cb)
	local scn = self.Elements
    scn.content:SetFocus()
	scn.title.Text = title
    scn.content:OnTextEntered(function(name)
        self:Close('ok', name, 0)
    end)
	scn.close:OnClick(function()
		self:Close('cancel')
	end)
end




