if ModalData['Title'] ~= nil then
    GetElement('title').Text = ModalData.Title
end
if ModalData['Content'] ~= nil then
    GetElement('content').Text = ModalData.Content
end
GetElement('close').Clicked:Add(function()
	CloseModal({ Result = 'ok' })
end)



