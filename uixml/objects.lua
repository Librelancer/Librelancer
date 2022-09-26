local objects = {
    UiRenderable = ClrTypes.LibreLancer_Interface_UiRenderable.__new,
    DisplayModel = ClrTypes.LibreLancer_Interface_DisplayModel.__new,
	DisplayColor = ClrTypes.LibreLancer_Interface_DisplayColor.__new,
	DisplayWireBorder = ClrTypes.LibreLancer_Interface_DisplayWireBorder.__new,
    InterfaceModel = ClrTypes.LibreLancer_Interface_InterfaceModel.__new,
    Button = ClrTypes.LibreLancer_Interface_Button.__new,
    ButtonStyle = ClrTypes.LibreLancer_Interface_ButtonStyle.__new,
    ButtonAppearance = ClrTypes.LibreLancer_Interface_ButtonAppearance.__new,
	ListItem = ClrTypes.LibreLancer_Interface_ListItem.__new,
	Panel = ClrTypes.LibreLancer_Interface_Panel.__new,
	TextBlock = ClrTypes.LibreLancer_Interface_TextBlock.__new,
	Gauge = ClrTypes.LibreLancer_Interface_Gauge.__new
}

function NewObject(obj)
{
    return objects[obj]()
}



