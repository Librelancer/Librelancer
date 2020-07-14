function OnTryCreate(name)
    CloseModal({Result = 'ok', Name = name, Index = 0})
end
--Focus text entry on open
GetElement('content'):SetFocus()
GetElement('content'):OnTextEntered(OnTryCreate)
--Button events
GetElement('close'):OnClick(function()
	CloseModal({ Result = 'cancel' })
end)