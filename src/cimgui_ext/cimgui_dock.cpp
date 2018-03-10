#include "cimgui_dock.h"
#include "imgui.h"
#include "imgui_internal.h"
IGEXPORT void *igFontFindGlyph(void *font, unsigned short c)
{
    ImFont *fnt = (ImFont*)font;
    return (void*)fnt->FindGlyph((ImWchar)c);
}
