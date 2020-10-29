local objects = {
    { 'UiRenderable', ClrTypes.LibreLancer_Interface_UiRenderable.__new },
    { 'DisplayModel', ClrTypes.LibreLancer_Interface_DisplayModel.__new },
    { 'InterfaceModel', ClrTypes.LibreLancer_Interface_InterfaceModel.__new },
    { 'Button', ClrTypes.LibreLancer_Interface_Button.__new },
    { 'ButtonStyle', ClrTypes.LibreLancer_Interface_ButtonStyle.__new },
    { 'ButtonAppearance', ClrTypes.LibreLancer_Interface_ButtonAppearance.__new }
}

function NewObject(obj)
    return objects[obj]()
end
