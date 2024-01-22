// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

#include "cimgui_ext.h"
#include "imgui.h"
#include "TextEditor.h"
#include <string>
#include <cstring>

IGEXPORT texteditor_t igExtTextEditorInit()
{
	TextEditor *editor = new TextEditor();
    editor->SetShowWhitespaces(false);
	editor->SetColorizerEnable(false);
	return (texteditor_t)editor;
}

IGEXPORT void igExtTextEditorSetMode(texteditor_t textedit, texteditor_mode_t mode)
{
    TextEditor *editor = (TextEditor*)textedit;
    if(mode == TEXTEDITOR_MODE_LUA) {
        editor->SetLanguageDefinition(TextEditor::LanguageDefinition::Lua());
        editor->SetColorizerEnable(true);
    }
    else {
        editor->SetColorizerEnable(false);
    }
}
IGEXPORT void igExtTextEditorSetReadOnly(texteditor_t textedit, int readonly)
{
    TextEditor *editor = (TextEditor*)textedit;
    editor->SetReadOnly(readonly != 0 ? true : false);
}

IGEXPORT const char *igExtTextEditorGetText(texteditor_t textedit)
{
	TextEditor *editor = (TextEditor*)textedit;
	return strdup(editor->GetText().c_str());
}

IGEXPORT void igExtFree(void *mem)
{
	free(mem);
}

IGEXPORT void igExtTextEditorSetText(texteditor_t textedit, const char *text)
{
	TextEditor *editor = (TextEditor*)textedit;
	editor->SetText(text);
}

IGEXPORT int igExtTextEditorIsTextChanged(texteditor_t textedit)
{
	TextEditor *editor = (TextEditor*)textedit;
	return editor->IsTextChanged() ? 1 : 0;
}

IGEXPORT void igExtTextEditorGetCoordinates(texteditor_t textedit, int32_t *x, int32_t *y)
{
	TextEditor *editor = (TextEditor*)textedit;
	auto cpos = editor->GetCursorPosition();
	*x = (int32_t)cpos.mColumn;
	*y = (int32_t)cpos.mLine;
}

IGEXPORT void igExtTextEditorRender(texteditor_t textedit, const char *id)
{
	TextEditor *editor = (TextEditor*)textedit;
	editor->Render(id, false, ImVec2(0,0), false);
}

IGEXPORT void igExtTextEditorFree(texteditor_t textedit)
{
	delete ((TextEditor*)textedit);
}


