function OnTryCreate(name)
    CloseModal({Result = 'ok', Name = name, Index = 0})
end
--Focus text entry on open
GetElement('content'):SetFocus()
GetElement('content').TextEntered:Add(OnTryCreate)
--Button events
GetElement('close').Clicked:Add(function()
	CloseModal({ Result = 'cancel' })
end)