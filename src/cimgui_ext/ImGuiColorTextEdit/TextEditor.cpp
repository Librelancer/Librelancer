//	TextEditor - A syntax highlighting text editor for Dear ImGui.
//	Copyright (c) 2024-2026 Johan A. Goossens. All rights reserved.
//
//	This work is licensed under the terms of the MIT license.
//	For a copy, see <https://opensource.org/licenses/MIT>.


//
//	Include files
//

#include <cmath>
#include <limits>

#ifndef IMGUI_DEFINE_MATH_OPERATORS
#define IMGUI_DEFINE_MATH_OPERATORS
#endif

#include "imgui.h"
#include "imgui_internal.h"

#include "TextEditor.h"


//
//	TextEditor::TextEditor
//

TextEditor::TextEditor() {
    SetPalette(defaultPalette);
}


//
//	TextEditor::setText
//

void TextEditor::setText(const std::string_view &text) {
    // load text into document and reset subsystems
    document.setText(text);
    transactions.reset();
    bracketeer.reset();
    colorizer.updateEntireDocument(document, language);
    cursors.clearAll();
    clearMarkers();
    makeCursorVisible();

    showMatchingBracketsChanged = false;
    languageChanged = false;
}


//
//	TextEditor::render
//

void TextEditor::render(const char* title, const ImVec2& size, bool border) {
    // get current transaction version
    auto transActionVersion = transactions.getVersion();

    // update color palette (if required)
    if (paletteAlpha != ImGui::GetStyle().Alpha) {
        updatePalette();
    }

    // get font information and determine horizontal offsets for line numbers, decorations and text
    font = ImGui::GetFont();
    fontSize = ImGui::GetFontSize();
    glyphSize = ImVec2(ImGui::CalcTextSize("#").x, ImGui::GetTextLineHeightWithSpacing() * lineSpacing);
    lineNumberLeftOffset = leftMargin * glyphSize.x;

    if (showLineNumbers) {
        int digits = static_cast<int>(std::log10(document.lineCount() + 1) + 1.0f);
        lineNumberRightOffset = lineNumberLeftOffset + digits * glyphSize.x;
        decorationOffset = lineNumberRightOffset + decorationMargin * glyphSize.x;

    } else {
        lineNumberRightOffset = lineNumberLeftOffset;
        decorationOffset = lineNumberLeftOffset;
    }

    if (decoratorWidth > 0.0f) {
        textOffset = decorationOffset + decoratorWidth + decorationMargin * glyphSize.x;

    } else if (decoratorWidth < 0.0f) {
        textOffset = decorationOffset + (-decoratorWidth + decorationMargin) * glyphSize.x;

    } else {
        textOffset = decorationOffset + textMargin * glyphSize.x;
    }

    // get current position and total/visible editor size
    auto pos = ImGui::GetCursorScreenPos();
    auto totalSize = ImVec2(textOffset + document.getMaxColumn() * glyphSize.x + cursorWidth, document.size() * glyphSize.y);
    auto region = ImGui::GetContentRegionAvail();
    auto visibleSize = ImGui::CalcItemSize(size, region.x, region.y); // messing with Dear ImGui internals

    // see if we have scrollbars
    float scrollbarSize = ImGui::GetStyle().ScrollbarSize;
    verticalScrollBarSize = (totalSize.y > visibleSize.y) ? scrollbarSize : 0.0f;
    horizontalScrollBarSize = (totalSize.x > visibleSize.x) ? scrollbarSize : 0.0f;

    // determine visible lines and columns
    visibleWidth = visibleSize.x - textOffset - verticalScrollBarSize;
    visibleColumns = std::max(static_cast<int>(std::ceil(visibleWidth / glyphSize.x)), 0);
    visibleHeight = visibleSize.y - horizontalScrollBarSize;
    visibleLines = std::max(static_cast<int>(std::ceil(visibleHeight / glyphSize.y)), 0);

    // determine scrolling requirements
    float scrollX = -1.0f;
    float scrollY = -1.0f;

    // ensure cursor is visible (if requested)
    if (ensureCursorIsVisible) {
        auto cursor = cursors.getCurrent().getInteractiveEnd();

        if (cursor.line <= firstVisibleLine + 1) {
            scrollY = std::max(0.0f, (cursor.line - 2.0f) * glyphSize.y);

        } else if (cursor.line >= lastVisibleLine - 1) {
            scrollY = std::max(0.0f, (cursor.line + 2.0f) * glyphSize.y - visibleHeight);
        }

        if (cursor.column <= firstVisibleColumn + 1) {
            scrollX = std::max(0.0f, (cursor.column - 2.0f) * glyphSize.x);

        } else if (cursor.column >= lastVisibleColumn - 1) {
            scrollX = std::max(0.0f, (cursor.column + 2.0f) * glyphSize.x - visibleWidth);
        }

        ensureCursorIsVisible = false;
    }

    // scroll to specified line (if required)
    if (scrollToLineNumber >= 0) {
        scrollToLineNumber = std::min(scrollToLineNumber, document.lineCount());
        scrollX = 0.0f;

        switch (scrollToAlignment) {
            case Scroll::alignTop:
                scrollY = std::max(0.0f, static_cast<float>(scrollToLineNumber) * glyphSize.y);
                break;

            case Scroll::alignMiddle:
                scrollY = std::max(0.0f, static_cast<float>(scrollToLineNumber - visibleLines / 2) * glyphSize.y);
                break;

            case Scroll::alignBottom:
                scrollY = std::max(0.0f, static_cast<float>(scrollToLineNumber - (visibleLines - 1)) * glyphSize.y);
                break;
        }

        scrollToLineNumber = -1;
    }

    // set scroll (if required)
    if (scrollX >= 0.0f || scrollY >= 0.0f) {
        ImGui::SetNextWindowScroll(ImVec2(scrollX, scrollY));
    }

    // ensure editor has focus (if required)
    if (focusOnEditor) {
        ImGui::SetNextWindowFocus();
        focusOnEditor = false;
    }

    // start a new child window
    // this must be done before we handle keyboard and mouse interactions to ensure correct Dear ImGui context
    ImGui::SetNextWindowContentSize(totalSize);
    ImGui::PushStyleVar(ImGuiStyleVar_ItemSpacing, ImVec2(0.0f, 0.0f));
    ImGui::PushStyleColor(ImGuiCol_ChildBg, ImGui::ColorConvertU32ToFloat4(palette.get(Color::background)));
    ImGui::BeginChild(title, size, border, ImGuiWindowFlags_NoMove | ImGuiWindowFlags_HorizontalScrollbar | ImGuiWindowFlags_NoNavInputs);
    lastRenderOrigin = ImGui::GetCursorScreenPos();

    // handle keyboard and mouse inputs
    handleKeyboardInputs();
    handleMouseInteractions();

    // ensure cursors are up to date (sort and merge if required)
    if (cursors.anyHasUpdate()) {
        cursors.update();
    }

    // recolorize entire document and reset brackets (if required)
    if (showMatchingBracketsChanged || languageChanged) {
        colorizer.updateEntireDocument(document, language);
        bracketeer.reset();
    }

    // was document changed during this frame?
    auto documentChanged = document.isUpdated();

    if (language) {
        if (documentChanged) {
            // recolorize updated lines
            colorizer.updateChangedLines(document, language);
        }

        if (showMatchingBrackets && (documentChanged || showMatchingBracketsChanged || languageChanged)) {
            // rebuild bracket list
            bracketeer.update(document);
        }
    }

    // reset changed states
    showMatchingBracketsChanged = false;
    languageChanged = false;

    // determine view parameters
    firstVisibleColumn = std::max(static_cast<int>(std::floor(ImGui::GetScrollX() / glyphSize.x)), 0);
    lastVisibleColumn = static_cast<int>(std::floor((ImGui::GetScrollX() + visibleWidth) / glyphSize.x));
    firstVisibleLine = std::max(static_cast<int>(std::floor(ImGui::GetScrollY() / glyphSize.y)), 0);
    lastVisibleLine = std::min(static_cast<int>(std::floor((ImGui::GetScrollY() + visibleHeight) / glyphSize.y)), document.lineCount() - 1);

    // render editor parts
    renderSelections();
    renderMarkers();
    renderMatchingBrackets();
    renderText();
    renderCursors();
    renderMargin();
    renderLineNumbers();
    renderDecorations();
    renderScrollbarMiniMap();
    renderPanScrollIndicator();

    if (ImGui::BeginPopup("LineNumberContextMenu")) {
        lineNumberContextMenuCallback(contextMenuLine);
        ImGui::EndPopup();
    }

    if (ImGui::BeginPopup("TextContextMenu")) {
        textContextMenuCallback(contextMenuLine, contextMenuColumn);
        ImGui::EndPopup();
    }

    // render find/replace popup
    renderFindReplace(pos, visibleSize.x - verticalScrollBarSize);

    // render autocomplete popup
    if (autocomplete.render(document, cursors, language, textOffset, glyphSize)) {
        // user picked a suggestion so insert it
        auto start = autocomplete.getStart();
        auto end = document.findWordEnd(start, true);
        auto replacement = autocomplete.getReplacement();
        replaceSectionText(start, end, replacement);
    }

    // handle change tracking if there is a change callback in place
    if (delayedChangeCallback) {
        if (delayedChangeDetected) {
            if (std::chrono::system_clock::now() > delayedChangeReportTime) {
                delayedChangeCallback();
                delayedChangeDetected = false;
            }

        } else if (transactions.getVersion() != transActionVersion) {
            delayedChangeDetected = true;
            delayedChangeReportTime = std::chrono::system_clock::now() + delayedChangeDelay;
        }
    }

    ImGui::EndChild();
    ImGui::PopStyleColor();
    ImGui::PopStyleVar();
}


//
//	TextEditor::renderSelections
//

void TextEditor::renderSelections() {
    auto drawList = ImGui::GetWindowDrawList();
    ImVec2 cursorScreenPos = ImGui::GetCursorScreenPos();

    // draw background for selections
    for (auto& cursor : cursors) {
        if (cursor.hasSelection()) {
            auto start = cursor.getSelectionStart();
            auto end = cursor.getSelectionEnd();

            if (end.line >= firstVisibleLine && start.line <= lastVisibleLine) {
                auto first = std::max(start.line, firstVisibleLine);
                auto last = std::min(end.line, lastVisibleLine);

                for (auto line = first; line <= last; line++) {
                    auto x = cursorScreenPos.x + textOffset;
                    auto left = x + (line == first ? start.column : 0) * glyphSize.x;
                    auto right = x + (line == last ? end.column : document[line].maxColumn) * glyphSize.x;
                    auto y = cursorScreenPos.y + line * glyphSize.y;
                    drawList->AddRectFilled(ImVec2(left, y), ImVec2(right, y + glyphSize.y), palette.get(Color::selection));
                }
            }
        }
    }
}


//
//	TextEditor::renderMarkers
//

void TextEditor::renderMarkers() {
    if (markers.size()) {
        auto drawList = ImGui::GetWindowDrawList();
        ImVec2 cursorScreenPos = ImGui::GetCursorScreenPos();

        for (int line = firstVisibleLine; line <= lastVisibleLine; line++) {
            if (document[line].marker) {
                auto& marker = markers[document[line].marker - 1];
                auto y = cursorScreenPos.y + line * glyphSize.y;

                if (((marker.lineNumberColor >> IM_COL32_A_SHIFT) & 0xFF) != 0) {
                    auto left = cursorScreenPos.x + lineNumberLeftOffset;
                    auto right = cursorScreenPos.x + lineNumberRightOffset;
                    auto start = ImVec2(left, y);
                    auto end = ImVec2(right, y + glyphSize.y);
                    drawList->AddRectFilled(start, end, marker.lineNumberColor);

                    if (marker.lineNumberTooltip.size() && ImGui::IsMouseHoveringRect(start, end)) {
                        ImGui::PushStyleColor(ImGuiCol_PopupBg, marker.lineNumberColor);
                        ImGui::BeginTooltip();
                        ImGui::TextUnformatted(marker.lineNumberTooltip.c_str());
                        ImGui::EndTooltip();
                        ImGui::PopStyleColor();
                    }
                }

                if (((marker.textColor >> IM_COL32_A_SHIFT) & 0xFF) != 0) {
                    auto left = cursorScreenPos.x + textOffset;
                    auto right = left + lastVisibleColumn * glyphSize.x;
                    auto start = ImVec2(left, y);
                    auto end = ImVec2(right, y + glyphSize.y);
                    drawList->AddRectFilled(start, end, marker.textColor);

                    if (marker.textTooltip.size() && ImGui::IsMouseHoveringRect(start, end)) {
                        ImGui::PushStyleColor(ImGuiCol_PopupBg, marker.textColor);
                        ImGui::BeginTooltip();
                        ImGui::TextUnformatted(marker.textTooltip.c_str());
                        ImGui::EndTooltip();
                        ImGui::PopStyleColor();
                    }
                }
            }
        }
    }
}


//
//	TextEditor::renderMatchingBrackets
//

void TextEditor::renderMatchingBrackets() {
    if (showMatchingBrackets) {
        if (bracketeer.size()) {
            auto drawList = ImGui::GetWindowDrawList();
            ImVec2 cursorScreenPos = ImGui::GetCursorScreenPos();

            // render bracket pair lines
            for (auto& bracket : bracketeer) {
                if ((bracket.end.line - bracket.start.line) > 1 &&
                    bracket.start.line <= lastVisibleLine &&
                    bracket.end.line > firstVisibleLine) {

                    auto lineX = cursorScreenPos.x + textOffset + std::min(bracket.start.column, bracket.end.column) * glyphSize.x;
                auto startY = cursorScreenPos.y + (bracket.start.line + 1) * glyphSize.y;
                auto endY = cursorScreenPos.y + bracket.end.line * glyphSize.y;
                drawList->AddLine(ImVec2(lineX, startY), ImVec2(lineX, endY), palette.get(Color::whitespace), 1.0f);
                    }
            }

            // render active bracket pair
            auto active = bracketeer.getEnclosingBrackets(cursors.getMain().getInteractiveEnd());

            if (active != bracketeer.end() &&
                active->start.line <= lastVisibleLine &&
                active->end.line > firstVisibleLine) {

                auto x1 = cursorScreenPos.x + textOffset + active->start.column * glyphSize.x;
            auto y1 = cursorScreenPos.y + active->start.line * glyphSize.y;
            drawList->AddRectFilled(ImVec2(x1, y1), ImVec2(x1 + glyphSize.x, y1 + glyphSize.y), palette.get(Color::matchingBracketBackground));

            auto x2 = cursorScreenPos.x + textOffset + active->end.column * glyphSize.x;
            auto y2 = cursorScreenPos.y + active->end.line * glyphSize.y;
            drawList->AddRectFilled(ImVec2(x2, y2), ImVec2(x2 + glyphSize.x, y2 + glyphSize.y), palette.get(Color::matchingBracketBackground));

            if (active->end.line - active->start.line > 1) {
                auto lineX = std::min(x1, x2);
                drawList->AddLine(ImVec2(lineX, y1 + glyphSize.y), ImVec2(lineX, y2), palette.get(Color::matchingBracketActive), 1.0f);
            }
                }
        }
    }
}


//
//	TextEditor::renderText
//

void TextEditor::renderText() {
    auto drawList = ImGui::GetWindowDrawList();
    ImVec2 cursorScreenPos = ImGui::GetCursorScreenPos();
    ImVec2 lineScreenPos = cursorScreenPos + ImVec2(textOffset, firstVisibleLine * glyphSize.y);
    auto tabSize = document.getTabSize();
    auto firstRenderableColumn = (firstVisibleColumn / tabSize) * tabSize;

    for (int i = firstVisibleLine; i <= lastVisibleLine; i++) {
        auto& line = document[i];

        // draw colored glyphs for current line
        auto column = firstRenderableColumn;
        auto index = document.getIndex(line, column);
        auto lineSize = line.size();

        while (index < lineSize && column <= lastVisibleColumn) {
            auto& glyph = line[index];
            auto codepoint = glyph.codepoint;
            ImVec2 glyphPos{lineScreenPos.x + column * glyphSize.x, lineScreenPos.y};

            if (codepoint == '\t') {
                if (showTabs) {
                    const auto x1 = glyphPos.x + glyphSize.x * 0.3f;
                    const auto y = glyphPos.y + fontSize * 0.5f;
                    const auto x2 = glyphPos.x + glyphSize.x;

                    ImVec2 p1, p2, p3, p4;
                    p1 = ImVec2(x1, y);
                    p2 = ImVec2(x2, y);
                    p3 = ImVec2(x2 - fontSize * 0.16f, y - fontSize * 0.16f);
                    p4 = ImVec2(x2 - fontSize * 0.16f, y + fontSize * 0.16f);

                    drawList->AddLine(p1, p2, palette.get(Color::whitespace));
                    drawList->AddLine(p2, p3, palette.get(Color::whitespace));
                    drawList->AddLine(p2, p4, palette.get(Color::whitespace));
                }

            } else if (codepoint == ' ') {
                if (showSpaces) {
                    const auto x = glyphPos.x + glyphSize.x * 0.5f;
                    const auto y = glyphPos.y + fontSize * 0.5f;
                    drawList->AddCircleFilled(ImVec2(x, y), 1.5f, palette.get(Color::whitespace), 4);
                }

            } else {
                font->RenderChar(drawList, fontSize, glyphPos, palette.get(glyph.color), codepoint);
            }

            index++;
            column += (codepoint == '\t') ? tabSize - (column % tabSize) : 1;
        }

        lineScreenPos.y += glyphSize.y;
    }
}


//
//	TextEditor::renderCursors
//

void TextEditor::renderCursors() {
    // update cursor animation timer
    cursorAnimationTimer = std::fmod(cursorAnimationTimer + ImGui::GetIO().DeltaTime, 1.0f);

    if (ImGui::IsWindowFocused()) {
        ImVec2 cursorScreenPos = ImGui::GetCursorScreenPos();

        if (!ImGui::GetIO().ConfigInputTextCursorBlink || cursorAnimationTimer < 0.5f) {
            auto drawList = ImGui::GetWindowDrawList();

            for (auto& cursor : cursors) {
                auto pos = cursor.getInteractiveEnd();

                if (pos.line >= firstVisibleLine && pos.line <= lastVisibleLine) {
                    auto x = cursorScreenPos.x + textOffset + pos.column * glyphSize.x - 1;
                    auto y = cursorScreenPos.y + pos.line * glyphSize.y;
                    drawList->AddRectFilled(ImVec2(x, y), ImVec2(x + cursorWidth, y + glyphSize.y), palette.get(Color::cursor));
                }
            }
        }

        // notify OS of text input position for advanced Input Method Editor (IME)
        // this is required for the SDL3 backend as it will not report text input events unless we do this
        // see https://github.com/ocornut/imgui/issues/8584 for details
        if (!readOnly) {
            auto context = ImGui::GetCurrentContext();
            context->PlatformImeData.WantVisible = true;
            context->PlatformImeData.WantTextInput = true;
            context->PlatformImeData.InputPos = ImVec2(cursorScreenPos.x - 1.0f, cursorScreenPos.y - context->FontSize);
            context->PlatformImeData.InputLineHeight = context->FontSize;
            context->PlatformImeData.ViewportId = ImGui::GetCurrentWindow()->Viewport->ID;
        }
    }
}


//
//	TextEditor::renderMargin
//

void TextEditor::renderMargin() {
    if ((decoratorWidth != 0.0f && decoratorCallback) || showLineNumbers) {
        // erase background in case we are scrolling horizontally
        if (ImGui::GetScrollX() > 0.0f) {
            ImGui::GetWindowDrawList()->AddRectFilled(
                ImGui::GetWindowPos(),
                                                      ImGui::GetWindowPos() + ImVec2(textOffset, ImGui::GetWindowSize().y),
                                                      palette.get(Color::background));
        }
    }
}


//
//	TextEditor::renderLineNumbers
//

void TextEditor::renderLineNumbers() {
    if (showLineNumbers) {
        auto drawList = ImGui::GetWindowDrawList();
        auto cursorScreenPos = ImGui::GetCursorScreenPos();
        auto curserLine = cursors.getCurrent().getInteractiveEnd().line;
        auto position = ImVec2(ImGui::GetWindowPos().x + lineNumberRightOffset, cursorScreenPos.y);

        for (int i = firstVisibleLine; i <= lastVisibleLine; i++) {
            auto width = static_cast<int>(std::log10(i + 1) + 1.0f) * glyphSize.x;
            auto foreground = (i == curserLine) ? Color::currentLineNumber : Color::lineNumber;
            auto number = std::to_string(i + 1);
            drawList->AddText(position + ImVec2(-width, i * glyphSize.y), palette.get(foreground), number.c_str());
        }
    }
}


//
//	TextEditor::renderDecorations
//

void TextEditor::renderDecorations() {
    if (decoratorWidth != 0.0f && decoratorCallback) {
        auto cursorScreenPos = ImGui::GetCursorScreenPos();
        auto position = ImVec2(ImGui::GetWindowPos().x + decorationOffset, cursorScreenPos.y + glyphSize.y * firstVisibleLine);
        auto widthInPixels = (decoratorWidth < 0.0f) ? -decoratorWidth * glyphSize.x: decoratorWidth;
        Decorator decorator{0, widthInPixels, glyphSize.y, glyphSize, nullptr};

        for (int i = firstVisibleLine; i <= lastVisibleLine; i++) {
            decorator.line = i;
            decorator.userData = document.getUserData(i);
            ImGui::SetCursorScreenPos(position);
            ImGui::PushID(i);
            decoratorCallback(decorator);
            ImGui::PopID();
            position.y += glyphSize.y;
        }

        ImGui::SetCursorScreenPos(cursorScreenPos);
    }
}


//
//	TextEditor::renderScrollbarMiniMap
//

void TextEditor::renderScrollbarMiniMap() {
    // based on https://github.com/ocornut/imgui/issues/3114
    // messing with Dear ImGui internals
    if (showScrollbarMiniMap) {
        auto window = ImGui::GetCurrentWindow();

        if (window->ScrollbarY) {
            auto drawList = ImGui::GetWindowDrawList();
            auto rect = ImGui::GetWindowScrollbarRect(window, ImGuiAxis_Y);
            auto lineHeight = rect.GetHeight() / static_cast<float>(document.size());
            auto offset = (rect.Max.x - rect.Min.x) * 0.3f;
            auto left = rect.Min.x + offset;
            auto right = rect.Max.x - offset;

            drawList->PushClipRect(rect.Min, rect.Max, false);

            // render cursor locations
            for (auto& cursor : cursors) {
                auto begin = cursor.getSelectionStart();
                auto end = cursor.getSelectionEnd();

                auto ly1 = std::round(rect.Min.y + begin.line * lineHeight);
                auto ly2 = std::round(rect.Min.y + (end.line + 1) * lineHeight);

                drawList->AddRectFilled(ImVec2(left, ly1), ImVec2(right, ly2), palette.get(Color::selection));
            }

            // render marker locations
            if (markers.size()) {
                for (size_t line = 0; line < document.size(); line++) {
                    if (document[line].marker) {
                        auto color = markers[document[line].marker - 1].textColor;

                        if (!color) {
                            color = markers[document[line].marker - 1].lineNumberColor;
                        }

                        auto ly = std::round(rect.Min.y + line * lineHeight);
                        drawList->AddRectFilled(ImVec2(left, ly), ImVec2(right, ly + lineHeight), color);
                    }
                }
            }

            drawList->PopClipRect();
        }
    }
}


//
//	TextEditor::renderPanScrollIndicator
//

void TextEditor::renderPanScrollIndicator() {
    if (showPanScrollIndicator && (panning || scrolling)) {
        auto drawList = ImGui::GetWindowDrawList();
        auto center =ImGui::GetWindowPos() + ImGui::GetWindowSize() / 2.0f;
        static constexpr int alpha = 160;
        drawList->AddCircleFilled(center, 20.0f, IM_COL32(255, 255, 255, alpha));
        drawList->AddCircle(center, 5.0f, IM_COL32(0, 0, 0, alpha), 0, 2.0f);

        drawList->AddTriangle(
            ImVec2(center.x - 15.0f, center.y),
                              ImVec2(center.x - 8.0f, center.y - 4.0f),
                              ImVec2(center.x - 8.0f, center.y + 4.0f),
                              IM_COL32(0, 0, 0, alpha),
                              2.0f);

        drawList->AddTriangle(
            ImVec2(center.x + 15.0f, center.y),
                              ImVec2(center.x + 8.0f, center.y - 4.0f),
                              ImVec2(center.x + 8.0f, center.y + 4.0f),
                              IM_COL32(0, 0, 0, alpha),
                              2.0f);

        drawList->AddTriangle(
            ImVec2(center.x, center.y - 15.0f),
                              ImVec2(center.x - 4.0f, center.y - 8.0f),
                              ImVec2(center.x + 4.0f, center.y - 8.0f),
                              IM_COL32(0, 0, 0, alpha),
                              2.0f);

        drawList->AddTriangle(
            ImVec2(center.x, center.y + 15.0f),
                              ImVec2(center.x - 4.0f, center.y + 8.0f),
                              ImVec2(center.x + 4.0f, center.y + 8.0f),
                              IM_COL32(0, 0, 0, alpha),
                              2.0f);
    }
}


//
//	TextEditor::handleKeyboardInputs
//

void TextEditor::handleKeyboardInputs() {
    if (ImGui::IsWindowFocused()) {
        auto& io = ImGui::GetIO();
        io.WantCaptureKeyboard = true;
        io.WantTextInput = true;

        // get state of modifier keys
        auto shift = ImGui::IsKeyDown(ImGuiMod_Shift);
        auto ctrl = ImGui::IsKeyDown(ImGuiMod_Ctrl);
        auto alt = ImGui::IsKeyDown(ImGuiMod_Alt);

        auto isNoModifiers = !ctrl && !shift && !alt;
        auto isShortcut = ctrl && !shift && !alt;
        auto isShiftShortcut = ctrl && shift && !alt;
        auto isOptionalShiftShortcut = ctrl && !alt;
        auto isAltOnly = !ctrl && !shift && alt;
        auto isShiftOnly = !ctrl && shift && !alt;
        auto isOptionalShift = !ctrl && !alt;
        auto isOptionalAlt = !ctrl && !shift;

        #if __APPLE__
        // Dear ImGui switches the Cmd(Super) and Ctrl keys on MacOS
        auto super = ImGui::IsKeyDown(ImGuiMod_Super);
        auto isCtrlShift = !ctrl && shift && !alt && super;
        auto isOptionalAltShift = !ctrl;
        #else
        auto isShiftAlt = !ctrl && shift && alt;
        auto isOptionalCtrlShift = !alt;
        #endif

        // ignore specific keys when autocomplete is active, they will be handled later
        if (autocomplete.isActive() && autocomplete.isSpecialKeyPressed()) {
            if (autocomplete.hasSuggestions()) {
                return;

            } else {
                // this is the exception, cancel autocomplete when special keys are used without any suggestions
                autocomplete.cancel();
            }
        }

        // cursor movements and selections
        if (isOptionalShift && ImGui::IsKeyPressed(ImGuiKey_UpArrow)) { moveUp(1, shift); }
        else if (isOptionalShift && ImGui::IsKeyPressed(ImGuiKey_DownArrow)) { moveDown(1, shift); }

        #if __APPLE__
        else if (isCtrlShift && ImGui::IsKeyPressed(ImGuiKey_LeftArrow)) { shrinkSelectionsToCurlyBrackets(); }
        else if (isCtrlShift && ImGui::IsKeyPressed(ImGuiKey_RightArrow)) { growSelectionsToCurlyBrackets(); }
        else if (isOptionalAltShift && ImGui::IsKeyPressed(ImGuiKey_LeftArrow)) { moveLeft(shift, alt); }
        else if (isOptionalAltShift && ImGui::IsKeyPressed(ImGuiKey_RightArrow)) { moveRight(shift, alt); }
        #else
        else if (isShiftAlt && ImGui::IsKeyPressed(ImGuiKey_LeftArrow)) { shrinkSelectionsToCurlyBrackets(); }
        else if (isShiftAlt && ImGui::IsKeyPressed(ImGuiKey_RightArrow)) { growSelectionsToCurlyBrackets(); }
        else if (isOptionalCtrlShift && ImGui::IsKeyPressed(ImGuiKey_LeftArrow)) { moveLeft(shift, ctrl); }
        else if (isOptionalCtrlShift && ImGui::IsKeyPressed(ImGuiKey_RightArrow)) { moveRight(shift, ctrl); }
        #endif

        else if (isOptionalShift && ImGui::IsKeyPressed(ImGuiKey_PageUp)) { moveUp(visibleLines - 2, shift); }
        else if (isOptionalShift && ImGui::IsKeyPressed(ImGuiKey_PageDown)) { moveDown(visibleLines - 2, shift); }
        else if (isOptionalShiftShortcut && ImGui::IsKeyPressed(ImGuiKey_UpArrow)) { moveToTop(shift); }
        else if (isOptionalShiftShortcut && ImGui::IsKeyPressed(ImGuiKey_Home)) { moveToTop(shift); }
        else if (isOptionalShiftShortcut && ImGui::IsKeyPressed(ImGuiKey_DownArrow)) { moveToBottom(shift); }
        else if (isOptionalShiftShortcut && ImGui::IsKeyPressed(ImGuiKey_End)) { moveToBottom(shift); }
        else if (isOptionalShift && ImGui::IsKeyPressed(ImGuiKey_Home)) { moveToStartOfLine(shift); }
        else if (isOptionalShift && ImGui::IsKeyPressed(ImGuiKey_End)) { moveToEndOfLine(shift); }
        else if (isShortcut && ImGui::IsKeyPressed(ImGuiKey_A)) { selectAll(); }
        else if (isShortcut && ImGui::IsKeyPressed(ImGuiKey_D) && cursors.currentCursorHasSelection()) { addNextOccurrence(); }

        // clipboard operations
        else if (isShortcut && ImGui::IsKeyPressed(ImGuiKey_X)) { cut(); }
        else if (isShiftOnly && ImGui::IsKeyPressed(ImGuiKey_Delete)) { cut(); }
        else if (isShortcut && ImGui::IsKeyPressed(ImGuiKey_C)) { copy() ;}
        else if (isShortcut && ImGui::IsKeyPressed(ImGuiKey_Insert)) { copy(); }

        else if (!readOnly && isShortcut && ImGui::IsKeyPressed(ImGuiKey_V)) { paste(); }
        else if (!readOnly && isShiftOnly && ImGui::IsKeyPressed(ImGuiKey_Insert)) { paste(); }
        else if (!readOnly && isShortcut && ImGui::IsKeyPressed(ImGuiKey_Z)) { undo(); }
        else if (!readOnly && isShiftShortcut && ImGui::IsKeyPressed(ImGuiKey_Z)) { redo(); }
        else if (!readOnly && isShortcut && ImGui::IsKeyPressed(ImGuiKey_Y)) { redo(); }

        // remove text
        else if (!readOnly && isOptionalAlt && ImGui::IsKeyPressed(ImGuiKey_Delete)) { handleDelete(alt); }
        else if (!readOnly && isOptionalAlt && ImGui::IsKeyPressed(ImGuiKey_Backspace)) { handleBackspace(alt); }
        else if (!readOnly && isShiftShortcut && ImGui::IsKeyPressed(ImGuiKey_K)) { removeSelectedLines(); }

        // text manipulation
        else if (!readOnly && isShortcut && ImGui::IsKeyPressed(ImGuiKey_LeftBracket)) { deindentLines(); }
        else if (!readOnly && isShortcut && ImGui::IsKeyPressed(ImGuiKey_RightBracket)) { indentLines(); }
        else if (!readOnly && isAltOnly && ImGui::IsKeyPressed(ImGuiKey_UpArrow)) { moveUpLines(); }
        else if (!readOnly && isAltOnly && ImGui::IsKeyPressed(ImGuiKey_DownArrow)) { moveDownLines(); }
        else if (!readOnly && language && isShortcut && ImGui::IsKeyPressed(ImGuiKey_Slash)) { toggleComments(); }

        // find/replace support
        else if (isShortcut && ImGui::IsKeyPressed(ImGuiKey_F)) {
            if (autocomplete.isActive()) {
                autocomplete.cancel();
                findCancelledAutocomplete = true;
            }

            openFindReplace();
        }

        else if (isShiftShortcut && ImGui::IsKeyPressed(ImGuiKey_F)) { findAll(); }
        else if (isShortcut && ImGui::IsKeyPressed(ImGuiKey_G)) { findNext(); }

        // autocomplete support
        else if (!readOnly && ImGui::IsKeyChordPressed(autocomplete.getTriggerShortcut())) {
            // don't activate if we have multiple cursors active
            if (cursors.hasMultiple()) {
                // TODO: inform user

            } else {
                if (autocomplete.startShortcut(cursors)) {
                    makeCursorVisible();
                }
            }
        }

        // change insert mode
        else if (isNoModifiers && ImGui::IsKeyPressed(ImGuiKey_Insert)) { overwrite = !overwrite; }

        // handle new line
        else if (!readOnly && isNoModifiers && (ImGui::IsKeyPressed(ImGuiKey_Enter) || ImGui::IsKeyPressed(ImGuiKey_KeypadEnter))) { handleCharacter('\n'); }
        else if (!readOnly && isShortcut && (ImGui::IsKeyPressed(ImGuiKey_Enter) || ImGui::IsKeyPressed(ImGuiKey_KeypadEnter))) { insertLineBelow(); }
        else if (!readOnly && isShiftShortcut && (ImGui::IsKeyPressed(ImGuiKey_Enter) || ImGui::IsKeyPressed(ImGuiKey_KeypadEnter))) { insertLineAbove(); }

        // handle tabs
        else if (!readOnly && isOptionalShift && ImGui::IsKeyPressed(ImGuiKey_Tab)) {
            if (cursors.anyHasSelection()) {
                if (shift) {
                    deindentLines();

                } else {
                    indentLines();
                }

            } else {
                handleCharacter('\t');
            }
        }

        // handle escape key
        else if (ImGui::IsKeyPressed(ImGuiKey_Escape)) {
            if (autocomplete.isActive()) {
                autocomplete.cancel();

            } else if (findReplaceVisible) {
                closeFindReplace();

            } else if (cursors.hasMultiple()) {
                cursors.clearAdditional();
            }
        }

        // handle regular text
        if (!io.InputQueueCharacters.empty()) {
            // ignore Ctrl inputs, but need to allow Alt+Ctrl as some keyboards (e.g. German) use AltGR (which is Alt+Ctrl) to input certain characters
            if (!(io.KeyCtrl && !io.KeyAlt) && !readOnly) {
                for (auto i = 0; i < io.InputQueueCharacters.size(); i++) {
                    auto character = io.InputQueueCharacters[i];

                    if (character == '\n' || character >= 32) {
                        handleCharacter(character);
                    }
                }
            }

            io.InputQueueCharacters.resize(0);
        }
    }
}


//
//	TextEditor::handleMouseInteractions
//

void TextEditor::handleMouseInteractions() {
    // handle middle mouse button modes
    panning &= panMode && ImGui::IsMouseDown(ImGuiMouseButton_Middle);
    auto absoluteMousePos = ImGui::GetMousePos() - ImGui::GetWindowPos();

    if (panning && ImGui::IsMouseDragging(ImGuiMouseButton_Middle)) {
        // handle middle mouse button panning
        auto windowSize = ImGui::GetWindowSize();
        auto mouseDelta = ImGui::GetMouseDragDelta(ImGuiMouseButton_Middle);
        float dragFactor = ImGui::GetIO().DeltaTime * 15.0f;
        ImVec2 autoPanMargin(glyphSize.x * 4.0f, glyphSize.y * 2.0f);

        if (absoluteMousePos.x < textOffset + autoPanMargin.x) {
            mouseDelta.x = (absoluteMousePos.x - (textOffset + autoPanMargin.x)) * dragFactor;

        } else if (absoluteMousePos.x > windowSize.x - verticalScrollBarSize - autoPanMargin.x) {
            mouseDelta.x = (absoluteMousePos.x - (windowSize.x - verticalScrollBarSize - autoPanMargin.x)) * dragFactor;
        }

        if (absoluteMousePos.y < autoPanMargin.y) {
            mouseDelta.y = (absoluteMousePos.y - autoPanMargin.y) * dragFactor;

        } else if (absoluteMousePos.y > windowSize.y - horizontalScrollBarSize - autoPanMargin.y) {
            mouseDelta.y = (absoluteMousePos.y - (windowSize.y - horizontalScrollBarSize - autoPanMargin.y)) * dragFactor;
        }

        ImGui::SetScrollX(ImGui::GetScrollX() - mouseDelta.x);
        ImGui::SetScrollY(ImGui::GetScrollY() - mouseDelta.y);
        ImGui::ResetMouseDragDelta(ImGuiMouseButton_Middle);

    } else if (scrolling) {
        // handle middle mouse button scrolling
        float deadzone = glyphSize.x;
        auto offset = scrollStart - absoluteMousePos;
        offset.x = (offset.x < 0.0f) ? std::min(offset.x + deadzone, 0.0f) : std::max(offset.x - deadzone, 0.0f);
        offset.y = (offset.y < 0.0f) ? std::min(offset.y + deadzone, 0.0f) : std::max(offset.y - deadzone, 0.0f);

        float scrollFactor = ImGui::GetIO().DeltaTime * 5.0f;
        offset *= scrollFactor;

        ImGui::SetScrollX(ImGui::GetScrollX() - offset.x);
        ImGui::SetScrollY(ImGui::GetScrollY() - offset.y);

        if (ImGui::IsMouseClicked(ImGuiMouseButton_Left) ||
            ImGui::IsMouseClicked(ImGuiMouseButton_Middle) ||
            ImGui::IsMouseClicked(ImGuiMouseButton_Right)) {

            scrolling = false;
            }

            // ignore other interactions when the editor is not hovered
    } else if (ImGui::IsWindowHovered()) {
        auto io = ImGui::GetIO();
        auto mousePos = ImGui::GetMousePos() - ImGui::GetCursorScreenPos();
        bool overLineNumbers = showLineNumbers && (absoluteMousePos.x > lineNumberLeftOffset) && (absoluteMousePos.x < lineNumberRightOffset);
        bool overText = mousePos.x - ImGui::GetScrollX() > textOffset;

        Coordinate glyphCoordinate;
        Coordinate cursorCoordinate;

        document.normalizeCoordinate(
            mousePos.y / glyphSize.y,
            (mousePos.x - textOffset) / glyphSize.x,
                                     glyphCoordinate,
                                     cursorCoordinate);

        // show text cursor if required
        if (ImGui::IsWindowFocused() && overText) {
            ImGui::SetMouseCursor(ImGuiMouseCursor_TextInput);
        }

        if (ImGui::IsMouseDragging(ImGuiMouseButton_Left)) {
            // update selection with dragging left mouse button
            io.WantCaptureMouse = true;

            if (overLineNumbers) {
                auto& cursor = cursors.getCurrent();
                auto start = Coordinate(cursorCoordinate.line, 0);
                auto end = document.getDown(start);
                cursor.update(cursor.getInteractiveEnd() < cursor.getInteractiveStart() ? start : end);

            } else {
                cursors.updateCurrentCursor(cursorCoordinate);
            }

            makeCursorVisible();

        } else if (ImGui::IsMouseClicked(ImGuiMouseButton_Middle)) {
            // start panning/scrolling mode on middle mouse click
            if (panMode) {
                panning = true;

            } else {
                scrolling = true;
                scrollStart = absoluteMousePos;
            }

        } else if (ImGui::IsMouseClicked(ImGuiMouseButton_Right)) {
            // handle right clicks by setting up context menu (if required)
            if (overLineNumbers && lineNumberContextMenuCallback) {
                contextMenuLine = glyphCoordinate.line;
                ImGui::OpenPopup("LineNumberContextMenu");

            } else if (overText && textContextMenuCallback) {
                contextMenuLine = glyphCoordinate.line;
                contextMenuColumn = glyphCoordinate.column;
                ImGui::OpenPopup("TextContextMenu");
            }

        } else if (ImGui::IsMouseClicked(ImGuiMouseButton_Left)) {
            // handle left mouse button actions
            auto click = ImGui::IsMouseClicked(ImGuiMouseButton_Left);
            auto doubleClick = ImGui::IsMouseDoubleClicked(ImGuiMouseButton_Left);
            auto now = static_cast<float>(ImGui::GetTime());
            auto tripleClick = click && !doubleClick && (lastClickTime != -1.0f && (now - lastClickTime) < io.MouseDoubleClickTime);

            if (click || doubleClick || tripleClick) {
                lastClickTime = tripleClick ? -1.0f : now;
            }

            if (tripleClick) {
                // left mouse button triple click
                if (overText) {
                    auto start = document.getStartOfLine(cursorCoordinate);
                    auto end = document.getDown(start);
                    cursors.updateCurrentCursor(start, end);
                }

            } else if (doubleClick) {
                // left mouse button double click
                if (overText) {
                    auto codepoint = document.getCodePoint(glyphCoordinate);
                    bool handled = false;

                    // select bracketed section (if required)
                    if (CodePoint::isBracketOpener(codepoint)) {
                        auto brackets = bracketeer.getEnclosingBrackets(document.getRight(glyphCoordinate));

                        if (brackets != bracketeer.end()) {
                            if (ImGui::IsKeyDown(ImGuiMod_Shift)) {
                                cursors.setCursor(brackets->start, document.getRight(brackets->end));

                            } else {
                                cursors.setCursor(document.getRight(brackets->start), brackets->end);
                            }

                            handled = true;
                        }

                    } else if (CodePoint::isBracketCloser(codepoint)) {
                        auto brackets = bracketeer.getEnclosingBrackets(glyphCoordinate);

                        if (brackets != bracketeer.end()) {
                            cursors.setCursor(brackets->start, document.getRight(brackets->end));
                            handled = true;
                        }
                    }

                    // select "word" if it wasn't a bracketed section
                    // includes whitespace and operator sequences as well
                    if (!handled && !document.isEndOfLine(glyphCoordinate)) {
                        auto start = document.findWordStart(glyphCoordinate);
                        auto end = document.findWordEnd(glyphCoordinate);
                        cursors.updateCurrentCursor(start, end);
                    }
                }

            } else if (click) {
                // left mouse button single click
                auto extendCursor = ImGui::IsKeyDown(ImGuiMod_Shift);

                #if __APPLE__
                auto addCursor = ImGui::IsKeyDown(ImGuiMod_Alt);
                #else
                auto addCursor = ImGui::IsKeyDown(ImGuiMod_Ctrl);
                #endif

                if (overLineNumbers) {
                    // handle line number clicks
                    auto start = Coordinate(cursorCoordinate.line, 0);
                    auto end = document.getDown(start);

                    if (extendCursor) {
                        auto& cursor = cursors.getCurrent();
                        cursor.update(cursor.getInteractiveEnd() < cursor.getInteractiveStart() ? start : end);
                        autocomplete.cancel();

                    } else if (addCursor) {
                        cursors.addCursor(start, end);
                        autocomplete.cancel();

                    } else {
                        cursors.setCursor(start, end);
                    }

                    makeCursorVisible();

                } else if (overText) {
                    // handle mouse clicks in text
                    if (extendCursor) {
                        cursors.updateCurrentCursor(cursorCoordinate);
                        autocomplete.cancel();

                    } else if (addCursor) {
                        cursors.addCursor(cursorCoordinate);
                        autocomplete.cancel();

                    } else {
                        cursors.setCursor(cursorCoordinate);
                    }

                    makeCursorVisible();
                }
            }
        }
    }
}


//
//	TextEditor::selectAll
//

void TextEditor::selectAll() {
    moveToTop(false);
    moveToBottom(true);
}


//
//	TextEditor::selectLine
//

void TextEditor::selectLine(int line) {
    Coordinate start{line, 0};
    moveTo(start, false);
    moveTo(document.getDown(start), true);
}


//
//	TextEditor::selectLines
//

void TextEditor::selectLines(int startLine, int endLine) {
    Coordinate start{startLine, 0};
    moveTo(start, false);
    moveTo(document.getDown(start, endLine - startLine + 1), true);
}


//
//	TextEditor::selectRegion
//

void TextEditor::selectRegion(int startLine, int startColumn, int endLine, int endColumn) {
    auto start = document.normalizeCoordinate(Coordinate(startLine, startColumn));
    auto end = document.normalizeCoordinate(Coordinate(endLine, endColumn));

    if (end < start) {
        std::swap(start, end);
    }

    cursors.setCursor(start, end);
}


//
//	TextEditor::selectToBrackets
//

void TextEditor::selectToBrackets(bool includeBrackets) {
    if (!showMatchingBrackets) {
        bracketeer.update(document);
    }

    for (auto& cursor : cursors) {
        auto bracket = bracketeer.getEnclosingBrackets(cursor.getSelectionStart());

        if (bracket != bracketeer.end()) {
            if (includeBrackets) {
                cursor.update(bracket->start, document.getRight(bracket->end));

            } else {
                cursor.update(document.getRight(bracket->start), bracket->end);
            }
        }
    }
}


//
//	TextEditor::growSelectionsToCurlyBrackets
//

void TextEditor::growSelectionsToCurlyBrackets() {
    if (!showMatchingBrackets) {
        bracketeer.update(document);
    }

    for (auto& cursor : cursors) {
        auto start = cursor.getSelectionStart();
        auto end = cursor.getSelectionEnd();
        auto startCodePoint = document.getCodePoint(document.getLeft(start));
        auto endCodePoint = document.getCodePoint(end);

        if (startCodePoint == CodePoint::openCurlyBracket && endCodePoint == CodePoint::closeCurlyBracket) {
            cursor.update(document.getLeft(start),document.getRight(end));

        } else {
            auto bracket = bracketeer.getEnclosingCurlyBrackets(start, end);

            if (bracket != bracketeer.end()) {
                cursor.update(document.getRight(bracket->start), bracket->end);
            }
        }
    }
}


//
//	TextEditor::shrinkSelectionsToCurlyBrackets
//

void TextEditor::shrinkSelectionsToCurlyBrackets() {
    if (!showMatchingBrackets) {
        bracketeer.update(document);
    }

    for (auto& cursor : cursors) {
        if (cursor.hasSelection()){
            auto start = cursor.getSelectionStart();
            auto end = cursor.getSelectionEnd();
            auto startCodePoint = document.getCodePoint(start);
            auto endCodePoint = document.getCodePoint(document.getLeft(end));

            if (startCodePoint == CodePoint::openCurlyBracket && endCodePoint == CodePoint::closeCurlyBracket) {
                cursor.update(document.getRight(start),document.getLeft(end));

            } else {
                auto bracket = bracketeer.getInnerCurlyBrackets(start, end);

                if (bracket != bracketeer.end()) {
                    cursor.update(bracket->start, document.getRight(bracket->end));
                }
            }
        }
    }
}


//
//	TextEditor::cut
//

void TextEditor::cut() {
    // copy selections to clipboard and remove them
    copy();
    auto transaction = startTransaction();
    deleteTextFromAllCursors(transaction);
    cursors.getCurrent().resetToStart();
    endTransaction(transaction);
}


//
//	TextEditor::copy
//

void TextEditor::copy() const {
    // copy all selections and put them on the clipboard
    // empty cursors copy the entire line
    std::string text;

    if (cursors.anyHasSelection()) {
        for (auto& cursor : cursors) {
            if (text.size()) {
                text += "\n";
            }

            if (cursor.hasSelection()) {
                text += document.getSectionText(cursor.getSelectionStart(), cursor.getSelectionEnd());

            } else {
                text += document.getLineText(cursor.getSelectionStart().line);
            }
        }

    } else {
        for (auto& cursor : cursors) {
            text += document.getLineText(cursor.getSelectionStart().line) + "\n";
        }
    }

    ImGui::SetClipboardText(text.c_str());
}


//
//	TextEditor::paste
//

void TextEditor::paste() {
    // ignore non-text clipboard content
    auto clipboard = ImGui::GetClipboardText();

    if (clipboard) {
        auto transaction = startTransaction();
        insertTextIntoAllCursors(transaction, clipboard);
        endTransaction(transaction);
    }
}


//
//	TextEditor::undo
//

void TextEditor::undo() {
    if (transactions.canUndo()) {
        transactions.undo(document, cursors);
        makeCursorVisible();
    }
}


//
//	TextEditor::redo
//

void TextEditor::redo() {
    if (transactions.canRedo()) {
        transactions.redo(document, cursors);
        makeCursorVisible();
    }
}


//
//	TextEditor::getCursor
//

void TextEditor::getCursor(int& line, int& column, size_t cursor) const {
    cursor = std::min(cursor, cursors.size() - 1);
    auto pos = cursors[cursor].getInteractiveEnd();
    line = pos.line;
    column = pos.column;
}


//
//	TextEditor::getCursor
//

void TextEditor::getCursor(int& startLine, int& startColumn, int& endLine, int& endColumn, size_t cursor) const {
    cursor = std::min(cursor, cursors.size() - 1);
    auto start = cursors[cursor].getSelectionStart();
    auto end = cursors[cursor].getSelectionEnd();
    startLine = start.line;
    startColumn = start.column;
    endLine = end.line;
    endColumn = end.column;
}


//
//	TextEditor::getCursorText
//

std::string TextEditor::getCursorText(size_t cursor) const {
    cursor = std::min(cursor, cursors.size() - 1);
    return document.getSectionText(cursors[cursor].getSelectionStart(), cursors[cursor].getSelectionEnd());
}


//
//	TextEditor::GetWordAtScreenPos
//

std::string TextEditor::GetWordAtScreenPos(const ImVec2& screenPos) const {
    // convert screen position to local coordinates using the origin saved during last Render()
    auto local = screenPos - lastRenderOrigin;

    // convert to text coordinates
    Coordinate glyphCoordinate;
    Coordinate cursorCoordinate;
    document.normalizeCoordinate(local.y / glyphSize.y, (local.x - textOffset) / glyphSize.x, glyphCoordinate, cursorCoordinate);

    // Find word boundaries and extract text
    auto start = document.findWordStart(glyphCoordinate);
    auto end = document.findWordEnd(glyphCoordinate);
    return document.getSectionText(start, end);
}


//
//	TextEditor::makeCursorVisible
//

void TextEditor::makeCursorVisible() {
    ensureCursorIsVisible = true;
    scrollToLineNumber = -1;
}


//
//	TextEditor::scrollToLine
//

void TextEditor::scrollToLine(int line, Scroll alignment) {
    ensureCursorIsVisible = false;
    scrollToLineNumber = line;
    scrollToAlignment = alignment;
}


//
//	TextEditor::addMarker
//

void TextEditor::addMarker(int line, ImU32 lineNumberColor, ImU32 textColor, const std::string_view& lineNumberTooltip, const std::string_view& textTooltip) {
    if (line >= 0 && line < document.lineCount()) {
        markers.emplace_back(lineNumberColor, textColor, lineNumberTooltip, textTooltip);
        document[line].marker = markers.size();
    }
}


//
//	TextEditor::clearMarkers
//

void TextEditor::clearMarkers() {
    for (auto& line : document) {
        line.marker = 0;
    }

    markers.clear();
}


//
//	TextEditor::moveUp
//

void TextEditor::moveUp(int lines, bool select) {
    for (auto& cursor : cursors) {
        cursor.update(document.getUp(cursor.getInteractiveEnd(), lines), select);
    }

    makeCursorVisible();
}


//
//	TextEditor::moveDown
//

void TextEditor::moveDown(int lines, bool select) {
    for (auto& cursor : cursors) {
        cursor.update(document.getDown(cursor.getInteractiveEnd(), lines), select);
    }

    makeCursorVisible();
}


//
//	TextEditor::moveLeft
//

void TextEditor::moveLeft(bool select, bool wordMode) {
    for (auto& cursor : cursors) {
        cursor.update(document.getLeft(cursor.getInteractiveEnd(), wordMode), select);
    }

    makeCursorVisible();
}


//
//	TextEditor::moveRight
//

void TextEditor::moveRight(bool select, bool wordMode) {
    for (auto& cursor : cursors) {
        cursor.update(document.getRight(cursor.getInteractiveEnd(), wordMode), select);
    }

    makeCursorVisible();
}


//
//	TextEditor::moveToTop
//

void TextEditor::moveToTop(bool select) {
    cursors.clearAdditional();
    cursors.updateCurrentCursor(document.getTop(), select);
    makeCursorVisible();
}


//
//	TextEditor::moveToBottom
//

void TextEditor::moveToBottom(bool select) {
    cursors.clearAdditional();
    cursors.updateCurrentCursor(document.getBottom(), select);
    makeCursorVisible();
}


//
//	TextEditor::moveToStartOfLine
//

void TextEditor::moveToStartOfLine(bool select) {
    cursors.clearAdditional();
    cursors.updateCurrentCursor(document.getStartOfLine(cursors.getCurrent().getInteractiveEnd()), select);
    makeCursorVisible();
}


//
//	TextEditor::moveToEndOfLine
//

void TextEditor::moveToEndOfLine(bool select) {
    cursors.clearAdditional();
    cursors.updateCurrentCursor(document.getEndOfLine(cursors.getCurrent().getInteractiveEnd()), select);
    makeCursorVisible();
}


//
//	TextEditor::moveTo
//

void TextEditor::moveTo(Coordinate coordinate, bool select) {
    cursors.clearAdditional();
    cursors.updateCurrentCursor(coordinate, select);
    makeCursorVisible();
}


//
//	TextEditor::handleCharacter
//

void TextEditor::handleCharacter(ImWchar character) {
    auto transaction = startTransaction(false);

    auto opener = character;
    auto isPaired = !overwrite && completePairedGlyphs && CodePoint::isPairOpener(opener);
    auto closer = CodePoint::toPairCloser(opener);

    // ignore input if it was the closing character for a pair that was automatically inserted
    if (completePairCloser) {
        if (completePairCloser == character && completePairLocation == cursors.getCurrent().getSelectionEnd()) {
            completePairCloser = 0;
            moveRight(false, false);
            return;
        }

        completePairCloser = 0;
    }

    if (cursors.anyHasSelection() && isPaired) {
        // encapsulate the current selections with the requested pairs
        for (auto cursor = cursors.begin(); cursor < cursors.end(); cursor++) {
            if (cursor->hasSelection()) {
                auto start = cursor->getSelectionStart();
                auto end = cursor->getSelectionEnd();

                // insert the closing glyph
                char utf8[4];
                auto end1 = insertText(transaction, end, std::string_view(utf8, CodePoint::write(utf8, closer)));
                cursors.adjustForInsert(cursor, start, end1);

                // insert the opening glyph
                auto end2 = insertText(transaction, start, std::string_view(utf8, CodePoint::write(utf8, opener)));
                cursors.adjustForInsert(cursor, start, end2);

                // update old selection
                cursor->update(Coordinate(start.line, start.column + 1), Coordinate(end.line, end.column + 1));
            }
        }

    } else if (isPaired) {
        // insert the requested pair
        char utf8[8];
        auto size = CodePoint::write(utf8, opener);
        size += CodePoint::write(utf8 + size, closer);
        std::string_view pair(utf8, size);

        for (auto cursor = cursors.begin(); cursor < cursors.end(); cursor++) {
            auto start = cursor->getSelectionStart();
            auto end = insertText(transaction, start, pair);
            cursors.adjustForInsert(cursor, start, end);
            cursor->update(Coordinate(start.line, start.column + 1), false);
        }

        // remember the closer
        completePairCloser = closer;
        completePairLocation = cursors.getCurrent().getSelectionEnd();

    } else if (!overwrite && autoIndent && character == '\n') {
        // handle auto indent case
        autoIndentAllCursors(transaction);

    } else {
        // handle overwrite by deleting next glyph before insert
        if (overwrite) {
            for (auto cursor = cursors.begin(); cursor < cursors.end(); cursor++) {
                if (!cursor->hasSelection()) {
                    auto start = cursor->getSelectionStart();

                    if (start != document.getEndOfLine(start)) {
                        auto end = document.getRight(start);
                        deleteText(transaction, start, end);
                        cursors.adjustForDelete(cursor, start, end);
                    }
                }
            }
        }

        // just insert a regular character
        char utf8[4];
        insertTextIntoAllCursors(transaction, std::string_view(utf8, CodePoint::write(utf8, character)));
    }

    endTransaction(transaction);

    if (CodePoint::isWord(character)) {
        if (autocomplete.startTyping(cursors)) {
            makeCursorVisible();
        }
    }
}


//
//	TextEditor::handleBackspace
//

void TextEditor::handleBackspace(bool wordMode) {
    auto transaction = startTransaction(false);

    // remove selections or characters to the left of the cursor
    for (auto cursor = cursors.begin(); cursor < cursors.end(); cursor++) {
        auto start = cursor->hasSelection() ? cursor->getSelectionStart() : document.getLeft(cursor->getSelectionStart(), wordMode);
        auto end = cursor->getSelectionEnd();
        deleteText(transaction, start, end);
        cursor->update(start, false);
        cursors.adjustForDelete(cursor, start, end);
    }

    endTransaction(transaction);
}


//
//	TextEditor::handleDelete
//

void TextEditor::handleDelete(bool wordMode) {
    auto transaction = startTransaction(false);

    // remove selections or characters to the right of the cursor
    for (auto cursor = cursors.begin(); cursor < cursors.end(); cursor++) {
        auto start = cursor->getSelectionStart();
        auto end = cursor->hasSelection() ? cursor->getSelectionEnd() : document.getRight(cursor->getSelectionEnd(), wordMode);
        deleteText(transaction, start, end);
        cursor->update(start, false);
        cursors.adjustForDelete(cursor, start, end);
    }

    endTransaction(transaction);
}


//
//	TextEditor::removeSelectedLines
//

void TextEditor::removeSelectedLines() {
    auto transaction = startTransaction();

    for (auto cursor = cursors.begin(); cursor < cursors.end(); cursor++) {
        auto start = document.getStartOfLine(cursor->getSelectionStart());
        auto end = cursor->getSelectionEnd();
        end = (end.column == 0) ? end : document.getNextLine(end);
        deleteText(transaction, start, end);
        cursor->update(start, false);
        cursors.adjustForDelete(cursor, start, end);
    }

    endTransaction(transaction);
}


//
//	TextEditor::insertLineAbove
//

void TextEditor::insertLineAbove() {
    auto transaction = startTransaction();

    for (auto cursor = cursors.begin(); cursor < cursors.end(); cursor++) {
        auto start = document.getStartOfLine(cursor->getSelectionStart());
        auto end = insertText(transaction, start, "\n");
        cursor->update(start, false);
        cursors.adjustForInsert(cursor, start, end);
    }

    endTransaction(transaction);
}


//
//	TextEditor::insertLineBelow
//

void TextEditor::insertLineBelow() {
    auto transaction = startTransaction();

    for (auto cursor = cursors.begin(); cursor < cursors.end(); cursor++) {
        auto start = cursor->getSelectionEnd();
        start = (start.column == 0) ? start : document.getNextLine(start);
        auto end = insertText(transaction, start, "\n");
        cursor->update(start, false);
        cursors.adjustForInsert(cursor, start, end);
    }

    endTransaction(transaction);
}


//
//	TextEditor::indentLines
//

void TextEditor::indentLines() {
    auto transaction = startTransaction();

    // process all cursors
    for (auto cursor = cursors.begin(); cursor < cursors.end(); cursor++) {
        auto cursorStart = cursor->getSelectionStart();
        auto cursorEnd = cursor->getSelectionEnd();

        // process all lines in this cursor
        for (auto line = cursorStart.line; line <= cursorEnd.line; line++) {
            if (Coordinate(line, 0) != cursorEnd && document[line].size()) {
                auto insertStart = Coordinate(line, 0);
                auto insertEnd = insertText(transaction, insertStart, "\t");
                cursors.adjustForInsert(cursor, insertStart, insertEnd);
            }
        }

        auto tabSize = document.getTabSize();
        cursorStart.column += cursorStart.column ? tabSize : 0;
        cursorEnd.column += cursorEnd.column ? tabSize : 0;
        cursor->update(cursorStart, cursorEnd);
    }

    endTransaction(transaction);
}


//
//	TextEditor::deindentLines
//

void TextEditor::deindentLines() {
    auto transaction = startTransaction();

    // process all cursors
    for (auto cursor = cursors.begin(); cursor < cursors.end(); cursor++) {
        auto cursorStart = cursor->getSelectionStart();
        auto cursorEnd = cursor->getSelectionEnd();
        auto tabSize = document.getTabSize();

        for (auto line = cursorStart.line; line <= cursorEnd.line; line++) {
            // determine how many whitespaces are available at the start with a max of 4 columns
            int column = 0;
            size_t index = 0;

            while (column < 4 && index < document[line].size() && std::isblank(document[line][index].codepoint)) {
                column += document[line][index].codepoint == '\t' ? tabSize - (column % tabSize) : 1;
                index++;
            }

            // delete that whitespace (if required)
            Coordinate deleteStart{line, 0};
            Coordinate deleteEnd{line, document.getColumn(line, index)};

            if (deleteEnd != deleteStart) {
                deleteText(transaction, deleteStart, deleteEnd);
                cursors.adjustForDelete(cursor, deleteStart, deleteEnd);
            }
        }
    }

    endTransaction(transaction);
}


//
//	Widget::moveUpLines
//

void TextEditor::moveUpLines() {
    // don't move up if first line is in one of the cursors
    if (cursors[0].getSelectionStart().line != 0) {
        auto transaction = startTransaction();

        for (auto cursor = cursors.begin(); cursor < cursors.end(); cursor++) {
            auto start = cursor->getSelectionStart();
            auto end = cursor->getSelectionEnd();

            // delete existing lines
            auto deleteStart = document.getStartOfLine(start);
            auto deleteEnd = (end.column == 0) ? end : document.getNextLine(end);;
            auto text = document.getSectionText(deleteStart, deleteEnd);
            deleteText(transaction, deleteStart, deleteEnd);
            cursors.adjustForDelete(cursor, deleteStart, deleteEnd);

            // insert text one line up
            auto insertStart = document.getUp(deleteStart);
            auto insertEnd = insertText(transaction, insertStart, text);
            cursors.adjustForInsert(cursor, insertStart, insertEnd);

            // update cursor
            cursor->update(start - Coordinate(1, 0), end - Coordinate(1, 0));
        }

        endTransaction(transaction);
    }
}


//
//	TextEditor::moveDownLines
//

void TextEditor::moveDownLines() {
    // don't move up if last line is in one of the cursors
    if (!document.isLastLine(cursors[cursors.size() - 1].getSelectionStart().line)) {
        auto transaction = startTransaction();

        for (auto cursor = cursors.begin(); cursor < cursors.end(); cursor++) {
            auto start = cursor->getSelectionStart();
            auto end = cursor->getSelectionEnd();

            // delete existing lines
            auto deleteStart = document.getStartOfLine(start);
            auto deleteEnd = (end.column == 0) ? end : document.getNextLine(end);;
            auto text = document.getSectionText(deleteStart, deleteEnd);
            deleteText(transaction, deleteStart, deleteEnd);
            cursors.adjustForDelete(cursor, deleteStart, deleteEnd);

            // insert text one line down
            auto insertStart = document.getDown(deleteStart);
            auto insertEnd = insertText(transaction, insertStart, text);
            cursors.adjustForInsert(cursor, insertStart, insertEnd);

            // update cursor
            cursor->update(start + Coordinate(1, 0), end + Coordinate(1, 0));
        }

        endTransaction(transaction);
    }
}


//
//	TextEditor::toggleComments
//

void TextEditor::toggleComments() {
    auto transaction = startTransaction();
    auto comment = language->singleLineComment;

    // process all cursors
    for (auto cursor = cursors.begin(); cursor < cursors.end(); cursor++) {
        auto cursorStart = cursor->getSelectionStart();
        auto cursorEnd = cursor->getSelectionEnd();

        // process all lines in this cursor
        for (auto line = cursorStart.line; line <= cursorEnd.line; line++) {
            if (Coordinate(line, 0) != cursorEnd && document[line].size()) {
                // see if line starts with a comment (after possible leading whitespaces)
                size_t start = 0;
                size_t i = 0;

                while (start < document[line].size() && CodePoint::isWhiteSpace(document[line][start].codepoint)) {
                    start++;
                }

                while (start + i < document[line].size() && i < comment.size() && document[line][start + i].codepoint == comment[i]) {
                    i++;
                }

                if (i == comment.size()) {
                    auto deleteStart = Coordinate(line, document.getColumn(line, start));
                    auto deleteEnd = Coordinate(line, document.getColumn(line, start + static_cast<int>(comment.size()) + 1));
                    deleteText(transaction, deleteStart, deleteEnd);
                    cursors.adjustForDelete(cursor, deleteStart, deleteEnd);

                } else {
                    auto insertStart = Coordinate(line, document.getColumn(line, start));
                    auto insertEnd = insertText(transaction, insertStart, comment + " ");
                    cursors.adjustForInsert(cursor, insertStart, insertEnd);
                }
            }
        }
    }

    endTransaction(transaction);
}


//
//	TextEditor::filterSelections
//

void TextEditor::filterSelections(std::function<std::string(std::string_view)> filter) {
    auto transaction = startTransaction();

    // process all cursors
    for (auto cursor = cursors.begin(); cursor < cursors.end(); cursor++) {
        auto start = cursor->getSelectionStart();
        auto end = cursor->getSelectionEnd();

        // process all lines in this cursor
        for (auto line = start.line; line <= end.line; line++) {
            if (Coordinate(line, 0) != end && document[line].size()) {
                // get original text and run it through filter
                auto before = document.getSectionText(start, end);
                std::string after = filter(before);

                // update selection if anything changed
                if (after != before) {
                    deleteText(transaction, start, end);
                    cursors.adjustForDelete(cursor, start, end);
                    auto newEnd = insertText(transaction, start, after);
                    cursor->update(start, newEnd);
                    cursors.adjustForInsert(cursor, start, newEnd);
                }
            }
        }
    }

    endTransaction(transaction);
}


//
//	TextEditor::selectionToLowerCase
//

void TextEditor::selectionToLowerCase() {
    FilterSelections([](const std::string_view& text) {
        std::string result;
        auto end = text.end();
        auto i = text.begin();
        char utf8[4];

        while (i < end) {
            ImWchar codepoint;
            i = CodePoint::read(i, end, &codepoint);
            result.append(utf8, CodePoint::write(utf8, CodePoint::toLower(codepoint)));
        }

        return result;
    });
}


//
//	TextEditor::selectionToUpperCase
//

void TextEditor::selectionToUpperCase() {
    FilterSelections([](const std::string_view& text) {
        std::string result;
        auto end = text.end();
        auto i = text.begin();
        char utf8[4];

        while (i < end) {
            ImWchar codepoint;
            i = CodePoint::read(i, end, &codepoint);
            result.append(utf8, CodePoint::write(utf8, CodePoint::toUpper(codepoint)));
        }

        return result;
    });
}


//
//	TextEditor::stripTrailingWhitespaces
//

void TextEditor::stripTrailingWhitespaces() {
    auto transaction = startTransaction();

    // process all the lines
    for (int i = 0; i < document.lineCount(); i++) {
        auto& line = document[i];
        size_t lineSize = line.size();
        size_t whitespace = std::numeric_limits<std::size_t>::max();
        bool done = false;

        // look for first non-whitespace glyph at the end of the line
        if (lineSize) {
            for (auto index = lineSize - 1; !done; index--) {
                if (CodePoint::isWhiteSpace(line[index].codepoint)) {
                    whitespace = index;

                    if (index == 0) {
                        done = true;
                    }

                } else {
                    done = true;
                }
            }
        }

        // remove whitespaces (if required)
        if (whitespace != std::numeric_limits<std::size_t>::max()) {
            auto start = Coordinate(i, document.getColumn(line, whitespace));
            auto end = Coordinate(i, document.getColumn(line, lineSize));
            deleteText(transaction, start, end);
        }
    }

    // update cursor if transaction wasn't empty
    if (endTransaction(transaction)) {
        cursors.setCursor(document.normalizeCoordinate(cursors.getCurrent().getSelectionEnd()));
    }
}


//
//	TextEditor::filterLines
//

void TextEditor::filterLines(std::function<std::string(std::string_view)> filter) {
    auto transaction = startTransaction();

    // process all the lines
    for (int i = 0; i < document.lineCount(); i++) {
        // get original text and run it through filter
        auto before = document.getLineText(i);
        std::string after = filter(before);

        // update line if anything changed
        if (after != before) {
            auto start = Coordinate(i, 0);
            auto end = document.getEndOfLine(start);
            deleteText(transaction, start, end);
            insertText(transaction, start, after);
        }
    }

    // update cursor if transaction wasn't empty
    if (endTransaction(transaction)) {
        cursors.setCursor(document.normalizeCoordinate(cursors.getCurrent().getSelectionEnd()));
    }
}


//
//	TextEditor::tabsToSpaces
//

void TextEditor::tabsToSpaces() {
    filterLines([this](const std::string_view& input) {
        auto tabSize = static_cast<size_t>(document.getTabSize());
        std::string output;
        auto end = input.end();
        auto i = input.begin();
        size_t pos = 0;

        while (i < end) {
            char utf8[4];
            ImWchar codepoint;
            i = CodePoint::read(i, end, &codepoint);

            if (codepoint == '\t') {
                auto spaces = tabSize - (pos % tabSize);
                output.append(spaces, ' ');
                pos += spaces;

            } else {
                output.append(utf8, CodePoint::write(utf8, codepoint));
                pos++;
            }
        }

        return output;
    });
}


//
//	TextEditor::spacesToTabs
//

void TextEditor::spacesToTabs() {
    FilterLines([this](const std::string_view& input) {
        auto tabSize = static_cast<size_t>(document.getTabSize());
        std::string output;
        auto end = input.end();
        auto i = input.begin();
        size_t pos = 0;
        size_t spaces = 0;

        while (i < end) {
            char utf8[4];
            ImWchar codepoint;
            i = CodePoint::read(i, end, &codepoint);

            if (codepoint == ' ') {
                spaces++;

            } else {
                while (spaces) {
                    auto spacesUntilNextTab = tabSize - (pos % tabSize);

                    if (spacesUntilNextTab == 1) {
                        output += ' ';
    pos++;
    spaces--;

                    } else if (spaces >= spacesUntilNextTab) {
                        output += '\t';
    pos += spacesUntilNextTab;
    spaces -= spacesUntilNextTab;

                    } else if (codepoint != '\t')
                        while (spaces) {
                            output += ' ';
    pos++;
    spaces--;
                        }

                        else {
                            spaces = 0;
                        }
                }

                if (codepoint == '\t') {
                    output += '\t';
    pos += tabSize - (pos % tabSize);

                } else {
                    output.append(utf8, CodePoint::write(utf8, codepoint));
                    pos++;
                }
            }
        }

        return output;
    });
}


//
//	TextEditor::startTransaction
//

std::shared_ptr<TextEditor::Transaction> TextEditor::startTransaction(bool cancelsAutoComplete) {
    if (cancelsAutoComplete) {
        autocomplete.cancel();
    }

    std::shared_ptr<Transaction> transaction = Transactions::create();
    transaction->setBeforeState(cursors);
    return transaction;
}


//
//	TextEditor::endTransaction
//

bool TextEditor::endTransaction(std::shared_ptr<Transaction> transaction) {
    if (transaction->actions() > 0) {
        cursors.update();
        transaction->setAfterState(cursors);
        transactions.add(transaction);
        std::vector<Change> changes;

        if (transactionCallback) {
            for (auto& action : *transaction) {
                auto& change = changes.emplace_back();
                change.insert = action.type == Action::Type::insertText;

                change.startLine = static_cast<int>(action.start.line);
                change.startColumn = static_cast<int>(action.start.column);
                change.startIndex = static_cast<int>(document.getIndex(action.start));

                change.startLine = static_cast<int>(action.end.line);
                change.startColumn = static_cast<int>(action.end.column);
                change.startIndex = static_cast<int>(document.getIndex(action.end));

                change.text = action.text;
            }

            transactionCallback(changes);
        }

        return true;

    } else {
        return false;
    }
}


//
//	TextEditor::insertTextIntoAllCursors
//

void TextEditor::insertTextIntoAllCursors(std::shared_ptr<Transaction> transaction, const std::string_view& text) {
    // delete any selection content first
    deleteTextFromAllCursors(transaction);

    // insert the text
    for (auto cursor = cursors.begin(); cursor < cursors.end(); cursor++) {
        auto start = cursor->getSelectionStart();
        auto end = insertText(transaction, start, text);
        cursor->update(end, false);
        cursors.adjustForInsert(cursor, start, end);
    }
}


//
//	TextEditor::deleteTextFromAllCursors
//

void TextEditor::deleteTextFromAllCursors(std::shared_ptr<Transaction> transaction) {
    for (auto cursor = cursors.begin(); cursor < cursors.end(); cursor++) {
        if (cursor->hasSelection()) {
            auto start = cursor->getSelectionStart();
            auto end = cursor->getSelectionEnd();
            deleteText(transaction, start, end);
            cursors.adjustForDelete(cursor, start, end);
        }
    }
}


//
//	TextEditor::autoIndentAllCursors
//

void TextEditor::autoIndentAllCursors(std::shared_ptr<Transaction> transaction) {
    for (auto cursor = cursors.begin(); cursor < cursors.end(); cursor++) {
        auto start = cursor->getSelectionStart();

        // delete any selections
        if (cursor->hasSelection()) {
            auto end = cursor->getSelectionEnd();
            deleteText(transaction, start, end);
            cursors.adjustForDelete(cursor, start, end);
        }

        // get previous and next character
        auto index = document.getIndex(start);
        auto& line = document[start.line];
        ImWchar previousChar = index > 0 ? line[index - 1].codepoint : 0;
        ImWchar nextChar = index < line.size() ? line[index].codepoint : 0;

        // remove extra whitespaces if required
        if (CodePoint::isWhiteSpace(nextChar)) {
            while (index < line.size() && CodePoint::isWhiteSpace(line[index].codepoint)) {
                index++;
            }

            auto end = Coordinate(start.line, document.getColumn(start.line, index));
            deleteText(transaction, start, end);
            cursors.adjustForDelete(cursor, start, end);
        }

        // determine whitespace at start of current line
        std::string whitespace;

        for (size_t i = 0; i < line.size() && CodePoint::isWhiteSpace(line[i].codepoint); i++) {
            char utf8[4];
            whitespace.append(utf8, CodePoint::write(utf8, line[i].codepoint));
        }

        // determine text to insert
        std::string insert = "\n" + whitespace;
        auto newCursorIndex = static_cast<int>(whitespace.size());

        // handle special cases
        if (previousChar == CodePoint::openCurlyBracket || previousChar == CodePoint::openSquareBracket) {
            // add to an existing block
            insert += "\t";
            newCursorIndex++;

            if ((previousChar == CodePoint::openCurlyBracket && nextChar == CodePoint::closeCurlyBracket) ||
                (previousChar == CodePoint::openSquareBracket && nextChar == CodePoint::closeSquareBracket)) {

                // open a new block
                insert += "\n" + whitespace;
                }
        }

        // insert new text
        auto end = insertText(transaction, start, insert);
        cursors.adjustForInsert(cursor, start, end);

        // set new cursor location
        cursor->update(Coordinate(start.line + 1, document.getColumn(start.line + 1, newCursorIndex)), false);
    }
}


//
//	TextEditor::insertText
//

TextEditor::Coordinate TextEditor::insertText(std::shared_ptr<Transaction> transaction, Coordinate start, const std::string_view& text) {
    // update document, add transaction and return coordinate of end of insert
    // this function does not touch the cursors
    auto end = document.insertText(start, text);
    transaction->addInsert(start, end, text);
    makeCursorVisible();
    return end;
}


//
//	TextEditor::deleteText
//

void TextEditor::deleteText(std::shared_ptr<Transaction> transaction, Coordinate start, Coordinate end) {
    // update document and add transaction
    // this function does not touch the cursors
    auto text = document.getSectionText(start, end);
    document.deleteText(start, end);
    transaction->addDelete(start, end, text);
    makeCursorVisible();
}


//
//	TextEditor::updatePalette
//

void TextEditor::updatePalette() {
    // Update palette with the current alpha from style
    paletteAlpha = ImGui::GetStyle().Alpha;

    for (size_t i = 0; i < static_cast<size_t>(Color::count); i++) {
        auto color = ImGui::ColorConvertU32ToFloat4(paletteBase[i]);
        color.w *= paletteAlpha;
        palette[i] = ImGui::ColorConvertFloat4ToU32(color);
    }
}


//
//	Color palettes
//

const TextEditor::Palette& TextEditor::GetDarkPalette() {
    const static Palette p = {{
        IM_COL32(224, 224, 224, 255),	// text
        IM_COL32(197, 134, 192, 255),	// keyword
        IM_COL32( 90, 179, 155, 255),	// declaration
        IM_COL32(181, 206, 168, 255),	// number
        IM_COL32(206, 145, 120, 255),	// string
        IM_COL32(255, 255, 153, 255),	// punctuation
        IM_COL32( 64, 192, 128, 255),	// preprocessor
        IM_COL32(156, 220, 254, 255),	// identifier
        IM_COL32( 79, 193, 255, 255),	// known identifier
        IM_COL32(106, 153,  85, 255),	// comment
        IM_COL32( 30,  30,  30, 255),	// background
        IM_COL32(224, 224, 224, 255),	// cursor
        IM_COL32( 32,  96, 160, 255),	// selection
        IM_COL32( 80,  80,  80, 255),	// whitespace
        IM_COL32( 70,  70,  70, 255),	// matchingBracketBackground
        IM_COL32(140, 140, 140, 255),	// matchingBracketActive
        IM_COL32(246, 222,  36, 255),	// matchingBracketLevel1
        IM_COL32( 66, 120, 198, 255),	// matchingBracketLevel2
        IM_COL32(213,  96, 213, 255),	// matchingBracketLevel3
        IM_COL32(198,   8,  32, 255),	// matchingBracketError
        IM_COL32(128, 128, 144, 255),	// line number
        IM_COL32(224, 224, 240, 255),	// current line number
    }};

    return p;
}

const TextEditor::Palette& TextEditor::GetLightPalette()
{
    const static Palette p = {{
        IM_COL32( 64,  64,  64, 255),	// text
        IM_COL32( 170,  0, 220, 255),	// keyword
        IM_COL32( 65,   0, 255, 255),	// declaration
        IM_COL32( 40, 140,  90, 255),	// number
        IM_COL32(160,  32,  32, 255),	// string
        IM_COL32(  0,   0,   0, 255),	// punctuation
        IM_COL32( 96,  96,  64, 255),	// preprocessor
        IM_COL32( 64,  64,  64, 255),	// identifier
        IM_COL32( 16,  96,  96, 255),	// known identifier
        IM_COL32( 35, 135,   5, 255),	// comment
        IM_COL32(255, 255, 255, 255),	// background
        IM_COL32(  0,   0,   0, 255),	// cursor
        IM_COL32(  0,   0,  96,  64),	// selection
        IM_COL32(144, 144, 144, 144),	// whitespace
        IM_COL32(180, 180, 180, 144),	// matchingBracketBackground
        IM_COL32( 72,  72,  72, 255),	// matchingBracketActive
        IM_COL32( 70,   0, 250, 255),	// matchingBracketLevel1
        IM_COL32( 80, 160,  70, 255),	// matchingBracketLevel2
        IM_COL32(120,  60, 25, 255),	// matchingBracketLevel3
        IM_COL32(198,   8,  32, 255),	// matchingBracketError
        IM_COL32(  0,  80,  80, 255),	// line number
        IM_COL32(  0,   0,   0, 255),	// current line number
    }};

    return p;
}

TextEditor::Palette TextEditor::defaultPalette = TextEditor::GetDarkPalette();


//
//	TextEditor::Cursor::adjustCoordinateForInsert
//

TextEditor::Coordinate TextEditor::Cursor::adjustCoordinateForInsert(Coordinate coordinate, Coordinate insertStart, Coordinate insertEnd) {
    if (coordinate.line == insertStart.line) {
        coordinate.column += insertEnd.column - insertStart.column;
    }

    coordinate.line += insertEnd.line - insertStart.line;
    return coordinate;
}


//
//	TextEditor::Cursor::adjustForInsert
//

void TextEditor::Cursor::adjustForInsert(Coordinate insertStart, Coordinate insertEnd) {
    start = adjustCoordinateForInsert(start, insertStart, insertEnd);
    end = adjustCoordinateForInsert(end, insertStart, insertEnd);
}


//
//	TextEditor::Cursor::adjustCoordinateForDelete
//

TextEditor::Coordinate TextEditor::Cursor::adjustCoordinateForDelete(Coordinate coordinate, Coordinate deleteStart, Coordinate deleteEnd) {
    if (deleteStart.line == deleteEnd.line) {
        if (coordinate.line == deleteEnd.line) {
            coordinate.column -= deleteEnd.column - deleteStart.column;
        }

    } else {
        coordinate.line -= deleteEnd.line - deleteStart.line;

        if (coordinate.line == deleteEnd.line) {
            coordinate.column -= deleteEnd.column;
        }
    }

    return coordinate;
}


//
//	TextEditor::Cursor::adjustForDelete
//

void TextEditor::Cursor::adjustForDelete(Coordinate deleteStart, Coordinate deleteEnd) {
    start = adjustCoordinateForDelete(start, deleteStart, deleteEnd);
    end = adjustCoordinateForDelete(end, deleteStart, deleteEnd);
}


//
//	TextEditor::Cursors::reset
//

void TextEditor::Cursors::reset() {
    clear();
    main = 0;
    current = 0;
}


//
//	TextEditor::Cursors::setCursor
//

void TextEditor::Cursors::setCursor(Coordinate cursorStart, Coordinate cursorEnd) {
    reset();
    emplace_back(cursorStart, cursorEnd);
    front().setMain(true);
    front().setCurrent(true);
}


//
//	TextEditor::Cursors::addCursor
//

void TextEditor::Cursors::addCursor(Coordinate start, Coordinate end) {
    at(current).setCurrent(false);
    emplace_back(start, end);
    back().setCurrent(true);
    current = size() - 1;
}


//
//	TextEditor::Cursors::anyHasSelection
//

bool TextEditor::Cursors::anyHasSelection() const {
    for (auto cursor = begin(); cursor < end(); cursor++) {
        if (cursor->hasSelection()) {
            return true;
        }
    }

    return false;
}


//
//	TextEditor::Cursors::allHaveSelection
//

bool TextEditor::Cursors::allHaveSelection() const {
    for (auto cursor = begin(); cursor < end(); cursor++) {
        if (!cursor->hasSelection()) {
            return false;
        }
    }

    return true;
}


//
//	TextEditor::Cursors::anyHasUpdate
//

bool TextEditor::Cursors::anyHasUpdate() const {
    for (auto cursor = begin(); cursor < end(); cursor++) {
        if (cursor->isUpdated()) {
            return true;
        }
    }

    return false;
}


//
//	TextEditor::Cursors::clearAll
//

void TextEditor::Cursors::clearAll() {
    reset();
    emplace_back(Coordinate(0, 0));
    front().setMain(true);
    front().setCurrent(true);
}


//
//	TextEditor::Cursors::clearAdditional
//

void TextEditor::Cursors::clearAdditional(bool reset) {
    for (auto cursor = begin(); cursor < end();) {
        if (cursor->isMain()) {
            cursor++;

        } else {
            cursor = erase(cursor);
        }
    }

    main = 0;
    current = 0;
    front().setCurrent(true);

    if (reset) {
        front().resetToEnd();
    }
}


//
//	TextEditor::Cursors::clearUpdated
//

void TextEditor::Cursors::clearUpdated() {
    for (auto cursor = begin(); cursor < end(); cursor++) {
        cursor->setUpdated(false);
    }
}


//
//	TextEditor::Cursors::update
//

void TextEditor::Cursors::update() {
    // reset update flags
    clearUpdated();

    //  only sort and potential merge when we have multiple cursors
    if (hasMultiple()) {
        // sort cursors
        std::sort(begin(), end(), [](Cursor& a, Cursor& b) {
            return a.getSelectionStart() < b.getSelectionStart();
        });

        // merge cursors
        for (auto cursor = rbegin(); cursor < rend() - 1;) {
            auto previous = cursor + 1;

            if (previous->getSelectionEnd() >= cursor->getSelectionEnd()) {
                if (cursor->isMain()) {
                    previous->setMain(true);
                }

                if (cursor->isCurrent()) {
                    previous->setCurrent(true);
                }

                erase((++cursor).base());

            } else if (previous->getSelectionEnd() > cursor->getSelectionStart()) {
                if (cursor->getInteractiveEnd() < cursor->getInteractiveStart()) {
                    previous->update(cursor->getSelectionEnd(), previous->getSelectionStart());

                } else {
                    previous->update(previous->getSelectionStart(), cursor->getSelectionEnd());
                }

                if (cursor->isMain()) {
                    previous->setMain(true);
                }

                if (cursor->isCurrent()) {
                    previous->setCurrent(true);
                }

                erase((++cursor).base());

            } else {
                cursor++;
            }
        }

        // find main and current cursor
        for (size_t c = 0; c < size(); c++) {
            if (at(c).isMain()) {
                main = c;

            } else if (at(c).isCurrent()) {
                current = c;
            }
        }
    }
}


//
//	TextEditor::Cursors::adjustForInsert
//

void TextEditor::Cursors::adjustForInsert(iterator start, Coordinate insertStart, Coordinate insertEnd) {
    for (auto cursor = start + 1; cursor < end(); cursor++) {
        cursor->adjustForInsert(insertStart, insertEnd);
    }
}


//
//	TextEditor::Cursors::adjustForDelete
//

void TextEditor::Cursors::adjustForDelete(iterator start, Coordinate deleteStart, Coordinate deleteEnd) {
    for (auto cursor = start + 1; cursor < end(); cursor++) {
        cursor->adjustForDelete(deleteStart, deleteEnd);
    }
}


//
//	TextEditor::Document::setText
//

void TextEditor::Document::setText(const std::string_view& text) {
    // reset document
    clearDocument();
    appendLine();
    updated = true;

    // process UTF-8 and generate lines of glyphs
    auto end = text.end();
    auto i = CodePoint::skipBOM(text.begin(), end);

    while (i < end) {
        ImWchar character;
        i = CodePoint::read(i, end, &character);

        if (character == '\n') {
            appendLine();

        } else if (insertSpacesOnTabs && character == '\t') {
            auto spaces = ((back().size() / tabSize) + 1) * tabSize - back().size();

            for (size_t s = 0; s < spaces; s++) {
                back().emplace_back(Glyph(' ', Color::text));
            }

        } else if (character != '\r') {
            back().emplace_back(Glyph(character, Color::text));
        }
    }

    // update maximum column counts
    updateMaximumColumn(0, lineCount() - 1);
}


//
//	TextEditor::Document::setText
//

void TextEditor::Document::setText(const std::vector<std::string_view>& text) {
    // reset document
    clearDocument();
    updated = true;

    if (text.size()) {
        // process input UTF-8 and generate lines of glyphs
        for (auto& line : text) {
            appendLine();
            auto i = line.begin();
            auto end = line.end();

            while (i < end) {
                ImWchar character;
                i = CodePoint::read(i, end, &character);

                if (insertSpacesOnTabs && character == '\t') {
                    auto spaces = ((back().size() / tabSize) + 1) * tabSize - back().size();

                    for (size_t s = 0; s < spaces; s++) {
                        back().emplace_back(Glyph(' ', Color::text));
                    }

                } else if (character != '\r') {
                    back().emplace_back(Glyph(character, Color::text));
                }
            }
        }

    } else {
        appendLine();
    }

    // update maximum column counts
    updateMaximumColumn(0, lineCount() - 1);
}


//
//	TextEditor::Document::insertText
//

TextEditor::Coordinate TextEditor::Document::insertText(Coordinate start, const std::string_view& text) {
    auto line = begin() + start.line;
    auto index = getIndex(start);
    auto lineNo = start.line;

    // process input as UTF-8
    auto endOfText = text.end();
    auto i = text.begin();

    // process all codepoints
    while (i < endOfText) {
        ImWchar character;
        i = CodePoint::read(i, endOfText, &character);

        if (character == '\n') {
            // split line
            insertLine(lineNo + 1);
            line = begin() + lineNo;
            auto nextLine = begin() + ++lineNo;

            for (auto j = line->begin() + index; j < line->end(); j++) {
                nextLine->push_back(*j);
            }

            line->erase(line->begin() + index, line->end());
            line = nextLine;
            index = 0;

        } else if (insertSpacesOnTabs && character == '\t') {
            auto spaces = ((index / tabSize) + 1) * tabSize - index;

            for (size_t s = 0; s < spaces; s++) {
                line->insert(line->begin() + (index++), Glyph(' ', Color::text));
            }

        } else if (character != '\r') {
            // insert next glyph
            line->insert(line->begin() + (index++), Glyph(character, Color::text));
        }
    }

    // determine end of insert
    auto end = Coordinate(lineNo, getColumn(static_cast<int>(line - begin()), index));

    // mark affected lines for colorization
    for (auto j = start.line; j <= end.line; j++) {
        at(j).colorize = true;
    }

    // update maximum column counts
    updateMaximumColumn(start.line, end.line);

    updated = true;
    return end;
}


//
//	TextEditor::Document::deleteText
//

void TextEditor::Document::deleteText(Coordinate start, Coordinate end) {
    auto& startLine = at(start.line);
    auto& endLine = at(end.line);
    auto startIndex = getIndex(start);
    auto endIndex = getIndex(end);

    // see if start and end are on the same line
    if (start.line == end.line) {
        startLine.erase(startLine.begin() + startIndex, startLine.begin() + endIndex);

        // start and end are on different lines
    } else {
        // remove end of first line
        startLine.erase(startLine.begin() + startIndex, startLine.end());

        // remove start of last line
        endLine.erase(endLine.begin(), endLine.begin() + endIndex);

        // join lines
        startLine.insert(startLine.end(), endLine.begin(), endLine.end());

        // delete lines
        deleteLines(start.line + 1, end.line);
    }

    // remove marker
    startLine.marker = 0;

    // mark affected lines for colorization
    auto last = (start.line == lineCount() - 1) ? start.line : start.line + 1;

    for (auto line = start.line; line <= last; line++) {
        at(line).colorize = true;
    }

    // update maximum column counts
    updateMaximumColumn(start.line, end.line);
    updated = true;
}


//
//	TextEditor::Document::getText
//

std::string TextEditor::Document::getText() const {
    // process all glyphs and generate UTF-8 output
    std::string text;
    char utf8[4];

    for (auto line = begin(); line < end(); line++) {
        for (auto glyph = line->begin(); glyph < line->end(); glyph++) {
            text.append(std::string_view(utf8, CodePoint::write(utf8, glyph->codepoint)));
        }

        if (line < end() - 1) {
            text += "\n";
        }
    }

    return text;
}


//
//	TextEditor::Document::getLineText
//

std::string TextEditor::Document::getLineText(int line) const {
    return getSectionText(Coordinate(line, 0), Coordinate(line, at(line).maxColumn));
}


//
//	TextEditor::Document::getSectionText
//

std::string TextEditor::Document::getSectionText(Coordinate start, Coordinate end) const {
    std::string section;

    auto lineNo = start.line;
    auto index = getIndex(start);
    auto endIndex = getIndex(end);
    char utf8[4];

    while (lineNo < end.line || index < endIndex) {
        auto& line = at(lineNo);

        if (index < line.size()) {
            section.append(std::string_view(utf8, CodePoint::write(utf8, line[index].codepoint)));
            index++;

        } else {
            section += '\n';
            lineNo++;
            index = 0;
        }
    }

    return section;
}


//
//	TextEditor::Document::getCodePoint
//

ImWchar TextEditor::Document::getCodePoint(Coordinate location) const {
    auto index = getIndex(location);

    if (index < at(location.line).size()) {
        return at(location.line)[index].codepoint;

    } else {
        return IM_UNICODE_CODEPOINT_INVALID;
    }
}

//
//	TextEditor::Document::getColor
//

TextEditor::Color TextEditor::Document::getColor(Coordinate location)  const {
    auto index = getIndex(location);

    if (index < at(location.line).size()) {
        return at(location.line)[index].color;

    } else {
        return Color::text;
    }
}


//
//	TextEditor::Document::updateMaximumColumn
//

void TextEditor::Document::updateMaximumColumn(int first, int last) {
    // process specified lines
    for (auto line = begin() + first; line <= begin() + last; line++) {
        // determine the maximum column number for this line
        int column = 0;

        for (auto glyph = line->begin(); glyph < line->end(); glyph++) {
            column = (glyph->codepoint == '\t') ? ((column / tabSize) + 1) * tabSize : column + 1;
        }

        line->maxColumn = column;
    }

    // determine maximum column number in document
    maxColumn = 0;

    for (auto line = begin(); line < end(); line++) {
        maxColumn = std::max(maxColumn, line->maxColumn);
    }
}


//
//	TextEditor::Document::getIndex
//

size_t TextEditor::Document::getIndex(const Line& line, int column) const {
    // convert a column reference to a glyph index for a specified line (taking tabs into account)
    auto end = line.end();
    size_t index = 0;
    auto leftCol = 0;
    auto rightCol = 0;

    for (auto glyph = line.begin(); rightCol < column && glyph < end; glyph++) {
        leftCol = rightCol;
        rightCol = (glyph->codepoint == '\t') ? ((rightCol / tabSize) + 1) * tabSize : rightCol + 1;
        index++;
    }

    if (rightCol - leftCol <= 1) {
        return index;

    } else {
        auto leftDiff = column - leftCol;
        auto rightDiff = rightCol - column;
        return leftDiff <= rightDiff ? index - 1 : index;
    }
}


//
//	TextEditor::Document::getColumn
//

int TextEditor::Document::getColumn(const Line& line, size_t index) const {
    // convert a glyph index to a column reference for the specified line (taking tabs into account)
    auto end = line.begin() + index;
    int column = 0;

    for (auto glyph = line.begin(); glyph < end; glyph++) {
        column = (glyph->codepoint == '\t') ? ((column / tabSize) + 1) * tabSize : column + 1;
    }

    return column;
}


//
//	TextEditor::Document::getUp
//

TextEditor::Coordinate TextEditor::Document::getUp(Coordinate from, int lines) const {
    return normalizeCoordinate(Coordinate(from.line - lines, from.column));
}


//
//	TextEditor::Document::getDown
//

TextEditor::Coordinate TextEditor::Document::getDown(Coordinate from, int lines) const {
    return normalizeCoordinate(Coordinate(from.line + lines, from.column));
}


//
//	TextEditor::Document::getLeft
//

TextEditor::Coordinate TextEditor::Document::getLeft(Coordinate from, bool wordMode) const {
    if (wordMode) {
        // first move left by one glyph
        from = getLeft(from);

        // now skip all whitespaces
        from = findPreviousNonWhiteSpace(from, false);

        // find the start of the current word
        return findWordStart(from);

    } else {
        // calculate coordinate of previous glyph (could be on previous line)
        auto index = getIndex(from);

        if (index == 0) {
            return (from.line > 0) ? Coordinate(from.line - 1, at(from.line - 1).maxColumn) : from;

        } else {
            return Coordinate(from.line, getColumn(from.line, index - 1));
        }
    }
}


//
//	TextEditor::Document::getRight
//

TextEditor::Coordinate TextEditor::Document::getRight(Coordinate from, bool wordMode) const {
    if (wordMode) {
        // first move right by one glyph
        from = getRight(from);

        // now skip all whitespaces
        from = findNextNonWhiteSpace(from, false);

        // find the end of the current word
        auto index = getIndex(from);
        return findWordEnd(Coordinate(from.line, getColumn(from.line, index)));

    } else {
        // calculate coordinate of next glyph (could be on next line)
        auto index = getIndex(from);

        if (index == at(from.line).size()) {
            return (from.line < lineCount() - 1) ? Coordinate(from.line + 1, 0) : from;

        } else {
            return Coordinate(from.line, getColumn(from.line, index + 1));
        }
    }
}


//
//	TextEditor::Document::getTop
//

TextEditor::Coordinate TextEditor::Document::getTop() const {
    return Coordinate(0, 0);
}


//
//	TextEditor::Document::getBottom
//

TextEditor::Coordinate TextEditor::Document::getBottom() const {
    auto lastLine = lineCount() - 1;
    return Coordinate(lastLine, at(lastLine).maxColumn);
}


//
//	TextEditor::Document::getStartOfLine
//

TextEditor::Coordinate TextEditor::Document::getStartOfLine(Coordinate from) const {
    return Coordinate(from.line, 0);
}


//
//	TextEditor::Document::getEndOfLine
//

TextEditor::Coordinate TextEditor::Document::getEndOfLine(Coordinate from) const {
    return Coordinate(from.line, at(from.line).maxColumn);
}


//
//	TextEditor::Document::findWordStart
//

TextEditor::Coordinate TextEditor::Document::findWordStart(Coordinate from, bool wordOnly) const {
    auto& line = at(from.line);
    auto lineSize = line.size();

    if (from.column == 0 || lineSize == 0) {
        return from;

    } else {
        auto index = getIndex(from);
        auto firstCharacter = line[index - 1].codepoint;

        if (!wordOnly && CodePoint::isWhiteSpace(firstCharacter)) {
            while (index > 0 && CodePoint::isWhiteSpace(line[index - 1].codepoint)) {
                index--;
            }

        } else if (CodePoint::isWord(firstCharacter)) {
            while (index > 0 && CodePoint::isWord(line[index - 1].codepoint)) {
                index--;
            }

        } else {
            while (!wordOnly && index > 0 && !CodePoint::isWord(line[index - 1].codepoint) && !CodePoint::isWhiteSpace(line[index - 1].codepoint)) {
                index--;
            }
        }

        return Coordinate(from.line, getColumn(line, index));
    }
}


//
//	TextEditor::Document::findWordEnd
//

TextEditor::Coordinate TextEditor::Document::findWordEnd(Coordinate from, bool wordOnly) const {
    auto& line = at(from.line);
    auto index = getIndex(from);
    auto size = line.size();

    if (index >= size) {
        return from;

    } else {
        auto firstCharacter = line[index].codepoint;

        if (!wordOnly && CodePoint::isWhiteSpace(firstCharacter)) {
            while (index < size && CodePoint::isWhiteSpace(line[index].codepoint)) {
                index++;
            }

        } else if (CodePoint::isWord(firstCharacter)) {
            while (index < size && CodePoint::isWord(line[index].codepoint)) {
                index++;
            }

        } else {
            while (!wordOnly && index < size && !CodePoint::isWord(line[index].codepoint) && !CodePoint::isWhiteSpace(line[index].codepoint)) {
                index++;
            }
        }
    }

    return Coordinate(from.line, getColumn(line, index));
}


//
//	TextEditor::Document::findText
//

bool TextEditor::Document::findText(Coordinate from, const std::string_view& text, bool caseSensitive, bool wholeWord, Coordinate& start, Coordinate& end) const {
    // convert input string to vector of codepoints
    std::vector<ImWchar> search;
    auto endOfText = text.end();
    auto i = text.begin();

    while (i < endOfText) {
        ImWchar character;
        i = CodePoint::read(i, endOfText, &character);
        search.emplace_back(caseSensitive ? character : CodePoint::toLower(character));
    }

    // search document
    auto startLine = from.line;
    auto startIndex = getIndex(from);
    auto searchLine = startLine;
    auto searchIndex = startIndex;

    do {
        auto line = searchLine;
        auto index = searchIndex;
        auto lineSize = at(line).size();
        bool done = false;
        size_t j = 0;

        while (!done && j < search.size()) {
            if (search[j] == '\n') {
                if (index == lineSize) {
                    if (line == lineCount() - 1) {
                        done = true;

                    } else {
                        line++;
                        index = 0;
                        lineSize = at(line).size();
                        j++;
                    }

                } else {
                    done = true;
                }

            } else {
                if (index == lineSize) {
                    done = true;

                } else {
                    auto ch = at(line)[index].codepoint;

                    if (!caseSensitive) {
                        ch = CodePoint::toLower(ch);
                    }

                    if (ch == search[j]) {
                        index++;
                        j++;

                    } else {
                        done = true;
                    }
                }
            }
        }

        if (j == search.size()) {
            start = Coordinate(searchLine, getColumn(searchLine, searchIndex));
            end = Coordinate(line, getColumn(line, index));

            if (!wholeWord || isWholeWord(start, end)) {
                return true;
            }
        }

        if (searchIndex == at(searchLine).size()) {
            searchLine = (searchLine == lineCount() - 1) ? 0 : searchLine + 1;
            searchIndex = 0;

        } else {
            searchIndex++;
        }

    } while (searchLine != startLine || searchIndex != startIndex);

    return false;
}


//
//	TextEditor::Document::setUserData
//

void TextEditor::Document::setUserData(int line, void* data) {
    if (line >= 0 && line < lineCount()) {
        at(static_cast<size_t>(line)).userData = data;
    }
}


//
//	TextEditor::Document::getUserData
//

void* TextEditor::Document::getUserData(int line) const {
    if (line >= 0 && line < lineCount()) {
        return at(static_cast<size_t>(line)).userData;

    } else {
        return nullptr;
    }
}

//
//	TextEditor::Document::iterateUserData
//

void TextEditor::Document::iterateUserData(std::function<void(int line, void* data)> callback) const {
    for (size_t i = 0; i < size(); i++) {
        callback(static_cast<int>(i), at(i).userData);
    }
}


//
//	TextEditor::Document::iterateIdentifiers
//

static inline bool isIdentifier(TextEditor::Color color) {
    return
    color == TextEditor::Color::identifier ||
    color == TextEditor::Color::knownIdentifier;
}

void TextEditor::Document::iterateIdentifiers(std::function<void(const std::string&)> callback) const {
    for (size_t i = 0; i < size(); i++) {
        auto p = at(i).begin();
        auto end = at(i).end();
        char utf8[4];

        while (p < end) {
            if (isIdentifier(p->color)) {
                std::string identifier;

                while (p < end && isIdentifier(p->color)) {
                    identifier.append(std::string_view(utf8, CodePoint::write(utf8, p->codepoint)));
                    p++;
                }

                callback(identifier);

            } else {
                p++;
            }
        }
    }
}


//
//	TextEditor::Document::isWholeWord
//

bool TextEditor::Document::isWholeWord(Coordinate start, Coordinate end) const {
    if (start.line != end.line || end.column - start.column < 1) {
        return false;

    } else {
        auto wordStart = findWordStart(Coordinate(start.line, start.column + 1));
        auto wordEnd = findWordEnd(Coordinate(end.line, end.column - 1));
        return start == wordStart && end == wordEnd;
    }
}


//
//	TextEditor::Document::findPreviousNonWhiteSpace
//

TextEditor::Coordinate TextEditor::Document::findPreviousNonWhiteSpace(Coordinate from, bool includeEndOfLine) const {
    bool done = false;

    while (!done) {
        auto& line = at(from.line);
        auto index = getIndex(from);

        while (!done && index > 0) {
            index--;

            if (!CodePoint::isWhiteSpace(line[index].codepoint)) {
                from.column = getColumn(line, index);
                done = true;
            }
        }

        if (!done) {
            if (from.line == 0 || !includeEndOfLine) {
                from.column = 0;
                done = true;

            } else {
                from.line--;
                from.column = at(from.line).maxColumn;
            }
        }
    }

    return from;
}


//
//	TextEditor::Document::findNextNonWhiteSpace
//

TextEditor::Coordinate TextEditor::Document::findNextNonWhiteSpace(Coordinate from, bool includeEndOfLine) const {
    bool done = false;

    while (!done) {
        auto& line = at(from.line);
        auto index = getIndex(from);

        while (!done && index < line.size()) {
            if (CodePoint::isWhiteSpace(line[index].codepoint)) {
                index++;

            } else {
                from.column = getColumn(line, index);
                done = true;
            }
        }

        if (!done) {
            if (from.line == lineCount() || !includeEndOfLine) {
                from.column = line.maxColumn;
                done = true;

            } else {
                from.line++;
                from.column = 0;
            }
        }
    }

    return from;
}


//
//	TextEditor::Document::normalizeCoordinate
//

TextEditor::Coordinate TextEditor::Document::normalizeCoordinate(Coordinate coordinate) const {
    if (coordinate.line < 0) {
        return Coordinate(0, 0);

    } else if (coordinate.line >= lineCount()) {
        return Coordinate(lineCount() - 1, at(size() - 1).maxColumn);

    } else if (coordinate.column < 0) {
        return Coordinate(coordinate.line, 0);

    } else if (coordinate.column > at(coordinate.line).maxColumn) {
        return Coordinate(coordinate.line, at(coordinate.line).maxColumn);

    } else {
        // determine column numbers left and right of provided coordinate
        auto& line = at(coordinate.line);
        auto end = line.end();
        auto leftCol = 0;
        auto rightCol = 0;

        for (auto glyph = line.begin(); rightCol < coordinate.column && glyph < end; glyph++) {
            leftCol = rightCol;
            rightCol = (glyph->codepoint == '\t') ? ((rightCol / tabSize) + 1) * tabSize : rightCol + 1;
        }

        auto leftDiff = coordinate.column - leftCol;
        auto rightDiff = rightCol - coordinate.column;
        return Coordinate(coordinate.line, leftDiff <= rightDiff ? leftCol : rightCol);
    }
}


//
//	TextEditor::Document::normalizeCoordinate
//

void TextEditor::Document::normalizeCoordinate(float line, float column, Coordinate& glyphCoordinate, Coordinate& cursorCoordinate) const {
    // normalize coordinates by clamping them to the document and line range
    // the returned glyphCoordinate addresses the glyph pointed to by the line and column parameters
    // the returned cursorCoordinate returns the closest cursor position (which can be at the start or the end of the glyph)
    if (line < 0.0f) {
        glyphCoordinate = Coordinate(0, 0);
        cursorCoordinate = glyphCoordinate;

    } else if (line >= static_cast<float>(lineCount())) {
        glyphCoordinate = Coordinate(lineCount() - 1, at(lineCount() - 1).maxColumn);;
        cursorCoordinate = glyphCoordinate;

    } else {
        auto lineNo = static_cast<int>(line);

        if (column < 0.0f) {
            glyphCoordinate = Coordinate(lineNo, 0);
            cursorCoordinate = glyphCoordinate;

        } else if (column >= static_cast<float>(at(lineNo).maxColumn)) {
            glyphCoordinate = Coordinate(lineNo, at(lineNo).maxColumn);
            cursorCoordinate = glyphCoordinate;

        } else {
            // determine column numbers left and right of provided coordinate
            auto leftCol = 0;
            auto rightCol = 0;
            auto end = at(lineNo).end();

            for (auto glyph = at(lineNo).begin(); rightCol < column && glyph < end; glyph++) {
                leftCol = rightCol;
                rightCol = (glyph->codepoint == '\t') ? ((rightCol / tabSize) + 1) * tabSize : rightCol + 1;
            }

            auto leftDiff = column - static_cast<float>(leftCol);
            auto rightDiff = static_cast<float>(rightCol) - column;

            glyphCoordinate = Coordinate(lineNo, leftCol);
            cursorCoordinate = Coordinate(lineNo, leftDiff <= rightDiff ? leftCol : rightCol);
        }
    }
}


//
//	TextEditor::Document::appendLine
//

void TextEditor::Document::appendLine() {
    auto& line = emplace_back();

    if (insertor) {
        line.userData = insertor(static_cast<int>(size() - 1));
    }
}


//
//	TextEditor::Document::insertLine
//

void TextEditor::Document::insertLine(int offsset) {
    auto line = insert(begin() + offsset, Line());

    if (insertor) {
        line->userData = insertor(offsset);
    }
}


//
//	TextEditor::Document::deleteLines
//

void TextEditor::Document::deleteLines(int start, int end) {
    if (deletor) {
        for (auto i = start; i <= end; i++) {
            deletor(i, at(i).userData);
        }
    }

    erase(begin() + start, begin() + end + 1);
}


//
//	TextEditor::Document::clearDocument
//

void TextEditor::Document::clearDocument() {
    if (deletor) {
        for (auto i = 0; i <= lineCount(); i++) {
            deletor(i, at(i).userData);
        }
    }

    clear();
}


//
//	TextEditor::Transactions::reset
//

void TextEditor::Transactions::reset() {
    clear();
    undoIndex = 0;
    version = 0;
}


//
//	TextEditor::Transactions::add
//

void TextEditor::Transactions::add(std::shared_ptr<Transaction> transaction) {
    resize(undoIndex);
    push_back(transaction);
    undoIndex++;
    version++;
}


//
//	TextEditor::Transactions::undo
//

void TextEditor::Transactions::undo(Document& document, Cursors& cursors) {
    auto transaction = at(--undoIndex);

    for (auto action = transaction->rbegin(); action < transaction->rend(); action++) {
        if (action->type == Action::Type::insertText) {
            document.deleteText(action->start, action->end);

        } else {
            document.insertText(action->start, action->text);
        }
    }

    cursors = transaction->getBeforeState();
    version++;
}


//
//	TextEditor::Transactions::redo
//

void TextEditor::Transactions::redo(Document& document, Cursors& cursors) {
    auto transaction = at(undoIndex++);

    for (auto action = transaction->begin(); action < transaction->end(); action++) {
        if (action->type == Action::Type::insertText) {
            document.insertText(action->start, action->text);

        } else {
            document.deleteText(action->start, action->end);
        }
    }

    cursors = transaction->getAfterState();
    version++;
}


//
//	TextEditor::Colorizer::update
//

TextEditor::State TextEditor::Colorizer::update(Line& line, const Language* language) {
    auto state = line.state;

    // process all glyphs on this line
    auto nonWhiteSpace = false;
    auto glyph = line.begin();

    while (glyph < line.end()) {
        if (state == State::inText) {
            // special handling for preprocessor lines
            if (!nonWhiteSpace && language->preprocess && glyph->codepoint != language->preprocess && !CodePoint::isWhiteSpace(glyph->codepoint)) {
                nonWhiteSpace = true;
            }

            // start parsing glyphs
            auto start = glyph;

            // mark whitespace characters
            if (CodePoint::isWhiteSpace(glyph->codepoint)) {
                (glyph++)->color = Color::whitespace;

                // handle single line comments
            } else if (language->singleLineComment.size() && matches(glyph, line.end(), language->singleLineComment)) {
                setColor(glyph, line.end(), Color::comment);
                glyph = line.end();

            } else if (language->singleLineCommentAlt.size() && matches(glyph, line.end(), language->singleLineCommentAlt)) {
                setColor(glyph, line.end(), Color::comment);
                glyph = line.end();

                // are we starting a multiline comment
            } else if (language->commentStart.size() && matches(glyph, line.end(), language->commentStart)) {
                state = State::inComment;
                auto size = language->commentEnd.size();
                setColor(glyph, glyph + size, Color::comment);
                glyph += size;

                // are we starting a special string
            } else if (language->otherStringStart.size() && matches(glyph, line.end(), language->otherStringStart)) {
                state = State::inOtherString;
                auto size = language->otherStringStart.size();
                setColor(glyph, glyph + size, Color::string);
                glyph += size;

            } else if (language->otherStringAltStart.size() && matches(glyph, line.end(), language->otherStringAltStart)) {
                state = State::inOtherStringAlt;
                auto size = language->otherStringAltStart.size();
                setColor(glyph, glyph + size, Color::string);
                glyph += size;

                // are we starting a single quoted string
            } else if (language->hasSingleQuotedStrings && glyph->codepoint == CodePoint::singleQuote) {
                state = State::inSingleQuotedString;
                (glyph++)->color = Color::string;

                // are we starting a double quoted string
            } else if (language->hasDoubleQuotedStrings && glyph->codepoint == CodePoint::doubleQuote) {
                state = State::inDoubleQuotedString;
                (glyph++)->color = Color::string;

                // is this a preprocessor line
            } else if (language->preprocess && !nonWhiteSpace && glyph->codepoint == language->preprocess) {
                setColor(line.begin(), line.end(), Color::preprocessor);
                glyph = line.end();

                // handle custom tokenizer (if we have one)
            } else if (language->customTokenizer) {
                Color color;
                Iterator tokenStart(&*glyph);
                Iterator lineEnd(line.data() + line.size());
                Iterator tokenEnd = language->customTokenizer(tokenStart, lineEnd, color);

                if (tokenEnd != tokenStart) {
                    auto size = tokenEnd - tokenStart;
                    setColor(glyph, glyph + size, color);
                    glyph += size;
                }
            }

            if (glyph == start) {
                // nothing worked so far so it's time to do some tokenizing
                Color color;
                Iterator lineEnd(line.data() + line.size());
                Iterator tokenStart(&*glyph);
                Iterator tokenEnd;

                // do we have an identifier
                if (language->getIdentifier && (tokenEnd = language->getIdentifier(tokenStart, lineEnd)) != tokenStart) {
                    // determine identifier text and color color
                    auto size = tokenEnd - tokenStart;
                    std::string identifier;
                    color = Color::identifier;

                    for (auto i = tokenStart; i < tokenEnd; i++) {
                        ImWchar codepoint = *i;

                        if (!language->caseSensitive) {
                            codepoint = CodePoint::toLower(codepoint);
                        }

                        char utf8[4];
                        identifier.append(utf8, CodePoint::write(utf8, codepoint));
                    }

                    if (language->keywords.find(identifier) != language->keywords.end()) {
                        color = Color::keyword;

                    } else if (language->declarations.find(identifier) != language->declarations.end()) {
                        color = Color::declaration;

                    } else if (language->identifiers.find(identifier) != language->identifiers.end()) {
                        color = Color::knownIdentifier;
                    }

                    // colorize identifier and move on
                    setColor(glyph, glyph + size, color);
                    glyph += size;

                    // do we have a number
                } else if (language->getNumber && (tokenEnd = language->getNumber(tokenStart, lineEnd)) != tokenStart) {
                    auto size = tokenEnd - tokenStart;
                    setColor(glyph, glyph + size, Color::number);
                    glyph += size;

                    // is this punctuation
                } else if (language->isPunctuation && language->isPunctuation(glyph->codepoint)) {
                    (glyph++)->color = Color::punctuation;

                } else {
                    // I guess we don't know what this character is
                    (glyph++)->color = Color::text;
                }
            }

        } else if (state == State::inComment) {
            // stay in comment state until we see the end sequence
            if (matches(glyph, line.end(), language->commentEnd)) {
                auto size = language->commentEnd.size();
                setColor(glyph, glyph + size, Color::comment);
                glyph += size;
                state = State::inText;

            } else {
                (glyph++)->color = Color::comment;
            }

        } else if (state == State::inOtherString) {
            // stay in otherString state until we see the end sequence
            // skip escaped characters
            if (glyph->codepoint == language->stringEscape) {
                (glyph++)->color = Color::string;

                if (glyph < line.end()) {
                    (glyph++)->color = Color::string;
                }

            } else if (matches(glyph, line.end(), language->otherStringEnd)) {
                auto size = language->otherStringEnd.size();
                setColor(glyph, glyph + size, Color::string);
                glyph += size;
                state = State::inText;

            } else {
                (glyph++)->color = Color::comment;
            }

        } else if (state == State::inOtherStringAlt) {
            // stay in otherStringAlt state until we see the end sequence
            // skip escaped characters
            if (glyph->codepoint == language->stringEscape) {
                (glyph++)->color = Color::string;

                if (glyph < line.end()) {
                    (glyph++)->color = Color::string;
                }

            } else if (matches(glyph, line.end(), language->otherStringAltEnd)) {
                auto size = language->otherStringAltEnd.size();
                setColor(glyph, glyph + size, Color::string);
                glyph += size;
                state = State::inText;

            } else {
                (glyph++)->color = Color::comment;
            }

        } else if (state == State::inSingleQuotedString) {
            // stay in single quote state until we see an end
            // skip escaped characters
            if (glyph->codepoint == language->stringEscape) {
                (glyph++)->color = Color::string;

                if (glyph < line.end()) {
                    (glyph++)->color = Color::string;
                }

            } else if (glyph->codepoint == CodePoint::singleQuote) {
                (glyph++)->color = Color::string;
                state = State::inText;

            } else {
                (glyph++)->color = Color::string;
            }

        } else if (state == State::inDoubleQuotedString) {
            // stay in double quote state until we see an end
            // skip escaped characters
            if (glyph->codepoint == language->stringEscape) {
                (glyph++)->color = Color::string;

                if (glyph < line.end()) {
                    (glyph++)->color = Color::string;
                }

            } else if (glyph->codepoint == CodePoint::doubleQuote) {
                (glyph++)->color = Color::string;
                state = State::inText;

            } else {
                (glyph++)->color = Color::string;
            }
        }
    }

    line.colorize = false;
    return state;
}


//
//	TextEditor::Colorizer::updateEntireDocument
//

void TextEditor::Colorizer::updateEntireDocument(Document& document, const Language* language) {
    if (language) {
        for (auto line = document.begin(); line < document.end(); line++) {
            auto state = update(*line, language);
            auto next = line + 1;

            if (next < document.end()) {
                next->state = state;
            }
        }

    } else {
        for (auto line = document.begin(); line < document.end(); line++) {
            for (auto glyph = line->begin(); glyph < line->end(); glyph++) {
                glyph->color = Color::text;
            }

            line->state = State::inText;
            line->colorize = false;
        }
    }
}


//
//	TextEditor::Colorizer::updateChangedLines
//

void TextEditor::Colorizer::updateChangedLines(Document& document, const Language* language) {
    for (auto line = document.begin(); line < document.end(); line++) {
        if (line->colorize) {
            auto state = update(*line, language);
            auto next = line + 1;

            if (next < document.end() && next->state != state) {
                next->state = state;
                next->colorize = true;
            }
        }
    }
}


//
//	TextEditor::Colorizer::matches
//

bool TextEditor::Colorizer::matches(Line::iterator start, Line::iterator end, const std::string_view& text) {
    // see if text at iterators matches provided UTF-8 string
    auto i = text.begin();

    while (i < text.end()) {
        if (start == end) {
            return false;
        }

        ImWchar codepoint;
        i = CodePoint::read(i, text.end(), &codepoint);

        if ((start++)->codepoint != codepoint) {
            return false;
        }
    }

    return true;
}


//
//	TextEditor::Bracketeer::reset
//

void TextEditor::Bracketeer::reset() {
    clear();
}


//
//	TextEditor::Bracketeer::update
//

void TextEditor::Bracketeer::update(Document& document) {
    Color bracketColors[] = {
        Color::matchingBracketLevel1,
        Color::matchingBracketLevel2,
        Color::matchingBracketLevel3
    };

    reset();
    std::vector<size_t> levels;
    int level = 0;

    // process all the glyphs
    for (int line = 0; line < document.lineCount(); line++) {
        for (size_t index = 0; index < document[line].size(); index++) {
            auto& glyph = document[line][index];

            // handle a "bracket opener" that is not in a comment, string or preprocessor statement
            if (isBracketCandidate(glyph) && CodePoint::isBracketOpener(glyph.codepoint)) {
                // start a new level
                levels.emplace_back(size());
                emplace_back(glyph.codepoint, Coordinate(line, document.getColumn(line, index)), static_cast<ImWchar>(0), Coordinate::invalid(), level);
                glyph.color = bracketColors[level % 3];
                level++;

                // handle a "bracket closer" that is not in a comment, string or preprocessor statement
            } else if (isBracketCandidate(glyph) && CodePoint::isBracketCloser(glyph.codepoint)) {
                if (levels.size()) {
                    auto& lastBracket = at(levels.back());
                    levels.pop_back();
                    level--;

                    if (lastBracket.startChar == CodePoint::toPairOpener(glyph.codepoint)) {
                        // handle matching bracket
                        glyph.color = bracketColors[level % 3];
                        lastBracket.endChar = glyph.codepoint;
                        lastBracket.end = Coordinate(line, document.getColumn(line, index));

                    } else {
                        // no matching bracket, mark brackets as errors
                        glyph.color = Color::matchingBracketError;
                        document[lastBracket.start.line][document.getIndex(lastBracket.start)].color = Color::matchingBracketError;
                        pop_back();
                    }

                    // this is a closer without an opener
                } else {
                    glyph.color = Color::matchingBracketError;
                }
            }
        }
    }

    // handle levels left open and mark them as errors
    if (levels.size()) {
        for (auto i = levels.rbegin(); i < levels.rend(); i++) {
            auto& start = at(*i).start;
            document[start.line][document.getIndex(start)].color = Color::matchingBracketError;
            erase(begin() + *i);
        }
    }
}


//
//	TextEditor::Bracketeer::getEnclosingBrackets
//

TextEditor::Bracketeer::iterator TextEditor::Bracketeer::getEnclosingBrackets(Coordinate location) {
    iterator brackets = end();
    bool done = false;

    for (auto i = begin(); !done && i < end(); i++) {
        // brackets are sorted so no need to go past specified location
        if (i->isAfter(location)) {
            done = true;
        }

        else if (i->isAround(location)) {
            // this could be what we're looking for
            brackets = i;
        }
    }

    return brackets;
}


//
//	TextEditor::Bracketeer::getEnclosingCurlyBrackets
//

TextEditor::Bracketeer::iterator TextEditor::Bracketeer::getEnclosingCurlyBrackets(Coordinate first, Coordinate last) {
    iterator brackets = end();
    bool done = false;

    for (auto i = begin(); !done && i < end(); i++) {
        // brackets are sorted so no need to go past specified location
        if (i->isAfter(first)) {
            done = true;
        }

        else if (i->isAround(first) && i->isAround(last) && i->startChar == CodePoint::openCurlyBracket) {
            // this could be what we're looking for
            brackets = i;
        }
    }

    return brackets;
}


//
//	TextEditor::Bracketeer::getInnerCurlyBrackets
//

TextEditor::Bracketeer::iterator TextEditor::Bracketeer::getInnerCurlyBrackets(Coordinate first, Coordinate last) {
    iterator brackets = end();
    auto outer = getEnclosingCurlyBrackets(first, last);

    if (outer != end()) {
        bool done = false;

        for (auto i = outer + 1; i < end() && !done; i++) {
            if (i->level <= outer->level) {
                done = true;

            } else if (
                i->level == outer->level + 1 &&
                i->startChar == CodePoint::openCurlyBracket &&
                i->start > first &&
                i->end < last) {

                brackets = i;
            done = true;
                }
        }
    }

    return brackets;
}


//
//	latchButton
//

static bool latchButton(const char* label, bool* value, const ImVec2& size) {
    auto changed = false;
    ImVec4* colors = ImGui::GetStyle().Colors;

    if (*value) {
        ImGui::PushStyleColor(ImGuiCol_Button, colors[ImGuiCol_ButtonActive]);
        ImGui::PushStyleColor(ImGuiCol_ButtonHovered, colors[ImGuiCol_ButtonActive]);
        ImGui::PushStyleColor(ImGuiCol_ButtonActive, colors[ImGuiCol_TableBorderLight]);

    } else {
        ImGui::PushStyleColor(ImGuiCol_Button, colors[ImGuiCol_TableBorderLight]);
        ImGui::PushStyleColor(ImGuiCol_ButtonHovered, colors[ImGuiCol_TableBorderLight]);
        ImGui::PushStyleColor(ImGuiCol_ButtonActive, colors[ImGuiCol_ButtonActive]);
    }

    ImGui::Button(label, size);

    if (ImGui::IsItemClicked(ImGuiMouseButton_Left)) {
        *value = !*value;
        changed = true;
    }

    ImGui::PopStyleColor(3);
    return changed;
}


//
//	inputString
//

static bool inputString(const char* label, std::string* value, ImGuiInputTextFlags flags=ImGuiInputTextFlags_None) {
    flags |=
    ImGuiInputTextFlags_NoUndoRedo |
    ImGuiInputTextFlags_CallbackResize;

    return ImGui::InputText(label, (char*) value->c_str(), value->capacity() + 1, flags, [](ImGuiInputTextCallbackData* data) {
        if (data->EventFlag == ImGuiInputTextFlags_CallbackResize) {
            std::string* value = (std::string*) data->UserData;
            value->resize(data->BufTextLen);
            data->Buf = (char*) value->c_str();
        }

        return 0;
    }, value);
}


//
//	TextEditor::renderFindReplace
//

void TextEditor::renderFindReplace(ImVec2 pos, float width) {
    // render find/replace window (if required)
    if (findReplaceVisible) {
        // save current screen position
        auto currentScreenPosition = ImGui::GetCursorScreenPos();

        // calculate sizes
        ImGui::PushStyleVar(ImGuiStyleVar_ItemSpacing, ImVec2(6.0f, 4.0f));
        auto& style = ImGui::GetStyle();
        auto fieldWidth = 250.0f;

        auto button1Width = ImGui::CalcTextSize(findButtonLabel.c_str()).x + style.ItemSpacing.x * 2.0f;
        auto button2Width = ImGui::CalcTextSize(findAllButtonLabel.c_str()).x + style.ItemSpacing.x * 2.0f;
        auto optionWidth = ImGui::CalcTextSize("Aa").x + style.ItemSpacing.x * 2.0f;

        if (!readOnly) {
            button1Width = std::max(button1Width, ImGui::CalcTextSize(replaceButtonLabel.c_str()).x + style.ItemSpacing.x * 2.0f);
            button2Width = std::max(button2Width, ImGui::CalcTextSize(replaceAllButtonLabel.c_str()).x + style.ItemSpacing.x * 2.0f);
        }

        auto windowHeight =
        style.ChildBorderSize * 2.0f +
        style.WindowPadding.y * 2.0f +
        ImGui::GetFrameHeight() +
        (readOnly ? 0.0f : (style.ItemSpacing.y + ImGui::GetFrameHeight()));

        auto windowWidth =
        style.ChildBorderSize * 2.0f +
        style.WindowPadding.x * 2.0f +
        fieldWidth + style.ItemSpacing.x +
        button1Width + style.ItemSpacing.x +
        button2Width + style.ItemSpacing.x +
        optionWidth * 3.0f + style.ItemSpacing.x * 2.0f;

        // create window
        ImGui::SetNextWindowPos(ImVec2(
            pos.x + width - windowWidth - style.ItemSpacing.x,
            pos.y + style.ItemSpacing.y * 2.0f));

        ImGui::SetNextWindowSize(ImVec2(windowWidth, windowHeight));
        ImGui::SetNextWindowBgAlpha(0.75f);

        ImGui::BeginChild("find-replace", ImVec2(windowWidth, windowHeight), ImGuiChildFlags_Borders);
        ImGui::SetNextItemWidth(fieldWidth);

        if (focusOnFind) {
            ImGui::SetKeyboardFocusHere();
            focusOnFind = false;

        } else if (findCancelledAutocomplete) {
            ImGui::SetKeyboardFocusHere();
            findCancelledAutocomplete = false;
        }

        if (inputString("###find", &findText, ImGuiInputTextFlags_AutoSelectAll)) {
            if (findText.size()) {
                selectFirstOccurrenceOf(findText, caseSensitiveFind, wholeWordFind);

            } else {
                cursors.clearAll();
            }
        }

        if (ImGui::IsItemDeactivated()) {
            if (ImGui::IsKeyPressed(ImGuiKey_Escape)) {
                closeFindReplace();

            } else if (ImGui::IsKeyPressed(ImGuiKey_Enter) || ImGui::IsKeyPressed(ImGuiKey_KeypadEnter)) {
                focusOnEditor = true;
                focusOnFind = false;
            }
        }

        bool disableFindButtons = !findText.size();

        if (disableFindButtons) {
            ImGui::BeginDisabled();
        }

        ImGui::SameLine();

        if (ImGui::Button(findButtonLabel.c_str(), ImVec2(button1Width, 0.0f))) {
            find();
        }

        ImGui::SameLine();

        if (ImGui::Button(findAllButtonLabel.c_str(), ImVec2(button2Width, 0.0f))) {
            findAll();
        }

        if (disableFindButtons) {
            ImGui::EndDisabled();
        }

        ImGui::SameLine();

        if (latchButton("Aa", &caseSensitiveFind, ImVec2(optionWidth, 0.0f))) {
            find();
        }

        ImGui::SameLine();

        if (latchButton("[]", &wholeWordFind, ImVec2(optionWidth, 0.0f))) {
            find();
        }

        ImGui::SameLine();

        if (ImGui::Button("x", ImVec2(optionWidth, 0.0f))) {
            closeFindReplace();
        }

        if (!readOnly) {
            ImGui::SetNextItemWidth(fieldWidth);
            inputString("###replace", &replaceText);
            ImGui::SameLine();

            bool disableReplaceButtons = !findText.size() || !replaceText.size();

            if (disableReplaceButtons) {
                ImGui::BeginDisabled();
            }

            if (ImGui::Button(replaceButtonLabel.c_str(), ImVec2(button1Width, 0.0f))) {
                replace();
            }

            ImGui::SameLine();

            if (ImGui::Button(replaceAllButtonLabel.c_str(), ImVec2(button2Width, 0.0f))) {
                replaceAll();
            }

            if (disableReplaceButtons) {
                ImGui::EndDisabled();
            }
        }

        ImGui::EndChild();
        ImGui::PopStyleVar();
        ImGui::SetCursorScreenPos(currentScreenPosition);
    }
}


//
//	TextEditor::selectFirstOccurrenceOf
//

void TextEditor::selectFirstOccurrenceOf(const std::string_view& text, bool caseSensitive, bool wholeWord) {
    Coordinate start, end;

    if (document.findText(Coordinate(0, 0), text, caseSensitive, wholeWord, start, end)) {
        cursors.setCursor(start, end);
        makeCursorVisible();

    } else {
        cursors.clearAdditional(true);
    }
}


//
//	TextEditor::selectNextOccurrenceOf
//

void TextEditor::selectNextOccurrenceOf(const std::string_view& text, bool caseSensitive, bool wholeWord) {
    Coordinate start, end;

    if (document.findText(cursors.getCurrent().getSelectionEnd(), text, caseSensitive, wholeWord, start, end)) {
        cursors.setCursor(start, end);
        makeCursorVisible();

    } else {
        cursors.clearAdditional(true);
    }
}


//
//	TextEditor::selectAllOccurrencesOf
//

void TextEditor::selectAllOccurrencesOf(const std::string_view& text, bool caseSensitive, bool wholeWord) {
    Coordinate start, end;

    if (document.findText(Coordinate(0, 0), text, caseSensitive, wholeWord, start, end)) {
        cursors.setCursor(start, end);
        bool done = false;

        while (!done) {
            Coordinate nextStart, nextEnd;
            document.findText(cursors.getCurrent().getSelectionEnd(), text, caseSensitive, wholeWord, nextStart, nextEnd);

            if (nextStart == start && nextEnd == end) {
                done = true;

            } else {
                cursors.addCursor(nextStart, nextEnd);
            }
        }

        makeCursorVisible();

    } else {
        cursors.clearAdditional(true);
    }
}


//
//	TextEditor::addNextOccurrence
//

void TextEditor::addNextOccurrence() {

    auto cursor = cursors.getCurrent();
    auto text = document.getSectionText(cursor.getSelectionStart(), cursor.getSelectionEnd());
    Coordinate start, end;

    if (document.findText(cursor.getSelectionEnd(), text, true, false, start, end)) {
        cursors.addCursor(start, end);
    }
}


//
//	TextEditor::addNextOccurrences
//

void TextEditor::selectAllOccurrences() {
    auto cursor = cursors.getCurrent();
    auto text = document.getSectionText(cursor.getSelectionStart(), cursor.getSelectionEnd());
    selectAllOccurrencesOf(text, true, false);
}


//
//	TextEditor::replaceTextInCurrentCursor
//

void TextEditor::replaceTextInCurrentCursor(const std::string_view& text) {
    auto transaction = startTransaction();

    // first delete old text
    auto cursor = cursors.getCurrentAsIterator();
    auto start = cursor->getSelectionStart();
    auto end = cursor->getSelectionEnd();
    deleteText(transaction, start, end);
    cursors.adjustForDelete(cursor, start, end);

    // now insert new text
    Coordinate newEnd = insertText(transaction, start, text);
    cursor->update(newEnd, false);
    cursors.adjustForInsert(cursor, start, newEnd);

    endTransaction(transaction);
}


//
//	TextEditor::replaceTextInAllCursors
//

void TextEditor::replaceTextInAllCursors(const std::string_view& text) {
    auto transaction = startTransaction();
    insertTextIntoAllCursors(transaction, text);
    endTransaction(transaction);
}


//
//	TextEditor::replaceSectionText
//

void TextEditor::replaceSectionText(const Coordinate& start, const Coordinate& end, const std::string_view& text) {
    auto transaction = startTransaction();
    deleteText(transaction, start, end);
    auto newEnd = insertText(transaction, start, text);
    cursors.clearAdditional();
    cursors.getMain().update(newEnd, newEnd);
    endTransaction(transaction);
}


//
//	TextEditor::openFindReplace
//

void TextEditor::openFindReplace() {
    // get main cursor location
    auto cursor = cursors.getMain();

    // see if we have a current selection that's on one line
    if (cursor.hasSelection()) {
        if (cursor.getSelectionStart().line == cursor.getSelectionEnd().line) {
            // use it as the default search
            findText = document.getSectionText(cursor.getSelectionStart(), cursor.getSelectionEnd());
        }

    } else {
        // if cursor is inside "real" word, use that as the default
        auto start = document.findWordStart(cursor.getSelectionStart(), true);
        auto end = document.findWordEnd(cursor.getSelectionStart(), true);

        if (start != end) {
            findText = document.getSectionText(start, end);
        }
    }

    findReplaceVisible = true;
    focusOnFind = true;
}


//
//	TextEditor::closeFindReplace
//

void TextEditor::closeFindReplace() {
    findReplaceVisible = false;
    focusOnEditor = true;
    focusOnFind = false;
}


//
//	TextEditor::find
//

void TextEditor::find() {
    if (findText.size()) {
        selectNextOccurrenceOf(findText, caseSensitiveFind, wholeWordFind);
        focusOnEditor = true;
        focusOnFind = false;
    }
}


//
//	TextEditor::findNext
//

void TextEditor::findNext() {
    if (findText.size()) {
        selectNextOccurrenceOf(findText, caseSensitiveFind, wholeWordFind);
        focusOnEditor = true;
        focusOnFind = false;
    }
}


//
//	TextEditor::findAll
//

void TextEditor::findAll() {
    if (findText.size()) {
        selectAllOccurrencesOf(findText, caseSensitiveFind, wholeWordFind);
        focusOnEditor = true;
        focusOnFind = false;
    }
}


//
//	TextEditor::replace
//

void TextEditor::replace() {
    if (findText.size()) {
        if (!cursors.anyHasSelection()) {
            selectNextOccurrenceOf(findText, caseSensitiveFind, wholeWordFind);
        }

        replaceTextInCurrentCursor(replaceText);
        selectNextOccurrenceOf(findText, caseSensitiveFind, wholeWordFind);
        focusOnEditor = true;
        focusOnFind = false;
    }
}


//
//	TextEditor::replaceAll
//

void TextEditor::replaceAll() {
    if (findText.size()) {
        selectAllOccurrencesOf(findText, caseSensitiveFind, wholeWordFind);
        replaceTextInAllCursors(replaceText);
        focusOnEditor = true;
        focusOnFind = false;
    }
}



//
//	TextEditor::setAutoCompleteConfig
//


void TextEditor::Autocomplete::setConfig(const AutoCompleteConfig* config) {
    if (config) {
        configuration = *config;
        configured = true;

    } else {
        configured = false;
    }

    active = false;
}


//
//	TextEditor::Autocomplete::startTyping
//

bool TextEditor::Autocomplete::startTyping(Cursors& cursors) {
    if (!active && !requestActivation && configured && configuration.triggerOnTyping) {
        triggeredManually = false;
        start(cursors);
        return true;

    } else {
        return false;
    }
}


//
//	TextEditor::Autocomplete::startShortcut
//

bool TextEditor::Autocomplete::startShortcut(Cursors& cursors) {
    if (!active && !requestActivation && configured && configuration.triggerOnShortcut) {
        triggeredManually = true;
        start(cursors);
        return true;

    } else {
        return false;
    }
}


//
//	TextEditor::Autocomplete::cancel
//

void TextEditor::Autocomplete::cancel() {
    if (active) {
        requestDeactivation = true;
    }
}


//
//	renderSuggestion
//

static bool renderSuggestion(const std::string_view& suggestion, const std::string_view& searchTerm, float width, bool selected) {
    // custom widget to render an autocomplete suggestion in the style of Visual Studio Code
    auto glyphPos = ImGui::GetCursorScreenPos();
    auto size = ImVec2(width, ImGui::GetFrameHeightWithSpacing());
    auto clicked = ImGui::InvisibleButton("suggestion", size);

    auto drawList = ImGui::GetWindowDrawList();
    auto font = ImGui::GetFont();
    auto fontSize = ImGui::GetFontSize();
    auto glyphWidth = ImGui::CalcTextSize("#").x;

    // highlight selected item
    if (selected) {
        drawList->AddRectFilled(glyphPos, glyphPos + size, ImGui::GetColorU32(ImGuiCol_Header));
    }

    // process all UTF-8 glyphs in suggestion
    glyphPos += ImGui::GetStyle().FramePadding;
    auto suggestionEnd = suggestion.end();
    auto searchTermEnd = searchTerm.end();
    auto i = TextEditor::CodePoint::skipBOM(suggestion.begin(), suggestionEnd);
    auto j = TextEditor::CodePoint::skipBOM(searchTerm.begin(), searchTermEnd);

    while (i < suggestionEnd) {
        // get next glyph from suggestion
        ImWchar codepoint;
        i = TextEditor::CodePoint::read(i, suggestionEnd, &codepoint);

        // highlight glyph in suggestion that match search term
        auto color = ImGui::GetColorU32(ImGuiCol_Text);

        if (j < searchTermEnd) {
            ImWchar searchCodePoint;
            auto next = TextEditor::CodePoint::read(j, searchTermEnd, &searchCodePoint);

            if (TextEditor::CodePoint::toLower(searchCodePoint) == TextEditor::CodePoint::toLower(codepoint)) {
                color = ImGui::GetColorU32(ImGuiCol_TextLink);
                j = next;
            }
        }

        // render the glyph
        font->RenderChar(drawList, fontSize, glyphPos, color, codepoint);
        glyphPos.x += glyphWidth;
    }

    return clicked;
}


//
//	TextEditor::Autocomplete::render
//

bool TextEditor::Autocomplete::render(Document& document, Cursors& cursors, const Language* language, float textOffset, ImVec2 glyphSize) {
    // see if we need to activate autocomplete mode
    if (requestActivation) {
        // apply popup delay
        if (std::chrono::system_clock::now() > activationTime) {
            // reset activation flag
            requestActivation = false;

            // capture locations
            startLocation = document.findWordStart(currentLocation, true);

            // update the autocomplete state
            updateState(document, language);

            // handle cases where autocomplete request is ignored
            if(state.inComment && !configuration.triggerInComments) {
                return false;
            }

            if(state.inString && !configuration.triggerInStrings) {
                return false;
            }

            // get initial list of suggestions from the app
            refreshSuggestions();

            // show autocomplete popup window
            ImGui::OpenPopup("AutoCompleteContextMenu");
            active = true;
        }
    }

    // only continue if autocomplete is active
    if (!active) {
        return false;
    }

    // see if cursor moved since last time
    auto newLocation = cursors.getMain().getSelectionEnd();

    if (newLocation != currentLocation) {
        // see if we need to deactivate autocomplete because cursor is on new line
        if (newLocation.line != currentLocation.line) {
            requestDeactivation = true;

        } else {
            // see if cursor moved away from current word
            auto newStart = document.findWordStart(newLocation, true);

            if (newStart == startLocation) {
                currentLocation = newLocation;

                // we deactivate autocomplete if the current location is the start
                if (currentLocation == startLocation) {
                    requestDeactivation = true;

                } else {
                    updateState(document, language);
                    refreshSuggestions();
                }

            } else {
                requestDeactivation = true;
            }
        }
    }

    // open popup window
    bool result = false;
    auto cursorScreenPos = ImGui::GetCursorScreenPos();

    ImGui::SetNextWindowPos(ImVec2(
        cursorScreenPos.x + textOffset + currentLocation.column * glyphSize.x,
        cursorScreenPos.y + (currentLocation.line + 1) * glyphSize.y));

    auto suggestions = state.suggestions.size();
    auto visibleSuggestions = (suggestions == 0) ? 1 : std::min(static_cast<size_t>(10), suggestions);
    auto& style = ImGui::GetStyle();
    auto height = ImGui::GetFrameHeightWithSpacing() * visibleSuggestions + style.WindowPadding.y * 2.0f;
    ImGui::SetNextWindowSize(ImVec2(suggestionWidth, height));

    ImGuiWindowFlags flags =
    ImGuiWindowFlags_NoFocusOnAppearing |
    ImGuiWindowFlags_NoNav;

    if (ImGui::BeginPopup("AutoCompleteContextMenu", flags)) {
        if (ImGui::IsWindowAppearing()) {
            ImGui::BringWindowToDisplayFront(ImGui::GetCurrentWindow());
        }

        // deactivate popup (if requested)
        if (requestDeactivation) {
            ImGui::CloseCurrentPopup();
            requestDeactivation = false;
            active = false;

        } else {
            // do we have any suggestions
            if (suggestions) {
                auto items = state.suggestions.size();

                // apply arrow keys to selected suggestion
                if (ImGui::IsKeyPressed(ImGuiKey_UpArrow)) {
                    if (currentSelection == 0) {
                        currentSelection = items - 1;
                    }
                    else {
                        currentSelection--;
                    }

                } else if (ImGui::IsKeyPressed(ImGuiKey_DownArrow)) {
                    if (currentSelection == items - 1) {
                        currentSelection = 0;

                    } else {
                        currentSelection++;
                    }

                    // use selected suggestion if user hit tab of return
                } else if (ImGui::IsKeyPressed(ImGuiKey_Tab) || ImGui::IsKeyPressed(ImGuiKey_Enter) || ImGui::IsKeyPressed(ImGuiKey_KeypadEnter)) {
                    requestDeactivation = true;
                    result = true;

                } else if (configuration.autoInsertSingleSuggestions && triggeredManually && state.suggestions.size() == 1) {
                    requestDeactivation = true;
                    result = true;
                }

                // render suggestions
                for (size_t i = 0; i < items; i++) {
                    // ensure unique ID
                    ImGui::PushID(static_cast<int>(i));

                    // scroll list to selected item (if required)
                    auto selected = i == currentSelection;

                    if (selected) {
                        ImGui::SetScrollHereY(1.0f);
                    }

                    if (renderSuggestion(state.suggestions[i].c_str(), state.searchTerm, ImGui::GetContentRegionAvail().x, selected)) {
                        // user clicked on a suggestion, use it
                        currentSelection = i;
                        requestDeactivation = true;
                        result = true;
                    }

                    ImGui::PopID();
                }

            } else {
                ImGui::TextUnformatted(configuration.noSuggestionsLabel.c_str());
            }
        }

        ImGui::EndPopup();

    } else {
        requestDeactivation = false;
        active = false;
    }

    return result;
}


//
//	TextEditor::Autocomplete::setSuggestions
//

void TextEditor::Autocomplete::setSuggestions(const std::vector<std::string>& suggestions) {
    state.suggestions = suggestions;
    currentSelection = 0;
}


//
//	TextEditor::Autocomplete::isSpecialKeyPressed
//

bool TextEditor::Autocomplete::isSpecialKeyPressed() const {
    for (auto key : {ImGuiKey_Tab, ImGuiKey_Enter, ImGuiKey_KeypadEnter, ImGuiKey_UpArrow, ImGuiKey_DownArrow}) {
        if (ImGui::IsKeyPressed(key)) {
            return true;
        }
    }

    return false;
}


//
//	TextEditor::Autocomplete::start
//

void TextEditor::Autocomplete::start(Cursors& cursors) {
    // request start of autocomplete mode (can't be done here as the Dear ImGui context might not be right)
    requestActivation = true;
    currentLocation = cursors.getMain().getSelectionEnd();
    activationTime = std::chrono::system_clock::now() + configuration.triggerDelay;
}


//
//	TextEditor::Autocomplete::updateState
//

void TextEditor::Autocomplete::updateState(Document& document, const Language* language) {
    state.searchTerm = document.getSectionText(startLocation, currentLocation);

    if (currentLocation.column == 0) {
        state.inIdentifier = false;
        state.inNumber = false;

        auto lineState = document[currentLocation.line].state;
        state.inComment = lineState == State::inComment;

        state.inString =
        lineState == State::inDoubleQuotedString ||
        lineState == State::inSingleQuotedString||
        lineState == State::inOtherString ||
        lineState == State::inOtherStringAlt;

    } else {
        auto color = document.getColor(Coordinate(currentLocation.line, currentLocation.column - 1));
        state.inIdentifier = color == Color::identifier || color == Color::knownIdentifier;
        state.inNumber = color == Color::number;
        state.inComment = color == Color::comment;
        state.inString = color == Color::string;
    }

    state.line = currentLocation.line;
    state.searchTermStartColumn = startLocation.column;
    state.searchTermStartIndex = document.getIndex(startLocation);
    state.searchTermEndColumn = currentLocation.column;
    state.searchTermEndIndex= document.getIndex(currentLocation);

    state.language = language;
    state.userData = configuration.userData;
}


//
//	TextEditor::Autocomplete::refreshSuggestions
//

void TextEditor::Autocomplete::refreshSuggestions() {
    // populate suggestion list through callback (or clear it if there is none)
    if (configuration.callback) {
        configuration.callback(state);

    } else {
        state.suggestions.clear();
    }

    currentSelection = 0;
}


//
//	TextEditor::Trie::insert
//

void TextEditor::Trie::insert(const std::string_view& word) {
    auto node = root.get();
    auto end = word.end();
    auto i = TextEditor::CodePoint::skipBOM(word.begin(), end);

    while (i < end) {
        ImWchar codepoint;
        i = TextEditor::CodePoint::read(i, end, &codepoint);

        if (node->children.find(codepoint) == node->children.end()) {
            node->children[codepoint] = std::make_unique<Node>();
        }

        node = node->children[codepoint].get();
    }

    node->word = word;
}


//
//	TextEditor::Trie::findSuggestions
//

void TextEditor::Trie::findSuggestions(std::vector<std::string>& suggestions, const std::string_view& searchTerm, size_t limit, size_t maxSkippedLetters) {
    // clear result vector
    maxSkip = maxSkippedLetters;
    suggestions.clear();

    // don't even try if search term is empty
    if (searchTerm.size() != 0) {
        // convert search term into vector of code blocks
        searchCodepoints.clear();
        auto end = searchTerm.end();
        auto i = TextEditor::CodePoint::skipBOM(searchTerm.begin(), end);

        while (i < end) {
            ImWchar codepoint;
            i = TextEditor::CodePoint::read(i, end, &codepoint);
            searchCodepoints.emplace_back(codepoint);
        }

        // recursively evaluate nodes
        candidates.clear();
        evaluateNode(root.get(), 0, 0, maxSkip);

        // did we find anything?
        if (candidates.size()) {
            // sort candidates by cost
            std::sort(candidates.begin(), candidates.end());

            // remove duplicates which are caused by mutiple paths based on skips
            auto last = std::unique(candidates.begin(), candidates.end());
            candidates.erase(last, candidates.end());

            // populate suggestions (applying limit)
            auto size = std::min(static_cast<size_t>(limit), candidates.size());

            for (size_t j = 0; j < size; j++) {
                suggestions.emplace_back(candidates[j].node->word);
            }
        }
    }
}


//
//	TextEditor::Trie::evaluateNode
//

void TextEditor::Trie::evaluateNode(const Node* node, size_t index, size_t cost, size_t skip) {
    // see if that is one of our children (check both lower and uppercase matches)
    ImWchar codepointLower = TextEditor::CodePoint::toLower(searchCodepoints[index]);
    Node* childLower = nullptr;

    if (node->children.find(codepointLower) != node->children.end()) {
        // codepoint found, is this the last one in our searchTerm?
        childLower = node->children.at(codepointLower).get();

        if (index == searchCodepoints.size() - 1) {
            // yes, add candidate words to results
            addCandidates(childLower, cost);

        } else {
            // no, try to find the rest
            evaluateNode(childLower, index + 1, cost, maxSkip);
        }
    }

    ImWchar codepointUpper = TextEditor::CodePoint::toUpper(searchCodepoints[index]);
    Node* childUpper = nullptr;

    if (node->children.find(codepointUpper) != node->children.end()) {
        // codepoint found, is this the last one in our searchTerm?
        childUpper = node->children.at(codepointUpper).get();

        if (index == searchCodepoints.size() - 1) {
            // yes, add candidate words to results
            addCandidates(childUpper, cost);

        } else {
            // no, try to find the rest
            evaluateNode(childUpper, index + 1, cost, maxSkip);
        }
    }

    // also try children to support detection of missing letters (if we haven't skipped too many entries yet)
    if (skip) {
        for (auto const& [key, value] : node->children) {
            auto next = value.get();

            if (next != childLower && next != childUpper) {
                evaluateNode(next, index, cost + 1, skip - 1);
            }
        }
    }
}


//
//	TextEditor::Trie::addCandidates
//

void TextEditor::Trie::addCandidates(const Node* node, size_t cost) {
    if (node->word.size()) {
        candidates.emplace_back(node, cost);
    }

    for (auto const& [key, value] : node->children) {
        addCandidates(value.get(), cost + 1);
    }
}


//
//	Range table types
//

template <typename T>
struct Range {
    T low;
    T high;
    T stride;
};

using Range16 = Range<ImWchar16>;
using Range32 = Range<ImWchar32>;

template <typename T>
struct CaseRange {
    T low;
    T high;
    int32_t toUpper;
    int32_t toLower;
};

using CaseRange16 = CaseRange<char16_t>;
using CaseRange32 = CaseRange<char32_t>;


//
//	letters16
//

static Range16 letters16[] = {
    {0x0041, 0x005a, 0x0001}, {0x0061, 0x007a, 0x0001}, {0x00aa, 0x00b5, 0x000b}, {0x00ba, 0x00c0, 0x0006},
    {0x00c1, 0x00d6, 0x0001}, {0x00d8, 0x00f6, 0x0001}, {0x00f8, 0x02c1, 0x0001}, {0x02c6, 0x02d1, 0x0001},
    {0x02e0, 0x02e4, 0x0001}, {0x02ec, 0x02ee, 0x0002}, {0x0370, 0x0374, 0x0001}, {0x0376, 0x0377, 0x0001},
    {0x037a, 0x037d, 0x0001}, {0x037f, 0x0386, 0x0007}, {0x0388, 0x038a, 0x0001}, {0x038c, 0x038e, 0x0002},
    {0x038f, 0x03a1, 0x0001}, {0x03a3, 0x03f5, 0x0001}, {0x03f7, 0x0481, 0x0001}, {0x048a, 0x052f, 0x0001},
    {0x0531, 0x0556, 0x0001}, {0x0559, 0x0560, 0x0007}, {0x0561, 0x0588, 0x0001}, {0x05d0, 0x05ea, 0x0001},
    {0x05ef, 0x05f2, 0x0001}, {0x0620, 0x064a, 0x0001}, {0x066e, 0x066f, 0x0001}, {0x0671, 0x06d3, 0x0001},
    {0x06d5, 0x06e5, 0x0010}, {0x06e6, 0x06ee, 0x0008}, {0x06ef, 0x06fa, 0x000b}, {0x06fb, 0x06fc, 0x0001},
    {0x06ff, 0x0710, 0x0011}, {0x0712, 0x072f, 0x0001}, {0x074d, 0x07a5, 0x0001}, {0x07b1, 0x07ca, 0x0019},
    {0x07cb, 0x07ea, 0x0001}, {0x07f4, 0x07f5, 0x0001}, {0x07fa, 0x0800, 0x0006}, {0x0801, 0x0815, 0x0001},
    {0x081a, 0x0824, 0x000a}, {0x0828, 0x0840, 0x0018}, {0x0841, 0x0858, 0x0001}, {0x0860, 0x086a, 0x0001},
    {0x0870, 0x0887, 0x0001}, {0x0889, 0x088f, 0x0001}, {0x08a0, 0x08c9, 0x0001}, {0x0904, 0x0939, 0x0001},
    {0x093d, 0x0950, 0x0013}, {0x0958, 0x0961, 0x0001}, {0x0971, 0x0980, 0x0001}, {0x0985, 0x098c, 0x0001},
    {0x098f, 0x0990, 0x0001}, {0x0993, 0x09a8, 0x0001}, {0x09aa, 0x09b0, 0x0001}, {0x09b2, 0x09b6, 0x0004},
    {0x09b7, 0x09b9, 0x0001}, {0x09bd, 0x09ce, 0x0011}, {0x09dc, 0x09dd, 0x0001}, {0x09df, 0x09e1, 0x0001},
    {0x09f0, 0x09f1, 0x0001}, {0x09fc, 0x0a05, 0x0009}, {0x0a06, 0x0a0a, 0x0001}, {0x0a0f, 0x0a10, 0x0001},
    {0x0a13, 0x0a28, 0x0001}, {0x0a2a, 0x0a30, 0x0001}, {0x0a32, 0x0a33, 0x0001}, {0x0a35, 0x0a36, 0x0001},
    {0x0a38, 0x0a39, 0x0001}, {0x0a59, 0x0a5c, 0x0001}, {0x0a5e, 0x0a72, 0x0014}, {0x0a73, 0x0a74, 0x0001},
    {0x0a85, 0x0a8d, 0x0001}, {0x0a8f, 0x0a91, 0x0001}, {0x0a93, 0x0aa8, 0x0001}, {0x0aaa, 0x0ab0, 0x0001},
    {0x0ab2, 0x0ab3, 0x0001}, {0x0ab5, 0x0ab9, 0x0001}, {0x0abd, 0x0ad0, 0x0013}, {0x0ae0, 0x0ae1, 0x0001},
    {0x0af9, 0x0b05, 0x000c}, {0x0b06, 0x0b0c, 0x0001}, {0x0b0f, 0x0b10, 0x0001}, {0x0b13, 0x0b28, 0x0001},
    {0x0b2a, 0x0b30, 0x0001}, {0x0b32, 0x0b33, 0x0001}, {0x0b35, 0x0b39, 0x0001}, {0x0b3d, 0x0b5c, 0x001f},
    {0x0b5d, 0x0b5f, 0x0002}, {0x0b60, 0x0b61, 0x0001}, {0x0b71, 0x0b83, 0x0012}, {0x0b85, 0x0b8a, 0x0001},
    {0x0b8e, 0x0b90, 0x0001}, {0x0b92, 0x0b95, 0x0001}, {0x0b99, 0x0b9a, 0x0001}, {0x0b9c, 0x0b9e, 0x0002},
    {0x0b9f, 0x0ba3, 0x0004}, {0x0ba4, 0x0ba8, 0x0004}, {0x0ba9, 0x0baa, 0x0001}, {0x0bae, 0x0bb9, 0x0001},
    {0x0bd0, 0x0c05, 0x0035}, {0x0c06, 0x0c0c, 0x0001}, {0x0c0e, 0x0c10, 0x0001}, {0x0c12, 0x0c28, 0x0001},
    {0x0c2a, 0x0c39, 0x0001}, {0x0c3d, 0x0c58, 0x001b}, {0x0c59, 0x0c5a, 0x0001}, {0x0c5c, 0x0c5d, 0x0001},
    {0x0c60, 0x0c61, 0x0001}, {0x0c80, 0x0c85, 0x0005}, {0x0c86, 0x0c8c, 0x0001}, {0x0c8e, 0x0c90, 0x0001},
    {0x0c92, 0x0ca8, 0x0001}, {0x0caa, 0x0cb3, 0x0001}, {0x0cb5, 0x0cb9, 0x0001}, {0x0cbd, 0x0cdc, 0x001f},
    {0x0cdd, 0x0cde, 0x0001}, {0x0ce0, 0x0ce1, 0x0001}, {0x0cf1, 0x0cf2, 0x0001}, {0x0d04, 0x0d0c, 0x0001},
    {0x0d0e, 0x0d10, 0x0001}, {0x0d12, 0x0d3a, 0x0001}, {0x0d3d, 0x0d4e, 0x0011}, {0x0d54, 0x0d56, 0x0001},
    {0x0d5f, 0x0d61, 0x0001}, {0x0d7a, 0x0d7f, 0x0001}, {0x0d85, 0x0d96, 0x0001}, {0x0d9a, 0x0db1, 0x0001},
    {0x0db3, 0x0dbb, 0x0001}, {0x0dbd, 0x0dc0, 0x0003}, {0x0dc1, 0x0dc6, 0x0001}, {0x0e01, 0x0e30, 0x0001},
    {0x0e32, 0x0e33, 0x0001}, {0x0e40, 0x0e46, 0x0001}, {0x0e81, 0x0e82, 0x0001}, {0x0e84, 0x0e86, 0x0002},
    {0x0e87, 0x0e8a, 0x0001}, {0x0e8c, 0x0ea3, 0x0001}, {0x0ea5, 0x0ea7, 0x0002}, {0x0ea8, 0x0eb0, 0x0001},
    {0x0eb2, 0x0eb3, 0x0001}, {0x0ebd, 0x0ec0, 0x0003}, {0x0ec1, 0x0ec4, 0x0001}, {0x0ec6, 0x0edc, 0x0016},
    {0x0edd, 0x0edf, 0x0001}, {0x0f00, 0x0f40, 0x0040}, {0x0f41, 0x0f47, 0x0001}, {0x0f49, 0x0f6c, 0x0001},
    {0x0f88, 0x0f8c, 0x0001}, {0x1000, 0x102a, 0x0001}, {0x103f, 0x1050, 0x0011}, {0x1051, 0x1055, 0x0001},
    {0x105a, 0x105d, 0x0001}, {0x1061, 0x1065, 0x0004}, {0x1066, 0x106e, 0x0008}, {0x106f, 0x1070, 0x0001},
    {0x1075, 0x1081, 0x0001}, {0x108e, 0x10a0, 0x0012}, {0x10a1, 0x10c5, 0x0001}, {0x10c7, 0x10cd, 0x0006},
    {0x10d0, 0x10fa, 0x0001}, {0x10fc, 0x1248, 0x0001}, {0x124a, 0x124d, 0x0001}, {0x1250, 0x1256, 0x0001},
    {0x1258, 0x125a, 0x0002}, {0x125b, 0x125d, 0x0001}, {0x1260, 0x1288, 0x0001}, {0x128a, 0x128d, 0x0001},
    {0x1290, 0x12b0, 0x0001}, {0x12b2, 0x12b5, 0x0001}, {0x12b8, 0x12be, 0x0001}, {0x12c0, 0x12c2, 0x0002},
    {0x12c3, 0x12c5, 0x0001}, {0x12c8, 0x12d6, 0x0001}, {0x12d8, 0x1310, 0x0001}, {0x1312, 0x1315, 0x0001},
    {0x1318, 0x135a, 0x0001}, {0x1380, 0x138f, 0x0001}, {0x13a0, 0x13f5, 0x0001}, {0x13f8, 0x13fd, 0x0001},
    {0x1401, 0x166c, 0x0001}, {0x166f, 0x167f, 0x0001}, {0x1681, 0x169a, 0x0001}, {0x16a0, 0x16ea, 0x0001},
    {0x16f1, 0x16f8, 0x0001}, {0x1700, 0x1711, 0x0001}, {0x171f, 0x1731, 0x0001}, {0x1740, 0x1751, 0x0001},
    {0x1760, 0x176c, 0x0001}, {0x176e, 0x1770, 0x0001}, {0x1780, 0x17b3, 0x0001}, {0x17d7, 0x17dc, 0x0005},
    {0x1820, 0x1878, 0x0001}, {0x1880, 0x1884, 0x0001}, {0x1887, 0x18a8, 0x0001}, {0x18aa, 0x18b0, 0x0006},
    {0x18b1, 0x18f5, 0x0001}, {0x1900, 0x191e, 0x0001}, {0x1950, 0x196d, 0x0001}, {0x1970, 0x1974, 0x0001},
    {0x1980, 0x19ab, 0x0001}, {0x19b0, 0x19c9, 0x0001}, {0x1a00, 0x1a16, 0x0001}, {0x1a20, 0x1a54, 0x0001},
    {0x1aa7, 0x1b05, 0x005e}, {0x1b06, 0x1b33, 0x0001}, {0x1b45, 0x1b4c, 0x0001}, {0x1b83, 0x1ba0, 0x0001},
    {0x1bae, 0x1baf, 0x0001}, {0x1bba, 0x1be5, 0x0001}, {0x1c00, 0x1c23, 0x0001}, {0x1c4d, 0x1c4f, 0x0001},
    {0x1c5a, 0x1c7d, 0x0001}, {0x1c80, 0x1c8a, 0x0001}, {0x1c90, 0x1cba, 0x0001}, {0x1cbd, 0x1cbf, 0x0001},
    {0x1ce9, 0x1cec, 0x0001}, {0x1cee, 0x1cf3, 0x0001}, {0x1cf5, 0x1cf6, 0x0001}, {0x1cfa, 0x1d00, 0x0006},
    {0x1d01, 0x1dbf, 0x0001}, {0x1e00, 0x1f15, 0x0001}, {0x1f18, 0x1f1d, 0x0001}, {0x1f20, 0x1f45, 0x0001},
    {0x1f48, 0x1f4d, 0x0001}, {0x1f50, 0x1f57, 0x0001}, {0x1f59, 0x1f5f, 0x0002}, {0x1f60, 0x1f7d, 0x0001},
    {0x1f80, 0x1fb4, 0x0001}, {0x1fb6, 0x1fbc, 0x0001}, {0x1fbe, 0x1fc2, 0x0004}, {0x1fc3, 0x1fc4, 0x0001},
    {0x1fc6, 0x1fcc, 0x0001}, {0x1fd0, 0x1fd3, 0x0001}, {0x1fd6, 0x1fdb, 0x0001}, {0x1fe0, 0x1fec, 0x0001},
    {0x1ff2, 0x1ff4, 0x0001}, {0x1ff6, 0x1ffc, 0x0001}, {0x2071, 0x207f, 0x000e}, {0x2090, 0x209c, 0x0001},
    {0x2102, 0x2107, 0x0005}, {0x210a, 0x2113, 0x0001}, {0x2115, 0x2119, 0x0004}, {0x211a, 0x211d, 0x0001},
    {0x2124, 0x212a, 0x0002}, {0x212b, 0x212d, 0x0001}, {0x212f, 0x2139, 0x0001}, {0x213c, 0x213f, 0x0001},
    {0x2145, 0x2149, 0x0001}, {0x214e, 0x2183, 0x0035}, {0x2184, 0x2c00, 0x0a7c}, {0x2c01, 0x2ce4, 0x0001},
    {0x2ceb, 0x2cee, 0x0001}, {0x2cf2, 0x2cf3, 0x0001}, {0x2d00, 0x2d25, 0x0001}, {0x2d27, 0x2d2d, 0x0006},
    {0x2d30, 0x2d67, 0x0001}, {0x2d6f, 0x2d80, 0x0011}, {0x2d81, 0x2d96, 0x0001}, {0x2da0, 0x2da6, 0x0001},
    {0x2da8, 0x2dae, 0x0001}, {0x2db0, 0x2db6, 0x0001}, {0x2db8, 0x2dbe, 0x0001}, {0x2dc0, 0x2dc6, 0x0001},
    {0x2dc8, 0x2dce, 0x0001}, {0x2dd0, 0x2dd6, 0x0001}, {0x2dd8, 0x2dde, 0x0001}, {0x2e2f, 0x3005, 0x01d6},
    {0x3006, 0x3031, 0x002b}, {0x3032, 0x3035, 0x0001}, {0x303b, 0x303c, 0x0001}, {0x3041, 0x3096, 0x0001},
    {0x309d, 0x309f, 0x0001}, {0x30a1, 0x30fa, 0x0001}, {0x30fc, 0x30ff, 0x0001}, {0x3105, 0x312f, 0x0001},
    {0x3131, 0x318e, 0x0001}, {0x31a0, 0x31bf, 0x0001}, {0x31f0, 0x31ff, 0x0001}, {0x3400, 0x4dbf, 0x19bf},
    {0x4e00, 0x9fff, 0x51ff}, {0xa000, 0xa48c, 0x0001}, {0xa4d0, 0xa4fd, 0x0001}, {0xa500, 0xa60c, 0x0001},
    {0xa610, 0xa61f, 0x0001}, {0xa62a, 0xa62b, 0x0001}, {0xa640, 0xa66e, 0x0001}, {0xa67f, 0xa69d, 0x0001},
    {0xa6a0, 0xa6e5, 0x0001}, {0xa717, 0xa71f, 0x0001}, {0xa722, 0xa788, 0x0001}, {0xa78b, 0xa7dc, 0x0001},
    {0xa7f1, 0xa801, 0x0001}, {0xa803, 0xa805, 0x0001}, {0xa807, 0xa80a, 0x0001}, {0xa80c, 0xa822, 0x0001},
    {0xa840, 0xa873, 0x0001}, {0xa882, 0xa8b3, 0x0001}, {0xa8f2, 0xa8f7, 0x0001}, {0xa8fb, 0xa8fd, 0x0002},
    {0xa8fe, 0xa90a, 0x000c}, {0xa90b, 0xa925, 0x0001}, {0xa930, 0xa946, 0x0001}, {0xa960, 0xa97c, 0x0001},
    {0xa984, 0xa9b2, 0x0001}, {0xa9cf, 0xa9e0, 0x0011}, {0xa9e1, 0xa9e4, 0x0001}, {0xa9e6, 0xa9ef, 0x0001},
    {0xa9fa, 0xa9fe, 0x0001}, {0xaa00, 0xaa28, 0x0001}, {0xaa40, 0xaa42, 0x0001}, {0xaa44, 0xaa4b, 0x0001},
    {0xaa60, 0xaa76, 0x0001}, {0xaa7a, 0xaa7e, 0x0004}, {0xaa7f, 0xaaaf, 0x0001}, {0xaab1, 0xaab5, 0x0004},
    {0xaab6, 0xaab9, 0x0003}, {0xaaba, 0xaabd, 0x0001}, {0xaac0, 0xaac2, 0x0002}, {0xaadb, 0xaadd, 0x0001},
    {0xaae0, 0xaaea, 0x0001}, {0xaaf2, 0xaaf4, 0x0001}, {0xab01, 0xab06, 0x0001}, {0xab09, 0xab0e, 0x0001},
    {0xab11, 0xab16, 0x0001}, {0xab20, 0xab26, 0x0001}, {0xab28, 0xab2e, 0x0001}, {0xab30, 0xab5a, 0x0001},
    {0xab5c, 0xab69, 0x0001}, {0xab70, 0xabe2, 0x0001}, {0xac00, 0xd7a3, 0x2ba3}, {0xd7b0, 0xd7c6, 0x0001},
    {0xd7cb, 0xd7fb, 0x0001}, {0xf900, 0xfa6d, 0x0001}, {0xfa70, 0xfad9, 0x0001}, {0xfb00, 0xfb06, 0x0001},
    {0xfb13, 0xfb17, 0x0001}, {0xfb1d, 0xfb1f, 0x0002}, {0xfb20, 0xfb28, 0x0001}, {0xfb2a, 0xfb36, 0x0001},
    {0xfb38, 0xfb3c, 0x0001}, {0xfb3e, 0xfb40, 0x0002}, {0xfb41, 0xfb43, 0x0002}, {0xfb44, 0xfb46, 0x0002},
    {0xfb47, 0xfbb1, 0x0001}, {0xfbd3, 0xfd3d, 0x0001}, {0xfd50, 0xfd8f, 0x0001}, {0xfd92, 0xfdc7, 0x0001},
    {0xfdf0, 0xfdfb, 0x0001}, {0xfe70, 0xfe74, 0x0001}, {0xfe76, 0xfefc, 0x0001}, {0xff21, 0xff3a, 0x0001},
    {0xff41, 0xff5a, 0x0001}, {0xff66, 0xffbe, 0x0001}, {0xffc2, 0xffc7, 0x0001}, {0xffca, 0xffcf, 0x0001},
    {0xffd2, 0xffd7, 0x0001}, {0xffda, 0xffdc, 0x0001}
};


//
//	letters32
//

#if defined(IMGUI_USE_WCHAR32)

static Range32 letters32[] = {
    {0x10000, 0x1000b, 0x0001}, {0x1000d, 0x10026, 0x0001}, {0x10028, 0x1003a, 0x0001}, {0x1003c, 0x1003d, 0x0001},
    {0x1003f, 0x1004d, 0x0001}, {0x10050, 0x1005d, 0x0001}, {0x10080, 0x100fa, 0x0001}, {0x10280, 0x1029c, 0x0001},
    {0x102a0, 0x102d0, 0x0001}, {0x10300, 0x1031f, 0x0001}, {0x1032d, 0x10340, 0x0001}, {0x10342, 0x10349, 0x0001},
    {0x10350, 0x10375, 0x0001}, {0x10380, 0x1039d, 0x0001}, {0x103a0, 0x103c3, 0x0001}, {0x103c8, 0x103cf, 0x0001},
    {0x10400, 0x1049d, 0x0001}, {0x104b0, 0x104d3, 0x0001}, {0x104d8, 0x104fb, 0x0001}, {0x10500, 0x10527, 0x0001},
    {0x10530, 0x10563, 0x0001}, {0x10570, 0x1057a, 0x0001}, {0x1057c, 0x1058a, 0x0001}, {0x1058c, 0x10592, 0x0001},
    {0x10594, 0x10595, 0x0001}, {0x10597, 0x105a1, 0x0001}, {0x105a3, 0x105b1, 0x0001}, {0x105b3, 0x105b9, 0x0001},
    {0x105bb, 0x105bc, 0x0001}, {0x105c0, 0x105f3, 0x0001}, {0x10600, 0x10736, 0x0001}, {0x10740, 0x10755, 0x0001},
    {0x10760, 0x10767, 0x0001}, {0x10780, 0x10785, 0x0001}, {0x10787, 0x107b0, 0x0001}, {0x107b2, 0x107ba, 0x0001},
    {0x10800, 0x10805, 0x0001}, {0x10808, 0x1080a, 0x0002}, {0x1080b, 0x10835, 0x0001}, {0x10837, 0x10838, 0x0001},
    {0x1083c, 0x1083f, 0x0003}, {0x10840, 0x10855, 0x0001}, {0x10860, 0x10876, 0x0001}, {0x10880, 0x1089e, 0x0001},
    {0x108e0, 0x108f2, 0x0001}, {0x108f4, 0x108f5, 0x0001}, {0x10900, 0x10915, 0x0001}, {0x10920, 0x10939, 0x0001},
    {0x10940, 0x10959, 0x0001}, {0x10980, 0x109b7, 0x0001}, {0x109be, 0x109bf, 0x0001}, {0x10a00, 0x10a10, 0x0010},
    {0x10a11, 0x10a13, 0x0001}, {0x10a15, 0x10a17, 0x0001}, {0x10a19, 0x10a35, 0x0001}, {0x10a60, 0x10a7c, 0x0001},
    {0x10a80, 0x10a9c, 0x0001}, {0x10ac0, 0x10ac7, 0x0001}, {0x10ac9, 0x10ae4, 0x0001}, {0x10b00, 0x10b35, 0x0001},
    {0x10b40, 0x10b55, 0x0001}, {0x10b60, 0x10b72, 0x0001}, {0x10b80, 0x10b91, 0x0001}, {0x10c00, 0x10c48, 0x0001},
    {0x10c80, 0x10cb2, 0x0001}, {0x10cc0, 0x10cf2, 0x0001}, {0x10d00, 0x10d23, 0x0001}, {0x10d4a, 0x10d65, 0x0001},
    {0x10d6f, 0x10d85, 0x0001}, {0x10e80, 0x10ea9, 0x0001}, {0x10eb0, 0x10eb1, 0x0001}, {0x10ec2, 0x10ec7, 0x0001},
    {0x10f00, 0x10f1c, 0x0001}, {0x10f27, 0x10f30, 0x0009}, {0x10f31, 0x10f45, 0x0001}, {0x10f70, 0x10f81, 0x0001},
    {0x10fb0, 0x10fc4, 0x0001}, {0x10fe0, 0x10ff6, 0x0001}, {0x11003, 0x11037, 0x0001}, {0x11071, 0x11072, 0x0001},
    {0x11075, 0x11083, 0x000e}, {0x11084, 0x110af, 0x0001}, {0x110d0, 0x110e8, 0x0001}, {0x11103, 0x11126, 0x0001},
    {0x11144, 0x11147, 0x0003}, {0x11150, 0x11172, 0x0001}, {0x11176, 0x11183, 0x000d}, {0x11184, 0x111b2, 0x0001},
    {0x111c1, 0x111c4, 0x0001}, {0x111da, 0x111dc, 0x0002}, {0x11200, 0x11211, 0x0001}, {0x11213, 0x1122b, 0x0001},
    {0x1123f, 0x11240, 0x0001}, {0x11280, 0x11286, 0x0001}, {0x11288, 0x1128a, 0x0002}, {0x1128b, 0x1128d, 0x0001},
    {0x1128f, 0x1129d, 0x0001}, {0x1129f, 0x112a8, 0x0001}, {0x112b0, 0x112de, 0x0001}, {0x11305, 0x1130c, 0x0001},
    {0x1130f, 0x11310, 0x0001}, {0x11313, 0x11328, 0x0001}, {0x1132a, 0x11330, 0x0001}, {0x11332, 0x11333, 0x0001},
    {0x11335, 0x11339, 0x0001}, {0x1133d, 0x11350, 0x0013}, {0x1135d, 0x11361, 0x0001}, {0x11380, 0x11389, 0x0001},
    {0x1138b, 0x1138e, 0x0003}, {0x11390, 0x113b5, 0x0001}, {0x113b7, 0x113d1, 0x001a}, {0x113d3, 0x11400, 0x002d},
    {0x11401, 0x11434, 0x0001}, {0x11447, 0x1144a, 0x0001}, {0x1145f, 0x11461, 0x0001}, {0x11480, 0x114af, 0x0001},
    {0x114c4, 0x114c5, 0x0001}, {0x114c7, 0x11580, 0x00b9}, {0x11581, 0x115ae, 0x0001}, {0x115d8, 0x115db, 0x0001},
    {0x11600, 0x1162f, 0x0001}, {0x11644, 0x11680, 0x003c}, {0x11681, 0x116aa, 0x0001}, {0x116b8, 0x11700, 0x0048},
    {0x11701, 0x1171a, 0x0001}, {0x11740, 0x11746, 0x0001}, {0x11800, 0x1182b, 0x0001}, {0x118a0, 0x118df, 0x0001},
    {0x118ff, 0x11906, 0x0001}, {0x11909, 0x1190c, 0x0003}, {0x1190d, 0x11913, 0x0001}, {0x11915, 0x11916, 0x0001},
    {0x11918, 0x1192f, 0x0001}, {0x1193f, 0x11941, 0x0002}, {0x119a0, 0x119a7, 0x0001}, {0x119aa, 0x119d0, 0x0001},
    {0x119e1, 0x119e3, 0x0002}, {0x11a00, 0x11a0b, 0x000b}, {0x11a0c, 0x11a32, 0x0001}, {0x11a3a, 0x11a50, 0x0016},
    {0x11a5c, 0x11a89, 0x0001}, {0x11a9d, 0x11ab0, 0x0013}, {0x11ab1, 0x11af8, 0x0001}, {0x11bc0, 0x11be0, 0x0001},
    {0x11c00, 0x11c08, 0x0001}, {0x11c0a, 0x11c2e, 0x0001}, {0x11c40, 0x11c72, 0x0032}, {0x11c73, 0x11c8f, 0x0001},
    {0x11d00, 0x11d06, 0x0001}, {0x11d08, 0x11d09, 0x0001}, {0x11d0b, 0x11d30, 0x0001}, {0x11d46, 0x11d60, 0x001a},
    {0x11d61, 0x11d65, 0x0001}, {0x11d67, 0x11d68, 0x0001}, {0x11d6a, 0x11d89, 0x0001}, {0x11d98, 0x11db0, 0x0018},
    {0x11db1, 0x11ddb, 0x0001}, {0x11ee0, 0x11ef2, 0x0001}, {0x11f02, 0x11f04, 0x0002}, {0x11f05, 0x11f10, 0x0001},
    {0x11f12, 0x11f33, 0x0001}, {0x11fb0, 0x12000, 0x0050}, {0x12001, 0x12399, 0x0001}, {0x12480, 0x12543, 0x0001},
    {0x12f90, 0x12ff0, 0x0001}, {0x13000, 0x1342f, 0x0001}, {0x13441, 0x13446, 0x0001}, {0x13460, 0x143fa, 0x0001},
    {0x14400, 0x14646, 0x0001}, {0x16100, 0x1611d, 0x0001}, {0x16800, 0x16a38, 0x0001}, {0x16a40, 0x16a5e, 0x0001},
    {0x16a70, 0x16abe, 0x0001}, {0x16ad0, 0x16aed, 0x0001}, {0x16b00, 0x16b2f, 0x0001}, {0x16b40, 0x16b43, 0x0001},
    {0x16b63, 0x16b77, 0x0001}, {0x16b7d, 0x16b8f, 0x0001}, {0x16d40, 0x16d6c, 0x0001}, {0x16e40, 0x16e7f, 0x0001},
    {0x16ea0, 0x16eb8, 0x0001}, {0x16ebb, 0x16ed3, 0x0001}, {0x16f00, 0x16f4a, 0x0001}, {0x16f50, 0x16f93, 0x0043},
    {0x16f94, 0x16f9f, 0x0001}, {0x16fe0, 0x16fe1, 0x0001}, {0x16fe3, 0x16ff2, 0x000f}, {0x16ff3, 0x17000, 0x000d},
    {0x187ff, 0x18cd5, 0x0001}, {0x18cff, 0x18d00, 0x0001}, {0x18d1e, 0x18d80, 0x0062}, {0x18d81, 0x18df2, 0x0001},
    {0x1aff0, 0x1aff3, 0x0001}, {0x1aff5, 0x1affb, 0x0001}, {0x1affd, 0x1affe, 0x0001}, {0x1b000, 0x1b122, 0x0001},
    {0x1b132, 0x1b150, 0x001e}, {0x1b151, 0x1b152, 0x0001}, {0x1b155, 0x1b164, 0x000f}, {0x1b165, 0x1b167, 0x0001},
    {0x1b170, 0x1b2fb, 0x0001}, {0x1bc00, 0x1bc6a, 0x0001}, {0x1bc70, 0x1bc7c, 0x0001}, {0x1bc80, 0x1bc88, 0x0001},
    {0x1bc90, 0x1bc99, 0x0001}, {0x1d400, 0x1d454, 0x0001}, {0x1d456, 0x1d49c, 0x0001}, {0x1d49e, 0x1d49f, 0x0001},
    {0x1d4a2, 0x1d4a5, 0x0003}, {0x1d4a6, 0x1d4a9, 0x0003}, {0x1d4aa, 0x1d4ac, 0x0001}, {0x1d4ae, 0x1d4b9, 0x0001},
    {0x1d4bb, 0x1d4bd, 0x0002}, {0x1d4be, 0x1d4c3, 0x0001}, {0x1d4c5, 0x1d505, 0x0001}, {0x1d507, 0x1d50a, 0x0001},
    {0x1d50d, 0x1d514, 0x0001}, {0x1d516, 0x1d51c, 0x0001}, {0x1d51e, 0x1d539, 0x0001}, {0x1d53b, 0x1d53e, 0x0001},
    {0x1d540, 0x1d544, 0x0001}, {0x1d546, 0x1d54a, 0x0004}, {0x1d54b, 0x1d550, 0x0001}, {0x1d552, 0x1d6a5, 0x0001},
    {0x1d6a8, 0x1d6c0, 0x0001}, {0x1d6c2, 0x1d6da, 0x0001}, {0x1d6dc, 0x1d6fa, 0x0001}, {0x1d6fc, 0x1d714, 0x0001},
    {0x1d716, 0x1d734, 0x0001}, {0x1d736, 0x1d74e, 0x0001}, {0x1d750, 0x1d76e, 0x0001}, {0x1d770, 0x1d788, 0x0001},
    {0x1d78a, 0x1d7a8, 0x0001}, {0x1d7aa, 0x1d7c2, 0x0001}, {0x1d7c4, 0x1d7cb, 0x0001}, {0x1df00, 0x1df1e, 0x0001},
    {0x1df25, 0x1df2a, 0x0001}, {0x1e030, 0x1e06d, 0x0001}, {0x1e100, 0x1e12c, 0x0001}, {0x1e137, 0x1e13d, 0x0001},
    {0x1e14e, 0x1e290, 0x0142}, {0x1e291, 0x1e2ad, 0x0001}, {0x1e2c0, 0x1e2eb, 0x0001}, {0x1e4d0, 0x1e4eb, 0x0001},
    {0x1e5d0, 0x1e5ed, 0x0001}, {0x1e5f0, 0x1e6c0, 0x00d0}, {0x1e6c1, 0x1e6de, 0x0001}, {0x1e6e0, 0x1e6e2, 0x0001},
    {0x1e6e4, 0x1e6e5, 0x0001}, {0x1e6e7, 0x1e6ed, 0x0001}, {0x1e6f0, 0x1e6f4, 0x0001}, {0x1e6fe, 0x1e6ff, 0x0001},
    {0x1e7e0, 0x1e7e6, 0x0001}, {0x1e7e8, 0x1e7eb, 0x0001}, {0x1e7ed, 0x1e7ee, 0x0001}, {0x1e7f0, 0x1e7fe, 0x0001},
    {0x1e800, 0x1e8c4, 0x0001}, {0x1e900, 0x1e943, 0x0001}, {0x1e94b, 0x1ee00, 0x04b5}, {0x1ee01, 0x1ee03, 0x0001},
    {0x1ee05, 0x1ee1f, 0x0001}, {0x1ee21, 0x1ee22, 0x0001}, {0x1ee24, 0x1ee27, 0x0003}, {0x1ee29, 0x1ee32, 0x0001},
    {0x1ee34, 0x1ee37, 0x0001}, {0x1ee39, 0x1ee3b, 0x0002}, {0x1ee42, 0x1ee47, 0x0005}, {0x1ee49, 0x1ee4d, 0x0002},
    {0x1ee4e, 0x1ee4f, 0x0001}, {0x1ee51, 0x1ee52, 0x0001}, {0x1ee54, 0x1ee57, 0x0003}, {0x1ee59, 0x1ee61, 0x0002},
    {0x1ee62, 0x1ee64, 0x0002}, {0x1ee67, 0x1ee6a, 0x0001}, {0x1ee6c, 0x1ee72, 0x0001}, {0x1ee74, 0x1ee77, 0x0001},
    {0x1ee79, 0x1ee7c, 0x0001}, {0x1ee7e, 0x1ee80, 0x0002}, {0x1ee81, 0x1ee89, 0x0001}, {0x1ee8b, 0x1ee9b, 0x0001},
    {0x1eea1, 0x1eea3, 0x0001}, {0x1eea5, 0x1eea9, 0x0001}, {0x1eeab, 0x1eebb, 0x0001}, {0x20000, 0x2a6df, 0xa6df},
    {0x2a700, 0x2b73f, 0x103f}, {0x2b740, 0x2b81d, 0x00dd}, {0x2b820, 0x2cead, 0x168d}, {0x2ceb0, 0x2ebe0, 0x1d30},
    {0x2ebf0, 0x2ee5d, 0x026d}, {0x2f800, 0x2f8cf, 0x0001}
};

#endif


//
//	lower16
//

static Range16 lower16[] = {
    {0x0061, 0x007a, 0x0001}, {0x00b5, 0x00df, 0x002a}, {0x00e0, 0x00f6, 0x0001}, {0x00f8, 0x00ff, 0x0001},
    {0x0101, 0x0137, 0x0002}, {0x0138, 0x0148, 0x0002}, {0x0149, 0x0177, 0x0002}, {0x017a, 0x017e, 0x0002},
    {0x017f, 0x0180, 0x0001}, {0x0183, 0x0185, 0x0002}, {0x0188, 0x018c, 0x0004}, {0x018d, 0x0192, 0x0005},
    {0x0195, 0x0199, 0x0004}, {0x019a, 0x019b, 0x0001}, {0x019e, 0x01a1, 0x0003}, {0x01a3, 0x01a5, 0x0002},
    {0x01a8, 0x01aa, 0x0002}, {0x01ab, 0x01ad, 0x0002}, {0x01b0, 0x01b4, 0x0004}, {0x01b6, 0x01b9, 0x0003},
    {0x01ba, 0x01bd, 0x0003}, {0x01be, 0x01bf, 0x0001}, {0x01c6, 0x01cc, 0x0003}, {0x01ce, 0x01dc, 0x0002},
    {0x01dd, 0x01ef, 0x0002}, {0x01f0, 0x01f3, 0x0003}, {0x01f5, 0x01f9, 0x0004}, {0x01fb, 0x0233, 0x0002},
    {0x0234, 0x0239, 0x0001}, {0x023c, 0x023f, 0x0003}, {0x0240, 0x0242, 0x0002}, {0x0247, 0x024f, 0x0002},
    {0x0250, 0x0293, 0x0001}, {0x0296, 0x02af, 0x0001}, {0x0371, 0x0373, 0x0002}, {0x0377, 0x037b, 0x0004},
    {0x037c, 0x037d, 0x0001}, {0x0390, 0x03ac, 0x001c}, {0x03ad, 0x03ce, 0x0001}, {0x03d0, 0x03d1, 0x0001},
    {0x03d5, 0x03d7, 0x0001}, {0x03d9, 0x03ef, 0x0002}, {0x03f0, 0x03f3, 0x0001}, {0x03f5, 0x03fb, 0x0003},
    {0x03fc, 0x0430, 0x0034}, {0x0431, 0x045f, 0x0001}, {0x0461, 0x0481, 0x0002}, {0x048b, 0x04bf, 0x0002},
    {0x04c2, 0x04ce, 0x0002}, {0x04cf, 0x052f, 0x0002}, {0x0560, 0x0588, 0x0001}, {0x10d0, 0x10fa, 0x0001},
    {0x10fd, 0x10ff, 0x0001}, {0x13f8, 0x13fd, 0x0001}, {0x1c80, 0x1c88, 0x0001}, {0x1c8a, 0x1d00, 0x0076},
    {0x1d01, 0x1d2b, 0x0001}, {0x1d6b, 0x1d77, 0x0001}, {0x1d79, 0x1d9a, 0x0001}, {0x1e01, 0x1e95, 0x0002},
    {0x1e96, 0x1e9d, 0x0001}, {0x1e9f, 0x1eff, 0x0002}, {0x1f00, 0x1f07, 0x0001}, {0x1f10, 0x1f15, 0x0001},
    {0x1f20, 0x1f27, 0x0001}, {0x1f30, 0x1f37, 0x0001}, {0x1f40, 0x1f45, 0x0001}, {0x1f50, 0x1f57, 0x0001},
    {0x1f60, 0x1f67, 0x0001}, {0x1f70, 0x1f7d, 0x0001}, {0x1f80, 0x1f87, 0x0001}, {0x1f90, 0x1f97, 0x0001},
    {0x1fa0, 0x1fa7, 0x0001}, {0x1fb0, 0x1fb4, 0x0001}, {0x1fb6, 0x1fb7, 0x0001}, {0x1fbe, 0x1fc2, 0x0004},
    {0x1fc3, 0x1fc4, 0x0001}, {0x1fc6, 0x1fc7, 0x0001}, {0x1fd0, 0x1fd3, 0x0001}, {0x1fd6, 0x1fd7, 0x0001},
    {0x1fe0, 0x1fe7, 0x0001}, {0x1ff2, 0x1ff4, 0x0001}, {0x1ff6, 0x1ff7, 0x0001}, {0x210a, 0x210e, 0x0004},
    {0x210f, 0x2113, 0x0004}, {0x212f, 0x2139, 0x0005}, {0x213c, 0x213d, 0x0001}, {0x2146, 0x2149, 0x0001},
    {0x214e, 0x2184, 0x0036}, {0x2c30, 0x2c5f, 0x0001}, {0x2c61, 0x2c65, 0x0004}, {0x2c66, 0x2c6c, 0x0002},
    {0x2c71, 0x2c73, 0x0002}, {0x2c74, 0x2c76, 0x0002}, {0x2c77, 0x2c7b, 0x0001}, {0x2c81, 0x2ce3, 0x0002},
    {0x2ce4, 0x2cec, 0x0008}, {0x2cee, 0x2cf3, 0x0005}, {0x2d00, 0x2d25, 0x0001}, {0x2d27, 0x2d2d, 0x0006},
    {0xa641, 0xa66d, 0x0002}, {0xa681, 0xa69b, 0x0002}, {0xa723, 0xa72f, 0x0002}, {0xa730, 0xa731, 0x0001},
    {0xa733, 0xa771, 0x0002}, {0xa772, 0xa778, 0x0001}, {0xa77a, 0xa77c, 0x0002}, {0xa77f, 0xa787, 0x0002},
    {0xa78c, 0xa78e, 0x0002}, {0xa791, 0xa793, 0x0002}, {0xa794, 0xa795, 0x0001}, {0xa797, 0xa7a9, 0x0002},
    {0xa7af, 0xa7b5, 0x0006}, {0xa7b7, 0xa7c3, 0x0002}, {0xa7c8, 0xa7ca, 0x0002}, {0xa7cd, 0xa7db, 0x0002},
    {0xa7f6, 0xa7fa, 0x0004}, {0xab30, 0xab5a, 0x0001}, {0xab60, 0xab68, 0x0001}, {0xab70, 0xabbf, 0x0001},
    {0xfb00, 0xfb06, 0x0001}, {0xfb13, 0xfb17, 0x0001}, {0xff41, 0xff5a, 0x0001}
};


//
//	lower32
//

#if defined(IMGUI_USE_WCHAR32)

static Range32 lower32[] = {
    {0x10428, 0x1044f, 0x0001}, {0x104d8, 0x104fb, 0x0001}, {0x10597, 0x105a1, 0x0001}, {0x105a3, 0x105b1, 0x0001},
    {0x105b3, 0x105b9, 0x0001}, {0x105bb, 0x105bc, 0x0001}, {0x10cc0, 0x10cf2, 0x0001}, {0x10d70, 0x10d85, 0x0001},
    {0x118c0, 0x118df, 0x0001}, {0x16e60, 0x16e7f, 0x0001}, {0x16ebb, 0x16ed3, 0x0001}, {0x1d41a, 0x1d433, 0x0001},
    {0x1d44e, 0x1d454, 0x0001}, {0x1d456, 0x1d467, 0x0001}, {0x1d482, 0x1d49b, 0x0001}, {0x1d4b6, 0x1d4b9, 0x0001},
    {0x1d4bb, 0x1d4bd, 0x0002}, {0x1d4be, 0x1d4c3, 0x0001}, {0x1d4c5, 0x1d4cf, 0x0001}, {0x1d4ea, 0x1d503, 0x0001},
    {0x1d51e, 0x1d537, 0x0001}, {0x1d552, 0x1d56b, 0x0001}, {0x1d586, 0x1d59f, 0x0001}, {0x1d5ba, 0x1d5d3, 0x0001},
    {0x1d5ee, 0x1d607, 0x0001}, {0x1d622, 0x1d63b, 0x0001}, {0x1d656, 0x1d66f, 0x0001}, {0x1d68a, 0x1d6a5, 0x0001},
    {0x1d6c2, 0x1d6da, 0x0001}, {0x1d6dc, 0x1d6e1, 0x0001}, {0x1d6fc, 0x1d714, 0x0001}, {0x1d716, 0x1d71b, 0x0001},
    {0x1d736, 0x1d74e, 0x0001}, {0x1d750, 0x1d755, 0x0001}, {0x1d770, 0x1d788, 0x0001}, {0x1d78a, 0x1d78f, 0x0001},
    {0x1d7aa, 0x1d7c2, 0x0001}, {0x1d7c4, 0x1d7c9, 0x0001}, {0x1d7cb, 0x1df00, 0x0735}, {0x1df01, 0x1df09, 0x0001},
    {0x1df0b, 0x1df1e, 0x0001}, {0x1df25, 0x1df2a, 0x0001}, {0x1e922, 0x1e943, 0x0001}
};

#endif


//
//	upper16
//

static Range16 upper16[] = {
    {0x0041, 0x005a, 0x0001}, {0x00c0, 0x00d6, 0x0001}, {0x00d8, 0x00de, 0x0001}, {0x0100, 0x0136, 0x0002},
    {0x0139, 0x0147, 0x0002}, {0x014a, 0x0178, 0x0002}, {0x0179, 0x017d, 0x0002}, {0x0181, 0x0182, 0x0001},
    {0x0184, 0x0186, 0x0002}, {0x0187, 0x0189, 0x0002}, {0x018a, 0x018b, 0x0001}, {0x018e, 0x0191, 0x0001},
    {0x0193, 0x0194, 0x0001}, {0x0196, 0x0198, 0x0001}, {0x019c, 0x019d, 0x0001}, {0x019f, 0x01a0, 0x0001},
    {0x01a2, 0x01a6, 0x0002}, {0x01a7, 0x01a9, 0x0002}, {0x01ac, 0x01ae, 0x0002}, {0x01af, 0x01b1, 0x0002},
    {0x01b2, 0x01b3, 0x0001}, {0x01b5, 0x01b7, 0x0002}, {0x01b8, 0x01bc, 0x0004}, {0x01c4, 0x01cd, 0x0003},
    {0x01cf, 0x01db, 0x0002}, {0x01de, 0x01ee, 0x0002}, {0x01f1, 0x01f4, 0x0003}, {0x01f6, 0x01f8, 0x0001},
    {0x01fa, 0x0232, 0x0002}, {0x023a, 0x023b, 0x0001}, {0x023d, 0x023e, 0x0001}, {0x0241, 0x0243, 0x0002},
    {0x0244, 0x0246, 0x0001}, {0x0248, 0x024e, 0x0002}, {0x0370, 0x0372, 0x0002}, {0x0376, 0x037f, 0x0009},
    {0x0386, 0x0388, 0x0002}, {0x0389, 0x038a, 0x0001}, {0x038c, 0x038e, 0x0002}, {0x038f, 0x0391, 0x0002},
    {0x0392, 0x03a1, 0x0001}, {0x03a3, 0x03ab, 0x0001}, {0x03cf, 0x03d2, 0x0003}, {0x03d3, 0x03d4, 0x0001},
    {0x03d8, 0x03ee, 0x0002}, {0x03f4, 0x03f7, 0x0003}, {0x03f9, 0x03fa, 0x0001}, {0x03fd, 0x042f, 0x0001},
    {0x0460, 0x0480, 0x0002}, {0x048a, 0x04c0, 0x0002}, {0x04c1, 0x04cd, 0x0002}, {0x04d0, 0x052e, 0x0002},
    {0x0531, 0x0556, 0x0001}, {0x10a0, 0x10c5, 0x0001}, {0x10c7, 0x10cd, 0x0006}, {0x13a0, 0x13f5, 0x0001},
    {0x1c89, 0x1c90, 0x0007}, {0x1c91, 0x1cba, 0x0001}, {0x1cbd, 0x1cbf, 0x0001}, {0x1e00, 0x1e94, 0x0002},
    {0x1e9e, 0x1efe, 0x0002}, {0x1f08, 0x1f0f, 0x0001}, {0x1f18, 0x1f1d, 0x0001}, {0x1f28, 0x1f2f, 0x0001},
    {0x1f38, 0x1f3f, 0x0001}, {0x1f48, 0x1f4d, 0x0001}, {0x1f59, 0x1f5f, 0x0002}, {0x1f68, 0x1f6f, 0x0001},
    {0x1fb8, 0x1fbb, 0x0001}, {0x1fc8, 0x1fcb, 0x0001}, {0x1fd8, 0x1fdb, 0x0001}, {0x1fe8, 0x1fec, 0x0001},
    {0x1ff8, 0x1ffb, 0x0001}, {0x2102, 0x2107, 0x0005}, {0x210b, 0x210d, 0x0001}, {0x2110, 0x2112, 0x0001},
    {0x2115, 0x2119, 0x0004}, {0x211a, 0x211d, 0x0001}, {0x2124, 0x212a, 0x0002}, {0x212b, 0x212d, 0x0001},
    {0x2130, 0x2133, 0x0001}, {0x213e, 0x213f, 0x0001}, {0x2145, 0x2183, 0x003e}, {0x2c00, 0x2c2f, 0x0001},
    {0x2c60, 0x2c62, 0x0002}, {0x2c63, 0x2c64, 0x0001}, {0x2c67, 0x2c6d, 0x0002}, {0x2c6e, 0x2c70, 0x0001},
    {0x2c72, 0x2c75, 0x0003}, {0x2c7e, 0x2c80, 0x0001}, {0x2c82, 0x2ce2, 0x0002}, {0x2ceb, 0x2ced, 0x0002},
    {0x2cf2, 0xa640, 0x794e}, {0xa642, 0xa66c, 0x0002}, {0xa680, 0xa69a, 0x0002}, {0xa722, 0xa72e, 0x0002},
    {0xa732, 0xa76e, 0x0002}, {0xa779, 0xa77d, 0x0002}, {0xa77e, 0xa786, 0x0002}, {0xa78b, 0xa78d, 0x0002},
    {0xa790, 0xa792, 0x0002}, {0xa796, 0xa7aa, 0x0002}, {0xa7ab, 0xa7ae, 0x0001}, {0xa7b0, 0xa7b4, 0x0001},
    {0xa7b6, 0xa7c4, 0x0002}, {0xa7c5, 0xa7c7, 0x0001}, {0xa7c9, 0xa7cb, 0x0002}, {0xa7cc, 0xa7dc, 0x0002},
    {0xa7f5, 0xff21, 0x572c}, {0xff22, 0xff3a, 0x0001}
};


//
//	upper32
//

#if defined(IMGUI_USE_WCHAR32)

static Range32 upper32[] = {
    {0x10400, 0x10427, 0x0001}, {0x104b0, 0x104d3, 0x0001}, {0x10570, 0x1057a, 0x0001}, {0x1057c, 0x1058a, 0x0001},
    {0x1058c, 0x10592, 0x0001}, {0x10594, 0x10595, 0x0001}, {0x10c80, 0x10cb2, 0x0001}, {0x10d50, 0x10d65, 0x0001},
    {0x118a0, 0x118bf, 0x0001}, {0x16e40, 0x16e5f, 0x0001}, {0x16ea0, 0x16eb8, 0x0001}, {0x1d400, 0x1d419, 0x0001},
    {0x1d434, 0x1d44d, 0x0001}, {0x1d468, 0x1d481, 0x0001}, {0x1d49c, 0x1d49e, 0x0002}, {0x1d49f, 0x1d4a5, 0x0003},
    {0x1d4a6, 0x1d4a9, 0x0003}, {0x1d4aa, 0x1d4ac, 0x0001}, {0x1d4ae, 0x1d4b5, 0x0001}, {0x1d4d0, 0x1d4e9, 0x0001},
    {0x1d504, 0x1d505, 0x0001}, {0x1d507, 0x1d50a, 0x0001}, {0x1d50d, 0x1d514, 0x0001}, {0x1d516, 0x1d51c, 0x0001},
    {0x1d538, 0x1d539, 0x0001}, {0x1d53b, 0x1d53e, 0x0001}, {0x1d540, 0x1d544, 0x0001}, {0x1d546, 0x1d54a, 0x0004},
    {0x1d54b, 0x1d550, 0x0001}, {0x1d56c, 0x1d585, 0x0001}, {0x1d5a0, 0x1d5b9, 0x0001}, {0x1d5d4, 0x1d5ed, 0x0001},
    {0x1d608, 0x1d621, 0x0001}, {0x1d63c, 0x1d655, 0x0001}, {0x1d670, 0x1d689, 0x0001}, {0x1d6a8, 0x1d6c0, 0x0001},
    {0x1d6e2, 0x1d6fa, 0x0001}, {0x1d71c, 0x1d734, 0x0001}, {0x1d756, 0x1d76e, 0x0001}, {0x1d790, 0x1d7a8, 0x0001},
    {0x1d7ca, 0x1e900, 0x1136}, {0x1e901, 0x1e921, 0x0001}
};

#endif


//
//	numbers16
//

static Range16 numbers16[] = {
    {0x0030, 0x0039, 0x0001}, {0x0660, 0x0669, 0x0001}, {0x06f0, 0x06f9, 0x0001}, {0x07c0, 0x07c9, 0x0001},
    {0x0966, 0x096f, 0x0001}, {0x09e6, 0x09ef, 0x0001}, {0x0a66, 0x0a6f, 0x0001}, {0x0ae6, 0x0aef, 0x0001},
    {0x0b66, 0x0b6f, 0x0001}, {0x0be6, 0x0bef, 0x0001}, {0x0c66, 0x0c6f, 0x0001}, {0x0ce6, 0x0cef, 0x0001},
    {0x0d66, 0x0d6f, 0x0001}, {0x0de6, 0x0def, 0x0001}, {0x0e50, 0x0e59, 0x0001}, {0x0ed0, 0x0ed9, 0x0001},
    {0x0f20, 0x0f29, 0x0001}, {0x1040, 0x1049, 0x0001}, {0x1090, 0x1099, 0x0001}, {0x17e0, 0x17e9, 0x0001},
    {0x1810, 0x1819, 0x0001}, {0x1946, 0x194f, 0x0001}, {0x19d0, 0x19d9, 0x0001}, {0x1a80, 0x1a89, 0x0001},
    {0x1a90, 0x1a99, 0x0001}, {0x1b50, 0x1b59, 0x0001}, {0x1bb0, 0x1bb9, 0x0001}, {0x1c40, 0x1c49, 0x0001},
    {0x1c50, 0x1c59, 0x0001}, {0xa620, 0xa629, 0x0001}, {0xa8d0, 0xa8d9, 0x0001}, {0xa900, 0xa909, 0x0001},
    {0xa9d0, 0xa9d9, 0x0001}, {0xa9f0, 0xa9f9, 0x0001}, {0xaa50, 0xaa59, 0x0001}, {0xabf0, 0xabf9, 0x0001},
    {0xff10, 0xff19, 0x0001}
};


//
//	numbers32
//

#if defined(IMGUI_USE_WCHAR32)

static Range32 numbers32[] = {
    {0x104a0, 0x104a9, 0x0001}, {0x10d30, 0x10d39, 0x0001}, {0x10d40, 0x10d49, 0x0001}, {0x11066, 0x1106f, 0x0001},
    {0x110f0, 0x110f9, 0x0001}, {0x11136, 0x1113f, 0x0001}, {0x111d0, 0x111d9, 0x0001}, {0x112f0, 0x112f9, 0x0001},
    {0x11450, 0x11459, 0x0001}, {0x114d0, 0x114d9, 0x0001}, {0x11650, 0x11659, 0x0001}, {0x116c0, 0x116c9, 0x0001},
    {0x116d0, 0x116e3, 0x0001}, {0x11730, 0x11739, 0x0001}, {0x118e0, 0x118e9, 0x0001}, {0x11950, 0x11959, 0x0001},
    {0x11bf0, 0x11bf9, 0x0001}, {0x11c50, 0x11c59, 0x0001}, {0x11d50, 0x11d59, 0x0001}, {0x11da0, 0x11da9, 0x0001},
    {0x11de0, 0x11de9, 0x0001}, {0x11f50, 0x11f59, 0x0001}, {0x16130, 0x16139, 0x0001}, {0x16a60, 0x16a69, 0x0001},
    {0x16ac0, 0x16ac9, 0x0001}, {0x16b50, 0x16b59, 0x0001}, {0x16d70, 0x16d79, 0x0001}, {0x1ccf0, 0x1ccf9, 0x0001},
    {0x1d7ce, 0x1d7ff, 0x0001}, {0x1e140, 0x1e149, 0x0001}, {0x1e2f0, 0x1e2f9, 0x0001}, {0x1e4f0, 0x1e4f9, 0x0001},
    {0x1e5f1, 0x1e5fa, 0x0001}, {0x1e950, 0x1e959, 0x0001}, {0x1fbf0, 0x1fbf9, 0x0001}
};

#endif


//
//	whitespace16
//

static Range16 whitespace16[] = {
    {0x0009, 0x000d, 0x0001}, {0x0020, 0x0085, 0x0065}, {0x00a0, 0x1680, 0x15e0}, {0x2000, 0x200a, 0x0001},
    {0x2028, 0x2029, 0x0001}, {0x202f, 0x205f, 0x0030}, {0x3000, 0x3000, 0x0001}
};


//
//	xidStart16
//

static Range16 xidStart16[] = {
    {0x0041, 0x005a, 0x0001}, {0x0061, 0x007a, 0x0001}, {0x00aa, 0x00b5, 0x000b}, {0x00ba, 0x00c0, 0x0006},
    {0x00c1, 0x00d6, 0x0001}, {0x00d8, 0x00f6, 0x0001}, {0x00f8, 0x02c1, 0x0001}, {0x02c6, 0x02d1, 0x0001},
    {0x02e0, 0x02e4, 0x0001}, {0x02ec, 0x02ee, 0x0002}, {0x0370, 0x0374, 0x0001}, {0x0376, 0x0377, 0x0001},
    {0x037b, 0x037d, 0x0001}, {0x037f, 0x0386, 0x0007}, {0x0388, 0x038a, 0x0001}, {0x038c, 0x038e, 0x0002},
    {0x038f, 0x03a1, 0x0001}, {0x03a3, 0x03f5, 0x0001}, {0x03f7, 0x0481, 0x0001}, {0x048a, 0x052f, 0x0001},
    {0x0531, 0x0556, 0x0001}, {0x0559, 0x0560, 0x0007}, {0x0561, 0x0588, 0x0001}, {0x05d0, 0x05ea, 0x0001},
    {0x05ef, 0x05f2, 0x0001}, {0x0620, 0x064a, 0x0001}, {0x066e, 0x066f, 0x0001}, {0x0671, 0x06d3, 0x0001},
    {0x06d5, 0x06e5, 0x0010}, {0x06e6, 0x06ee, 0x0008}, {0x06ef, 0x06fa, 0x000b}, {0x06fb, 0x06fc, 0x0001},
    {0x06ff, 0x0710, 0x0011}, {0x0712, 0x072f, 0x0001}, {0x074d, 0x07a5, 0x0001}, {0x07b1, 0x07ca, 0x0019},
    {0x07cb, 0x07ea, 0x0001}, {0x07f4, 0x07f5, 0x0001}, {0x07fa, 0x0800, 0x0006}, {0x0801, 0x0815, 0x0001},
    {0x081a, 0x0824, 0x000a}, {0x0828, 0x0840, 0x0018}, {0x0841, 0x0858, 0x0001}, {0x0860, 0x086a, 0x0001},
    {0x0870, 0x0887, 0x0001}, {0x0889, 0x088f, 0x0001}, {0x08a0, 0x08c9, 0x0001}, {0x0904, 0x0939, 0x0001},
    {0x093d, 0x0950, 0x0013}, {0x0958, 0x0961, 0x0001}, {0x0971, 0x0980, 0x0001}, {0x0985, 0x098c, 0x0001},
    {0x098f, 0x0990, 0x0001}, {0x0993, 0x09a8, 0x0001}, {0x09aa, 0x09b0, 0x0001}, {0x09b2, 0x09b6, 0x0004},
    {0x09b7, 0x09b9, 0x0001}, {0x09bd, 0x09ce, 0x0011}, {0x09dc, 0x09dd, 0x0001}, {0x09df, 0x09e1, 0x0001},
    {0x09f0, 0x09f1, 0x0001}, {0x09fc, 0x0a05, 0x0009}, {0x0a06, 0x0a0a, 0x0001}, {0x0a0f, 0x0a10, 0x0001},
    {0x0a13, 0x0a28, 0x0001}, {0x0a2a, 0x0a30, 0x0001}, {0x0a32, 0x0a33, 0x0001}, {0x0a35, 0x0a36, 0x0001},
    {0x0a38, 0x0a39, 0x0001}, {0x0a59, 0x0a5c, 0x0001}, {0x0a5e, 0x0a72, 0x0014}, {0x0a73, 0x0a74, 0x0001},
    {0x0a85, 0x0a8d, 0x0001}, {0x0a8f, 0x0a91, 0x0001}, {0x0a93, 0x0aa8, 0x0001}, {0x0aaa, 0x0ab0, 0x0001},
    {0x0ab2, 0x0ab3, 0x0001}, {0x0ab5, 0x0ab9, 0x0001}, {0x0abd, 0x0ad0, 0x0013}, {0x0ae0, 0x0ae1, 0x0001},
    {0x0af9, 0x0b05, 0x000c}, {0x0b06, 0x0b0c, 0x0001}, {0x0b0f, 0x0b10, 0x0001}, {0x0b13, 0x0b28, 0x0001},
    {0x0b2a, 0x0b30, 0x0001}, {0x0b32, 0x0b33, 0x0001}, {0x0b35, 0x0b39, 0x0001}, {0x0b3d, 0x0b5c, 0x001f},
    {0x0b5d, 0x0b5f, 0x0002}, {0x0b60, 0x0b61, 0x0001}, {0x0b71, 0x0b83, 0x0012}, {0x0b85, 0x0b8a, 0x0001},
    {0x0b8e, 0x0b90, 0x0001}, {0x0b92, 0x0b95, 0x0001}, {0x0b99, 0x0b9a, 0x0001}, {0x0b9c, 0x0b9e, 0x0002},
    {0x0b9f, 0x0ba3, 0x0004}, {0x0ba4, 0x0ba8, 0x0004}, {0x0ba9, 0x0baa, 0x0001}, {0x0bae, 0x0bb9, 0x0001},
    {0x0bd0, 0x0c05, 0x0035}, {0x0c06, 0x0c0c, 0x0001}, {0x0c0e, 0x0c10, 0x0001}, {0x0c12, 0x0c28, 0x0001},
    {0x0c2a, 0x0c39, 0x0001}, {0x0c3d, 0x0c58, 0x001b}, {0x0c59, 0x0c5a, 0x0001}, {0x0c5c, 0x0c5d, 0x0001},
    {0x0c60, 0x0c61, 0x0001}, {0x0c80, 0x0c85, 0x0005}, {0x0c86, 0x0c8c, 0x0001}, {0x0c8e, 0x0c90, 0x0001},
    {0x0c92, 0x0ca8, 0x0001}, {0x0caa, 0x0cb3, 0x0001}, {0x0cb5, 0x0cb9, 0x0001}, {0x0cbd, 0x0cdc, 0x001f},
    {0x0cdd, 0x0cde, 0x0001}, {0x0ce0, 0x0ce1, 0x0001}, {0x0cf1, 0x0cf2, 0x0001}, {0x0d04, 0x0d0c, 0x0001},
    {0x0d0e, 0x0d10, 0x0001}, {0x0d12, 0x0d3a, 0x0001}, {0x0d3d, 0x0d4e, 0x0011}, {0x0d54, 0x0d56, 0x0001},
    {0x0d5f, 0x0d61, 0x0001}, {0x0d7a, 0x0d7f, 0x0001}, {0x0d85, 0x0d96, 0x0001}, {0x0d9a, 0x0db1, 0x0001},
    {0x0db3, 0x0dbb, 0x0001}, {0x0dbd, 0x0dc0, 0x0003}, {0x0dc1, 0x0dc6, 0x0001}, {0x0e01, 0x0e30, 0x0001},
    {0x0e32, 0x0e40, 0x000e}, {0x0e41, 0x0e46, 0x0001}, {0x0e81, 0x0e82, 0x0001}, {0x0e84, 0x0e86, 0x0002},
    {0x0e87, 0x0e8a, 0x0001}, {0x0e8c, 0x0ea3, 0x0001}, {0x0ea5, 0x0ea7, 0x0002}, {0x0ea8, 0x0eb0, 0x0001},
    {0x0eb2, 0x0ebd, 0x000b}, {0x0ec0, 0x0ec4, 0x0001}, {0x0ec6, 0x0edc, 0x0016}, {0x0edd, 0x0edf, 0x0001},
    {0x0f00, 0x0f40, 0x0040}, {0x0f41, 0x0f47, 0x0001}, {0x0f49, 0x0f6c, 0x0001}, {0x0f88, 0x0f8c, 0x0001},
    {0x1000, 0x102a, 0x0001}, {0x103f, 0x1050, 0x0011}, {0x1051, 0x1055, 0x0001}, {0x105a, 0x105d, 0x0001},
    {0x1061, 0x1065, 0x0004}, {0x1066, 0x106e, 0x0008}, {0x106f, 0x1070, 0x0001}, {0x1075, 0x1081, 0x0001},
    {0x108e, 0x10a0, 0x0012}, {0x10a1, 0x10c5, 0x0001}, {0x10c7, 0x10cd, 0x0006}, {0x10d0, 0x10fa, 0x0001},
    {0x10fc, 0x1248, 0x0001}, {0x124a, 0x124d, 0x0001}, {0x1250, 0x1256, 0x0001}, {0x1258, 0x125a, 0x0002},
    {0x125b, 0x125d, 0x0001}, {0x1260, 0x1288, 0x0001}, {0x128a, 0x128d, 0x0001}, {0x1290, 0x12b0, 0x0001},
    {0x12b2, 0x12b5, 0x0001}, {0x12b8, 0x12be, 0x0001}, {0x12c0, 0x12c2, 0x0002}, {0x12c3, 0x12c5, 0x0001},
    {0x12c8, 0x12d6, 0x0001}, {0x12d8, 0x1310, 0x0001}, {0x1312, 0x1315, 0x0001}, {0x1318, 0x135a, 0x0001},
    {0x1380, 0x138f, 0x0001}, {0x13a0, 0x13f5, 0x0001}, {0x13f8, 0x13fd, 0x0001}, {0x1401, 0x166c, 0x0001},
    {0x166f, 0x167f, 0x0001}, {0x1681, 0x169a, 0x0001}, {0x16a0, 0x16ea, 0x0001}, {0x16ee, 0x16f8, 0x0001},
    {0x1700, 0x1711, 0x0001}, {0x171f, 0x1731, 0x0001}, {0x1740, 0x1751, 0x0001}, {0x1760, 0x176c, 0x0001},
    {0x176e, 0x1770, 0x0001}, {0x1780, 0x17b3, 0x0001}, {0x17d7, 0x17dc, 0x0005}, {0x1820, 0x1878, 0x0001},
    {0x1880, 0x18a8, 0x0001}, {0x18aa, 0x18b0, 0x0006}, {0x18b1, 0x18f5, 0x0001}, {0x1900, 0x191e, 0x0001},
    {0x1950, 0x196d, 0x0001}, {0x1970, 0x1974, 0x0001}, {0x1980, 0x19ab, 0x0001}, {0x19b0, 0x19c9, 0x0001},
    {0x1a00, 0x1a16, 0x0001}, {0x1a20, 0x1a54, 0x0001}, {0x1aa7, 0x1b05, 0x005e}, {0x1b06, 0x1b33, 0x0001},
    {0x1b45, 0x1b4c, 0x0001}, {0x1b83, 0x1ba0, 0x0001}, {0x1bae, 0x1baf, 0x0001}, {0x1bba, 0x1be5, 0x0001},
    {0x1c00, 0x1c23, 0x0001}, {0x1c4d, 0x1c4f, 0x0001}, {0x1c5a, 0x1c7d, 0x0001}, {0x1c80, 0x1c8a, 0x0001},
    {0x1c90, 0x1cba, 0x0001}, {0x1cbd, 0x1cbf, 0x0001}, {0x1ce9, 0x1cec, 0x0001}, {0x1cee, 0x1cf3, 0x0001},
    {0x1cf5, 0x1cf6, 0x0001}, {0x1cfa, 0x1d00, 0x0006}, {0x1d01, 0x1dbf, 0x0001}, {0x1e00, 0x1f15, 0x0001},
    {0x1f18, 0x1f1d, 0x0001}, {0x1f20, 0x1f45, 0x0001}, {0x1f48, 0x1f4d, 0x0001}, {0x1f50, 0x1f57, 0x0001},
    {0x1f59, 0x1f5f, 0x0002}, {0x1f60, 0x1f7d, 0x0001}, {0x1f80, 0x1fb4, 0x0001}, {0x1fb6, 0x1fbc, 0x0001},
    {0x1fbe, 0x1fc2, 0x0004}, {0x1fc3, 0x1fc4, 0x0001}, {0x1fc6, 0x1fcc, 0x0001}, {0x1fd0, 0x1fd3, 0x0001},
    {0x1fd6, 0x1fdb, 0x0001}, {0x1fe0, 0x1fec, 0x0001}, {0x1ff2, 0x1ff4, 0x0001}, {0x1ff6, 0x1ffc, 0x0001},
    {0x2071, 0x207f, 0x000e}, {0x2090, 0x209c, 0x0001}, {0x2102, 0x2107, 0x0005}, {0x210a, 0x2113, 0x0001},
    {0x2115, 0x2118, 0x0003}, {0x2119, 0x211d, 0x0001}, {0x2124, 0x212a, 0x0002}, {0x212b, 0x2139, 0x0001},
    {0x213c, 0x213f, 0x0001}, {0x2145, 0x2149, 0x0001}, {0x214e, 0x2160, 0x0012}, {0x2161, 0x2188, 0x0001},
    {0x2c00, 0x2ce4, 0x0001}, {0x2ceb, 0x2cee, 0x0001}, {0x2cf2, 0x2cf3, 0x0001}, {0x2d00, 0x2d25, 0x0001},
    {0x2d27, 0x2d2d, 0x0006}, {0x2d30, 0x2d67, 0x0001}, {0x2d6f, 0x2d80, 0x0011}, {0x2d81, 0x2d96, 0x0001},
    {0x2da0, 0x2da6, 0x0001}, {0x2da8, 0x2dae, 0x0001}, {0x2db0, 0x2db6, 0x0001}, {0x2db8, 0x2dbe, 0x0001},
    {0x2dc0, 0x2dc6, 0x0001}, {0x2dc8, 0x2dce, 0x0001}, {0x2dd0, 0x2dd6, 0x0001}, {0x2dd8, 0x2dde, 0x0001},
    {0x3005, 0x3007, 0x0001}, {0x3021, 0x3029, 0x0001}, {0x3031, 0x3035, 0x0001}, {0x3038, 0x303c, 0x0001},
    {0x3041, 0x3096, 0x0001}, {0x309d, 0x309f, 0x0001}, {0x30a1, 0x30fa, 0x0001}, {0x30fc, 0x30ff, 0x0001},
    {0x3105, 0x312f, 0x0001}, {0x3131, 0x318e, 0x0001}, {0x31a0, 0x31bf, 0x0001}, {0x31f0, 0x31ff, 0x0001},
    {0x3400, 0x4dbf, 0x0001}, {0x4e00, 0xa48c, 0x0001}, {0xa4d0, 0xa4fd, 0x0001}, {0xa500, 0xa60c, 0x0001},
    {0xa610, 0xa61f, 0x0001}, {0xa62a, 0xa62b, 0x0001}, {0xa640, 0xa66e, 0x0001}, {0xa67f, 0xa69d, 0x0001},
    {0xa6a0, 0xa6ef, 0x0001}, {0xa717, 0xa71f, 0x0001}, {0xa722, 0xa788, 0x0001}, {0xa78b, 0xa7dc, 0x0001},
    {0xa7f1, 0xa801, 0x0001}, {0xa803, 0xa805, 0x0001}, {0xa807, 0xa80a, 0x0001}, {0xa80c, 0xa822, 0x0001},
    {0xa840, 0xa873, 0x0001}, {0xa882, 0xa8b3, 0x0001}, {0xa8f2, 0xa8f7, 0x0001}, {0xa8fb, 0xa8fd, 0x0002},
    {0xa8fe, 0xa90a, 0x000c}, {0xa90b, 0xa925, 0x0001}, {0xa930, 0xa946, 0x0001}, {0xa960, 0xa97c, 0x0001},
    {0xa984, 0xa9b2, 0x0001}, {0xa9cf, 0xa9e0, 0x0011}, {0xa9e1, 0xa9e4, 0x0001}, {0xa9e6, 0xa9ef, 0x0001},
    {0xa9fa, 0xa9fe, 0x0001}, {0xaa00, 0xaa28, 0x0001}, {0xaa40, 0xaa42, 0x0001}, {0xaa44, 0xaa4b, 0x0001},
    {0xaa60, 0xaa76, 0x0001}, {0xaa7a, 0xaa7e, 0x0004}, {0xaa7f, 0xaaaf, 0x0001}, {0xaab1, 0xaab5, 0x0004},
    {0xaab6, 0xaab9, 0x0003}, {0xaaba, 0xaabd, 0x0001}, {0xaac0, 0xaac2, 0x0002}, {0xaadb, 0xaadd, 0x0001},
    {0xaae0, 0xaaea, 0x0001}, {0xaaf2, 0xaaf4, 0x0001}, {0xab01, 0xab06, 0x0001}, {0xab09, 0xab0e, 0x0001},
    {0xab11, 0xab16, 0x0001}, {0xab20, 0xab26, 0x0001}, {0xab28, 0xab2e, 0x0001}, {0xab30, 0xab5a, 0x0001},
    {0xab5c, 0xab69, 0x0001}, {0xab70, 0xabe2, 0x0001}, {0xac00, 0xd7a3, 0x0001}, {0xd7b0, 0xd7c6, 0x0001},
    {0xd7cb, 0xd7fb, 0x0001}, {0xf900, 0xfa6d, 0x0001}, {0xfa70, 0xfad9, 0x0001}, {0xfb00, 0xfb06, 0x0001},
    {0xfb13, 0xfb17, 0x0001}, {0xfb1d, 0xfb1f, 0x0002}, {0xfb20, 0xfb28, 0x0001}, {0xfb2a, 0xfb36, 0x0001},
    {0xfb38, 0xfb3c, 0x0001}, {0xfb3e, 0xfb40, 0x0002}, {0xfb41, 0xfb43, 0x0002}, {0xfb44, 0xfb46, 0x0002},
    {0xfb47, 0xfbb1, 0x0001}, {0xfbd3, 0xfc5d, 0x0001}, {0xfc64, 0xfd3d, 0x0001}, {0xfd50, 0xfd8f, 0x0001},
    {0xfd92, 0xfdc7, 0x0001}, {0xfdf0, 0xfdf9, 0x0001}, {0xfe71, 0xfe73, 0x0002}, {0xfe77, 0xfe7f, 0x0002},
    {0xfe80, 0xfefc, 0x0001}, {0xff21, 0xff3a, 0x0001}, {0xff41, 0xff5a, 0x0001}, {0xff66, 0xff9d, 0x0001},
    {0xffa0, 0xffbe, 0x0001}, {0xffc2, 0xffc7, 0x0001}, {0xffca, 0xffcf, 0x0001}, {0xffd2, 0xffd7, 0x0001},
    {0xffda, 0xffdc, 0x0001}
};


//
//	xidStart32
//

#if defined(IMGUI_USE_WCHAR32)

static Range32 xidStart32[] = {
    {0x10000, 0x1000b, 0x0001}, {0x1000d, 0x10026, 0x0001}, {0x10028, 0x1003a, 0x0001}, {0x1003c, 0x1003d, 0x0001},
    {0x1003f, 0x1004d, 0x0001}, {0x10050, 0x1005d, 0x0001}, {0x10080, 0x100fa, 0x0001}, {0x10140, 0x10174, 0x0001},
    {0x10280, 0x1029c, 0x0001}, {0x102a0, 0x102d0, 0x0001}, {0x10300, 0x1031f, 0x0001}, {0x1032d, 0x1034a, 0x0001},
    {0x10350, 0x10375, 0x0001}, {0x10380, 0x1039d, 0x0001}, {0x103a0, 0x103c3, 0x0001}, {0x103c8, 0x103cf, 0x0001},
    {0x103d1, 0x103d5, 0x0001}, {0x10400, 0x1049d, 0x0001}, {0x104b0, 0x104d3, 0x0001}, {0x104d8, 0x104fb, 0x0001},
    {0x10500, 0x10527, 0x0001}, {0x10530, 0x10563, 0x0001}, {0x10570, 0x1057a, 0x0001}, {0x1057c, 0x1058a, 0x0001},
    {0x1058c, 0x10592, 0x0001}, {0x10594, 0x10595, 0x0001}, {0x10597, 0x105a1, 0x0001}, {0x105a3, 0x105b1, 0x0001},
    {0x105b3, 0x105b9, 0x0001}, {0x105bb, 0x105bc, 0x0001}, {0x105c0, 0x105f3, 0x0001}, {0x10600, 0x10736, 0x0001},
    {0x10740, 0x10755, 0x0001}, {0x10760, 0x10767, 0x0001}, {0x10780, 0x10785, 0x0001}, {0x10787, 0x107b0, 0x0001},
    {0x107b2, 0x107ba, 0x0001}, {0x10800, 0x10805, 0x0001}, {0x10808, 0x1080a, 0x0002}, {0x1080b, 0x10835, 0x0001},
    {0x10837, 0x10838, 0x0001}, {0x1083c, 0x1083f, 0x0003}, {0x10840, 0x10855, 0x0001}, {0x10860, 0x10876, 0x0001},
    {0x10880, 0x1089e, 0x0001}, {0x108e0, 0x108f2, 0x0001}, {0x108f4, 0x108f5, 0x0001}, {0x10900, 0x10915, 0x0001},
    {0x10920, 0x10939, 0x0001}, {0x10940, 0x10959, 0x0001}, {0x10980, 0x109b7, 0x0001}, {0x109be, 0x109bf, 0x0001},
    {0x10a00, 0x10a10, 0x0010}, {0x10a11, 0x10a13, 0x0001}, {0x10a15, 0x10a17, 0x0001}, {0x10a19, 0x10a35, 0x0001},
    {0x10a60, 0x10a7c, 0x0001}, {0x10a80, 0x10a9c, 0x0001}, {0x10ac0, 0x10ac7, 0x0001}, {0x10ac9, 0x10ae4, 0x0001},
    {0x10b00, 0x10b35, 0x0001}, {0x10b40, 0x10b55, 0x0001}, {0x10b60, 0x10b72, 0x0001}, {0x10b80, 0x10b91, 0x0001},
    {0x10c00, 0x10c48, 0x0001}, {0x10c80, 0x10cb2, 0x0001}, {0x10cc0, 0x10cf2, 0x0001}, {0x10d00, 0x10d23, 0x0001},
    {0x10d4a, 0x10d65, 0x0001}, {0x10d6f, 0x10d85, 0x0001}, {0x10e80, 0x10ea9, 0x0001}, {0x10eb0, 0x10eb1, 0x0001},
    {0x10ec2, 0x10ec7, 0x0001}, {0x10f00, 0x10f1c, 0x0001}, {0x10f27, 0x10f30, 0x0009}, {0x10f31, 0x10f45, 0x0001},
    {0x10f70, 0x10f81, 0x0001}, {0x10fb0, 0x10fc4, 0x0001}, {0x10fe0, 0x10ff6, 0x0001}, {0x11003, 0x11037, 0x0001},
    {0x11071, 0x11072, 0x0001}, {0x11075, 0x11083, 0x000e}, {0x11084, 0x110af, 0x0001}, {0x110d0, 0x110e8, 0x0001},
    {0x11103, 0x11126, 0x0001}, {0x11144, 0x11147, 0x0003}, {0x11150, 0x11172, 0x0001}, {0x11176, 0x11183, 0x000d},
    {0x11184, 0x111b2, 0x0001}, {0x111c1, 0x111c4, 0x0001}, {0x111da, 0x111dc, 0x0002}, {0x11200, 0x11211, 0x0001},
    {0x11213, 0x1122b, 0x0001}, {0x1123f, 0x11240, 0x0001}, {0x11280, 0x11286, 0x0001}, {0x11288, 0x1128a, 0x0002},
    {0x1128b, 0x1128d, 0x0001}, {0x1128f, 0x1129d, 0x0001}, {0x1129f, 0x112a8, 0x0001}, {0x112b0, 0x112de, 0x0001},
    {0x11305, 0x1130c, 0x0001}, {0x1130f, 0x11310, 0x0001}, {0x11313, 0x11328, 0x0001}, {0x1132a, 0x11330, 0x0001},
    {0x11332, 0x11333, 0x0001}, {0x11335, 0x11339, 0x0001}, {0x1133d, 0x11350, 0x0013}, {0x1135d, 0x11361, 0x0001},
    {0x11380, 0x11389, 0x0001}, {0x1138b, 0x1138e, 0x0003}, {0x11390, 0x113b5, 0x0001}, {0x113b7, 0x113d1, 0x001a},
    {0x113d3, 0x11400, 0x002d}, {0x11401, 0x11434, 0x0001}, {0x11447, 0x1144a, 0x0001}, {0x1145f, 0x11461, 0x0001},
    {0x11480, 0x114af, 0x0001}, {0x114c4, 0x114c5, 0x0001}, {0x114c7, 0x11580, 0x00b9}, {0x11581, 0x115ae, 0x0001},
    {0x115d8, 0x115db, 0x0001}, {0x11600, 0x1162f, 0x0001}, {0x11644, 0x11680, 0x003c}, {0x11681, 0x116aa, 0x0001},
    {0x116b8, 0x11700, 0x0048}, {0x11701, 0x1171a, 0x0001}, {0x11740, 0x11746, 0x0001}, {0x11800, 0x1182b, 0x0001},
    {0x118a0, 0x118df, 0x0001}, {0x118ff, 0x11906, 0x0001}, {0x11909, 0x1190c, 0x0003}, {0x1190d, 0x11913, 0x0001},
    {0x11915, 0x11916, 0x0001}, {0x11918, 0x1192f, 0x0001}, {0x1193f, 0x11941, 0x0002}, {0x119a0, 0x119a7, 0x0001},
    {0x119aa, 0x119d0, 0x0001}, {0x119e1, 0x119e3, 0x0002}, {0x11a00, 0x11a0b, 0x000b}, {0x11a0c, 0x11a32, 0x0001},
    {0x11a3a, 0x11a50, 0x0016}, {0x11a5c, 0x11a89, 0x0001}, {0x11a9d, 0x11ab0, 0x0013}, {0x11ab1, 0x11af8, 0x0001},
    {0x11bc0, 0x11be0, 0x0001}, {0x11c00, 0x11c08, 0x0001}, {0x11c0a, 0x11c2e, 0x0001}, {0x11c40, 0x11c72, 0x0032},
    {0x11c73, 0x11c8f, 0x0001}, {0x11d00, 0x11d06, 0x0001}, {0x11d08, 0x11d09, 0x0001}, {0x11d0b, 0x11d30, 0x0001},
    {0x11d46, 0x11d60, 0x001a}, {0x11d61, 0x11d65, 0x0001}, {0x11d67, 0x11d68, 0x0001}, {0x11d6a, 0x11d89, 0x0001},
    {0x11d98, 0x11db0, 0x0018}, {0x11db1, 0x11ddb, 0x0001}, {0x11ee0, 0x11ef2, 0x0001}, {0x11f02, 0x11f04, 0x0002},
    {0x11f05, 0x11f10, 0x0001}, {0x11f12, 0x11f33, 0x0001}, {0x11fb0, 0x12000, 0x0050}, {0x12001, 0x12399, 0x0001},
    {0x12400, 0x1246e, 0x0001}, {0x12480, 0x12543, 0x0001}, {0x12f90, 0x12ff0, 0x0001}, {0x13000, 0x1342f, 0x0001},
    {0x13441, 0x13446, 0x0001}, {0x13460, 0x143fa, 0x0001}, {0x14400, 0x14646, 0x0001}, {0x16100, 0x1611d, 0x0001},
    {0x16800, 0x16a38, 0x0001}, {0x16a40, 0x16a5e, 0x0001}, {0x16a70, 0x16abe, 0x0001}, {0x16ad0, 0x16aed, 0x0001},
    {0x16b00, 0x16b2f, 0x0001}, {0x16b40, 0x16b43, 0x0001}, {0x16b63, 0x16b77, 0x0001}, {0x16b7d, 0x16b8f, 0x0001},
    {0x16d40, 0x16d6c, 0x0001}, {0x16e40, 0x16e7f, 0x0001}, {0x16ea0, 0x16eb8, 0x0001}, {0x16ebb, 0x16ed3, 0x0001},
    {0x16f00, 0x16f4a, 0x0001}, {0x16f50, 0x16f93, 0x0043}, {0x16f94, 0x16f9f, 0x0001}, {0x16fe0, 0x16fe1, 0x0001},
    {0x16fe3, 0x16ff2, 0x000f}, {0x16ff3, 0x16ff6, 0x0001}, {0x17000, 0x18cd5, 0x0001}, {0x18cff, 0x18d1e, 0x0001},
    {0x18d80, 0x18df2, 0x0001}, {0x1aff0, 0x1aff3, 0x0001}, {0x1aff5, 0x1affb, 0x0001}, {0x1affd, 0x1affe, 0x0001},
    {0x1b000, 0x1b122, 0x0001}, {0x1b132, 0x1b150, 0x001e}, {0x1b151, 0x1b152, 0x0001}, {0x1b155, 0x1b164, 0x000f},
    {0x1b165, 0x1b167, 0x0001}, {0x1b170, 0x1b2fb, 0x0001}, {0x1bc00, 0x1bc6a, 0x0001}, {0x1bc70, 0x1bc7c, 0x0001},
    {0x1bc80, 0x1bc88, 0x0001}, {0x1bc90, 0x1bc99, 0x0001}, {0x1d400, 0x1d454, 0x0001}, {0x1d456, 0x1d49c, 0x0001},
    {0x1d49e, 0x1d49f, 0x0001}, {0x1d4a2, 0x1d4a5, 0x0003}, {0x1d4a6, 0x1d4a9, 0x0003}, {0x1d4aa, 0x1d4ac, 0x0001},
    {0x1d4ae, 0x1d4b9, 0x0001}, {0x1d4bb, 0x1d4bd, 0x0002}, {0x1d4be, 0x1d4c3, 0x0001}, {0x1d4c5, 0x1d505, 0x0001},
    {0x1d507, 0x1d50a, 0x0001}, {0x1d50d, 0x1d514, 0x0001}, {0x1d516, 0x1d51c, 0x0001}, {0x1d51e, 0x1d539, 0x0001},
    {0x1d53b, 0x1d53e, 0x0001}, {0x1d540, 0x1d544, 0x0001}, {0x1d546, 0x1d54a, 0x0004}, {0x1d54b, 0x1d550, 0x0001},
    {0x1d552, 0x1d6a5, 0x0001}, {0x1d6a8, 0x1d6c0, 0x0001}, {0x1d6c2, 0x1d6da, 0x0001}, {0x1d6dc, 0x1d6fa, 0x0001},
    {0x1d6fc, 0x1d714, 0x0001}, {0x1d716, 0x1d734, 0x0001}, {0x1d736, 0x1d74e, 0x0001}, {0x1d750, 0x1d76e, 0x0001},
    {0x1d770, 0x1d788, 0x0001}, {0x1d78a, 0x1d7a8, 0x0001}, {0x1d7aa, 0x1d7c2, 0x0001}, {0x1d7c4, 0x1d7cb, 0x0001},
    {0x1df00, 0x1df1e, 0x0001}, {0x1df25, 0x1df2a, 0x0001}, {0x1e030, 0x1e06d, 0x0001}, {0x1e100, 0x1e12c, 0x0001},
    {0x1e137, 0x1e13d, 0x0001}, {0x1e14e, 0x1e290, 0x0142}, {0x1e291, 0x1e2ad, 0x0001}, {0x1e2c0, 0x1e2eb, 0x0001},
    {0x1e4d0, 0x1e4eb, 0x0001}, {0x1e5d0, 0x1e5ed, 0x0001}, {0x1e5f0, 0x1e6c0, 0x00d0}, {0x1e6c1, 0x1e6de, 0x0001},
    {0x1e6e0, 0x1e6e2, 0x0001}, {0x1e6e4, 0x1e6e5, 0x0001}, {0x1e6e7, 0x1e6ed, 0x0001}, {0x1e6f0, 0x1e6f4, 0x0001},
    {0x1e6fe, 0x1e6ff, 0x0001}, {0x1e7e0, 0x1e7e6, 0x0001}, {0x1e7e8, 0x1e7eb, 0x0001}, {0x1e7ed, 0x1e7ee, 0x0001},
    {0x1e7f0, 0x1e7fe, 0x0001}, {0x1e800, 0x1e8c4, 0x0001}, {0x1e900, 0x1e943, 0x0001}, {0x1e94b, 0x1ee00, 0x04b5},
    {0x1ee01, 0x1ee03, 0x0001}, {0x1ee05, 0x1ee1f, 0x0001}, {0x1ee21, 0x1ee22, 0x0001}, {0x1ee24, 0x1ee27, 0x0003},
    {0x1ee29, 0x1ee32, 0x0001}, {0x1ee34, 0x1ee37, 0x0001}, {0x1ee39, 0x1ee3b, 0x0002}, {0x1ee42, 0x1ee47, 0x0005},
    {0x1ee49, 0x1ee4d, 0x0002}, {0x1ee4e, 0x1ee4f, 0x0001}, {0x1ee51, 0x1ee52, 0x0001}, {0x1ee54, 0x1ee57, 0x0003},
    {0x1ee59, 0x1ee61, 0x0002}, {0x1ee62, 0x1ee64, 0x0002}, {0x1ee67, 0x1ee6a, 0x0001}, {0x1ee6c, 0x1ee72, 0x0001},
    {0x1ee74, 0x1ee77, 0x0001}, {0x1ee79, 0x1ee7c, 0x0001}, {0x1ee7e, 0x1ee80, 0x0002}, {0x1ee81, 0x1ee89, 0x0001},
    {0x1ee8b, 0x1ee9b, 0x0001}, {0x1eea1, 0x1eea3, 0x0001}, {0x1eea5, 0x1eea9, 0x0001}, {0x1eeab, 0x1eebb, 0x0001},
    {0x20000, 0x2a6df, 0x0001}, {0x2a700, 0x2b81d, 0x0001}, {0x2b820, 0x2cead, 0x0001}, {0x2ceb0, 0x2ebe0, 0x0001},
    {0x2ebf0, 0x2ee5d, 0x0001}, {0x2f800, 0x2fa1d, 0x0001}, {0x30000, 0x3134a, 0x0001}, {0x31350, 0x33479, 0x0001}
};

#endif


//
//	xidContinue16
//

static Range16 xidContinue16[] = {
    {0x0030, 0x0039, 0x0001}, {0x0041, 0x005a, 0x0001}, {0x005f, 0x0061, 0x0002}, {0x0062, 0x007a, 0x0001},
    {0x00aa, 0x00b5, 0x000b}, {0x00b7, 0x00ba, 0x0003}, {0x00c0, 0x00d6, 0x0001}, {0x00d8, 0x00f6, 0x0001},
    {0x00f8, 0x02c1, 0x0001}, {0x02c6, 0x02d1, 0x0001}, {0x02e0, 0x02e4, 0x0001}, {0x02ec, 0x02ee, 0x0002},
    {0x0300, 0x0374, 0x0001}, {0x0376, 0x0377, 0x0001}, {0x037b, 0x037d, 0x0001}, {0x037f, 0x0386, 0x0007},
    {0x0387, 0x038a, 0x0001}, {0x038c, 0x038e, 0x0002}, {0x038f, 0x03a1, 0x0001}, {0x03a3, 0x03f5, 0x0001},
    {0x03f7, 0x0481, 0x0001}, {0x0483, 0x0487, 0x0001}, {0x048a, 0x052f, 0x0001}, {0x0531, 0x0556, 0x0001},
    {0x0559, 0x0560, 0x0007}, {0x0561, 0x0588, 0x0001}, {0x0591, 0x05bd, 0x0001}, {0x05bf, 0x05c1, 0x0002},
    {0x05c2, 0x05c4, 0x0002}, {0x05c5, 0x05c7, 0x0002}, {0x05d0, 0x05ea, 0x0001}, {0x05ef, 0x05f2, 0x0001},
    {0x0610, 0x061a, 0x0001}, {0x0620, 0x0669, 0x0001}, {0x066e, 0x06d3, 0x0001}, {0x06d5, 0x06dc, 0x0001},
    {0x06df, 0x06e8, 0x0001}, {0x06ea, 0x06fc, 0x0001}, {0x06ff, 0x0710, 0x0011}, {0x0711, 0x074a, 0x0001},
    {0x074d, 0x07b1, 0x0001}, {0x07c0, 0x07f5, 0x0001}, {0x07fa, 0x0800, 0x0003}, {0x0801, 0x082d, 0x0001},
    {0x0840, 0x085b, 0x0001}, {0x0860, 0x086a, 0x0001}, {0x0870, 0x0887, 0x0001}, {0x0889, 0x088f, 0x0001},
    {0x0897, 0x08e1, 0x0001}, {0x08e3, 0x0963, 0x0001}, {0x0966, 0x096f, 0x0001}, {0x0971, 0x0983, 0x0001},
    {0x0985, 0x098c, 0x0001}, {0x098f, 0x0990, 0x0001}, {0x0993, 0x09a8, 0x0001}, {0x09aa, 0x09b0, 0x0001},
    {0x09b2, 0x09b6, 0x0004}, {0x09b7, 0x09b9, 0x0001}, {0x09bc, 0x09c4, 0x0001}, {0x09c7, 0x09c8, 0x0001},
    {0x09cb, 0x09ce, 0x0001}, {0x09d7, 0x09dc, 0x0005}, {0x09dd, 0x09df, 0x0002}, {0x09e0, 0x09e3, 0x0001},
    {0x09e6, 0x09f1, 0x0001}, {0x09fc, 0x09fe, 0x0002}, {0x0a01, 0x0a03, 0x0001}, {0x0a05, 0x0a0a, 0x0001},
    {0x0a0f, 0x0a10, 0x0001}, {0x0a13, 0x0a28, 0x0001}, {0x0a2a, 0x0a30, 0x0001}, {0x0a32, 0x0a33, 0x0001},
    {0x0a35, 0x0a36, 0x0001}, {0x0a38, 0x0a39, 0x0001}, {0x0a3c, 0x0a3e, 0x0002}, {0x0a3f, 0x0a42, 0x0001},
    {0x0a47, 0x0a48, 0x0001}, {0x0a4b, 0x0a4d, 0x0001}, {0x0a51, 0x0a59, 0x0008}, {0x0a5a, 0x0a5c, 0x0001},
    {0x0a5e, 0x0a66, 0x0008}, {0x0a67, 0x0a75, 0x0001}, {0x0a81, 0x0a83, 0x0001}, {0x0a85, 0x0a8d, 0x0001},
    {0x0a8f, 0x0a91, 0x0001}, {0x0a93, 0x0aa8, 0x0001}, {0x0aaa, 0x0ab0, 0x0001}, {0x0ab2, 0x0ab3, 0x0001},
    {0x0ab5, 0x0ab9, 0x0001}, {0x0abc, 0x0ac5, 0x0001}, {0x0ac7, 0x0ac9, 0x0001}, {0x0acb, 0x0acd, 0x0001},
    {0x0ad0, 0x0ae0, 0x0010}, {0x0ae1, 0x0ae3, 0x0001}, {0x0ae6, 0x0aef, 0x0001}, {0x0af9, 0x0aff, 0x0001},
    {0x0b01, 0x0b03, 0x0001}, {0x0b05, 0x0b0c, 0x0001}, {0x0b0f, 0x0b10, 0x0001}, {0x0b13, 0x0b28, 0x0001},
    {0x0b2a, 0x0b30, 0x0001}, {0x0b32, 0x0b33, 0x0001}, {0x0b35, 0x0b39, 0x0001}, {0x0b3c, 0x0b44, 0x0001},
    {0x0b47, 0x0b48, 0x0001}, {0x0b4b, 0x0b4d, 0x0001}, {0x0b55, 0x0b57, 0x0001}, {0x0b5c, 0x0b5d, 0x0001},
    {0x0b5f, 0x0b63, 0x0001}, {0x0b66, 0x0b6f, 0x0001}, {0x0b71, 0x0b82, 0x0011}, {0x0b83, 0x0b85, 0x0002},
    {0x0b86, 0x0b8a, 0x0001}, {0x0b8e, 0x0b90, 0x0001}, {0x0b92, 0x0b95, 0x0001}, {0x0b99, 0x0b9a, 0x0001},
    {0x0b9c, 0x0b9e, 0x0002}, {0x0b9f, 0x0ba3, 0x0004}, {0x0ba4, 0x0ba8, 0x0004}, {0x0ba9, 0x0baa, 0x0001},
    {0x0bae, 0x0bb9, 0x0001}, {0x0bbe, 0x0bc2, 0x0001}, {0x0bc6, 0x0bc8, 0x0001}, {0x0bca, 0x0bcd, 0x0001},
    {0x0bd0, 0x0bd7, 0x0007}, {0x0be6, 0x0bef, 0x0001}, {0x0c00, 0x0c0c, 0x0001}, {0x0c0e, 0x0c10, 0x0001},
    {0x0c12, 0x0c28, 0x0001}, {0x0c2a, 0x0c39, 0x0001}, {0x0c3c, 0x0c44, 0x0001}, {0x0c46, 0x0c48, 0x0001},
    {0x0c4a, 0x0c4d, 0x0001}, {0x0c55, 0x0c56, 0x0001}, {0x0c58, 0x0c5a, 0x0001}, {0x0c5c, 0x0c5d, 0x0001},
    {0x0c60, 0x0c63, 0x0001}, {0x0c66, 0x0c6f, 0x0001}, {0x0c80, 0x0c83, 0x0001}, {0x0c85, 0x0c8c, 0x0001},
    {0x0c8e, 0x0c90, 0x0001}, {0x0c92, 0x0ca8, 0x0001}, {0x0caa, 0x0cb3, 0x0001}, {0x0cb5, 0x0cb9, 0x0001},
    {0x0cbc, 0x0cc4, 0x0001}, {0x0cc6, 0x0cc8, 0x0001}, {0x0cca, 0x0ccd, 0x0001}, {0x0cd5, 0x0cd6, 0x0001},
    {0x0cdc, 0x0cde, 0x0001}, {0x0ce0, 0x0ce3, 0x0001}, {0x0ce6, 0x0cef, 0x0001}, {0x0cf1, 0x0cf3, 0x0001},
    {0x0d00, 0x0d0c, 0x0001}, {0x0d0e, 0x0d10, 0x0001}, {0x0d12, 0x0d44, 0x0001}, {0x0d46, 0x0d48, 0x0001},
    {0x0d4a, 0x0d4e, 0x0001}, {0x0d54, 0x0d57, 0x0001}, {0x0d5f, 0x0d63, 0x0001}, {0x0d66, 0x0d6f, 0x0001},
    {0x0d7a, 0x0d7f, 0x0001}, {0x0d81, 0x0d83, 0x0001}, {0x0d85, 0x0d96, 0x0001}, {0x0d9a, 0x0db1, 0x0001},
    {0x0db3, 0x0dbb, 0x0001}, {0x0dbd, 0x0dc0, 0x0003}, {0x0dc1, 0x0dc6, 0x0001}, {0x0dca, 0x0dcf, 0x0005},
    {0x0dd0, 0x0dd4, 0x0001}, {0x0dd6, 0x0dd8, 0x0002}, {0x0dd9, 0x0ddf, 0x0001}, {0x0de6, 0x0def, 0x0001},
    {0x0df2, 0x0df3, 0x0001}, {0x0e01, 0x0e3a, 0x0001}, {0x0e40, 0x0e4e, 0x0001}, {0x0e50, 0x0e59, 0x0001},
    {0x0e81, 0x0e82, 0x0001}, {0x0e84, 0x0e86, 0x0002}, {0x0e87, 0x0e8a, 0x0001}, {0x0e8c, 0x0ea3, 0x0001},
    {0x0ea5, 0x0ea7, 0x0002}, {0x0ea8, 0x0ebd, 0x0001}, {0x0ec0, 0x0ec4, 0x0001}, {0x0ec6, 0x0ec8, 0x0002},
    {0x0ec9, 0x0ece, 0x0001}, {0x0ed0, 0x0ed9, 0x0001}, {0x0edc, 0x0edf, 0x0001}, {0x0f00, 0x0f18, 0x0018},
    {0x0f19, 0x0f20, 0x0007}, {0x0f21, 0x0f29, 0x0001}, {0x0f35, 0x0f39, 0x0002}, {0x0f3e, 0x0f47, 0x0001},
    {0x0f49, 0x0f6c, 0x0001}, {0x0f71, 0x0f84, 0x0001}, {0x0f86, 0x0f97, 0x0001}, {0x0f99, 0x0fbc, 0x0001},
    {0x0fc6, 0x1000, 0x003a}, {0x1001, 0x1049, 0x0001}, {0x1050, 0x109d, 0x0001}, {0x10a0, 0x10c5, 0x0001},
    {0x10c7, 0x10cd, 0x0006}, {0x10d0, 0x10fa, 0x0001}, {0x10fc, 0x1248, 0x0001}, {0x124a, 0x124d, 0x0001},
    {0x1250, 0x1256, 0x0001}, {0x1258, 0x125a, 0x0002}, {0x125b, 0x125d, 0x0001}, {0x1260, 0x1288, 0x0001},
    {0x128a, 0x128d, 0x0001}, {0x1290, 0x12b0, 0x0001}, {0x12b2, 0x12b5, 0x0001}, {0x12b8, 0x12be, 0x0001},
    {0x12c0, 0x12c2, 0x0002}, {0x12c3, 0x12c5, 0x0001}, {0x12c8, 0x12d6, 0x0001}, {0x12d8, 0x1310, 0x0001},
    {0x1312, 0x1315, 0x0001}, {0x1318, 0x135a, 0x0001}, {0x135d, 0x135f, 0x0001}, {0x1369, 0x1371, 0x0001},
    {0x1380, 0x138f, 0x0001}, {0x13a0, 0x13f5, 0x0001}, {0x13f8, 0x13fd, 0x0001}, {0x1401, 0x166c, 0x0001},
    {0x166f, 0x167f, 0x0001}, {0x1681, 0x169a, 0x0001}, {0x16a0, 0x16ea, 0x0001}, {0x16ee, 0x16f8, 0x0001},
    {0x1700, 0x1715, 0x0001}, {0x171f, 0x1734, 0x0001}, {0x1740, 0x1753, 0x0001}, {0x1760, 0x176c, 0x0001},
    {0x176e, 0x1770, 0x0001}, {0x1772, 0x1773, 0x0001}, {0x1780, 0x17d3, 0x0001}, {0x17d7, 0x17dc, 0x0005},
    {0x17dd, 0x17e0, 0x0003}, {0x17e1, 0x17e9, 0x0001}, {0x180b, 0x180d, 0x0001}, {0x180f, 0x1819, 0x0001},
    {0x1820, 0x1878, 0x0001}, {0x1880, 0x18aa, 0x0001}, {0x18b0, 0x18f5, 0x0001}, {0x1900, 0x191e, 0x0001},
    {0x1920, 0x192b, 0x0001}, {0x1930, 0x193b, 0x0001}, {0x1946, 0x196d, 0x0001}, {0x1970, 0x1974, 0x0001},
    {0x1980, 0x19ab, 0x0001}, {0x19b0, 0x19c9, 0x0001}, {0x19d0, 0x19da, 0x0001}, {0x1a00, 0x1a1b, 0x0001},
    {0x1a20, 0x1a5e, 0x0001}, {0x1a60, 0x1a7c, 0x0001}, {0x1a7f, 0x1a89, 0x0001}, {0x1a90, 0x1a99, 0x0001},
    {0x1aa7, 0x1ab0, 0x0009}, {0x1ab1, 0x1abd, 0x0001}, {0x1abf, 0x1add, 0x0001}, {0x1ae0, 0x1aeb, 0x0001},
    {0x1b00, 0x1b4c, 0x0001}, {0x1b50, 0x1b59, 0x0001}, {0x1b6b, 0x1b73, 0x0001}, {0x1b80, 0x1bf3, 0x0001},
    {0x1c00, 0x1c37, 0x0001}, {0x1c40, 0x1c49, 0x0001}, {0x1c4d, 0x1c7d, 0x0001}, {0x1c80, 0x1c8a, 0x0001},
    {0x1c90, 0x1cba, 0x0001}, {0x1cbd, 0x1cbf, 0x0001}, {0x1cd0, 0x1cd2, 0x0001}, {0x1cd4, 0x1cfa, 0x0001},
    {0x1d00, 0x1f15, 0x0001}, {0x1f18, 0x1f1d, 0x0001}, {0x1f20, 0x1f45, 0x0001}, {0x1f48, 0x1f4d, 0x0001},
    {0x1f50, 0x1f57, 0x0001}, {0x1f59, 0x1f5f, 0x0002}, {0x1f60, 0x1f7d, 0x0001}, {0x1f80, 0x1fb4, 0x0001},
    {0x1fb6, 0x1fbc, 0x0001}, {0x1fbe, 0x1fc2, 0x0004}, {0x1fc3, 0x1fc4, 0x0001}, {0x1fc6, 0x1fcc, 0x0001},
    {0x1fd0, 0x1fd3, 0x0001}, {0x1fd6, 0x1fdb, 0x0001}, {0x1fe0, 0x1fec, 0x0001}, {0x1ff2, 0x1ff4, 0x0001},
    {0x1ff6, 0x1ffc, 0x0001}, {0x200c, 0x200d, 0x0001}, {0x203f, 0x2040, 0x0001}, {0x2054, 0x2071, 0x001d},
    {0x207f, 0x2090, 0x0011}, {0x2091, 0x209c, 0x0001}, {0x20d0, 0x20dc, 0x0001}, {0x20e1, 0x20e5, 0x0004},
    {0x20e6, 0x20f0, 0x0001}, {0x2102, 0x2107, 0x0005}, {0x210a, 0x2113, 0x0001}, {0x2115, 0x2118, 0x0003},
    {0x2119, 0x211d, 0x0001}, {0x2124, 0x212a, 0x0002}, {0x212b, 0x2139, 0x0001}, {0x213c, 0x213f, 0x0001},
    {0x2145, 0x2149, 0x0001}, {0x214e, 0x2160, 0x0012}, {0x2161, 0x2188, 0x0001}, {0x2c00, 0x2ce4, 0x0001},
    {0x2ceb, 0x2cf3, 0x0001}, {0x2d00, 0x2d25, 0x0001}, {0x2d27, 0x2d2d, 0x0006}, {0x2d30, 0x2d67, 0x0001},
    {0x2d6f, 0x2d7f, 0x0010}, {0x2d80, 0x2d96, 0x0001}, {0x2da0, 0x2da6, 0x0001}, {0x2da8, 0x2dae, 0x0001},
    {0x2db0, 0x2db6, 0x0001}, {0x2db8, 0x2dbe, 0x0001}, {0x2dc0, 0x2dc6, 0x0001}, {0x2dc8, 0x2dce, 0x0001},
    {0x2dd0, 0x2dd6, 0x0001}, {0x2dd8, 0x2dde, 0x0001}, {0x2de0, 0x2dff, 0x0001}, {0x3005, 0x3007, 0x0001},
    {0x3021, 0x302f, 0x0001}, {0x3031, 0x3035, 0x0001}, {0x3038, 0x303c, 0x0001}, {0x3041, 0x3096, 0x0001},
    {0x3099, 0x309a, 0x0001}, {0x309d, 0x309f, 0x0001}, {0x30a1, 0x30ff, 0x0001}, {0x3105, 0x312f, 0x0001},
    {0x3131, 0x318e, 0x0001}, {0x31a0, 0x31bf, 0x0001}, {0x31f0, 0x31ff, 0x0001}, {0x3400, 0x4dbf, 0x0001},
    {0x4e00, 0xa48c, 0x0001}, {0xa4d0, 0xa4fd, 0x0001}, {0xa500, 0xa60c, 0x0001}, {0xa610, 0xa62b, 0x0001},
    {0xa640, 0xa66f, 0x0001}, {0xa674, 0xa67d, 0x0001}, {0xa67f, 0xa6f1, 0x0001}, {0xa717, 0xa71f, 0x0001},
    {0xa722, 0xa788, 0x0001}, {0xa78b, 0xa7dc, 0x0001}, {0xa7f1, 0xa827, 0x0001}, {0xa82c, 0xa840, 0x0014},
    {0xa841, 0xa873, 0x0001}, {0xa880, 0xa8c5, 0x0001}, {0xa8d0, 0xa8d9, 0x0001}, {0xa8e0, 0xa8f7, 0x0001},
    {0xa8fb, 0xa8fd, 0x0002}, {0xa8fe, 0xa92d, 0x0001}, {0xa930, 0xa953, 0x0001}, {0xa960, 0xa97c, 0x0001},
    {0xa980, 0xa9c0, 0x0001}, {0xa9cf, 0xa9d9, 0x0001}, {0xa9e0, 0xa9fe, 0x0001}, {0xaa00, 0xaa36, 0x0001},
    {0xaa40, 0xaa4d, 0x0001}, {0xaa50, 0xaa59, 0x0001}, {0xaa60, 0xaa76, 0x0001}, {0xaa7a, 0xaac2, 0x0001},
    {0xaadb, 0xaadd, 0x0001}, {0xaae0, 0xaaef, 0x0001}, {0xaaf2, 0xaaf6, 0x0001}, {0xab01, 0xab06, 0x0001},
    {0xab09, 0xab0e, 0x0001}, {0xab11, 0xab16, 0x0001}, {0xab20, 0xab26, 0x0001}, {0xab28, 0xab2e, 0x0001},
    {0xab30, 0xab5a, 0x0001}, {0xab5c, 0xab69, 0x0001}, {0xab70, 0xabea, 0x0001}, {0xabec, 0xabed, 0x0001},
    {0xabf0, 0xabf9, 0x0001}, {0xac00, 0xd7a3, 0x0001}, {0xd7b0, 0xd7c6, 0x0001}, {0xd7cb, 0xd7fb, 0x0001},
    {0xf900, 0xfa6d, 0x0001}, {0xfa70, 0xfad9, 0x0001}, {0xfb00, 0xfb06, 0x0001}, {0xfb13, 0xfb17, 0x0001},
    {0xfb1d, 0xfb28, 0x0001}, {0xfb2a, 0xfb36, 0x0001}, {0xfb38, 0xfb3c, 0x0001}, {0xfb3e, 0xfb40, 0x0002},
    {0xfb41, 0xfb43, 0x0002}, {0xfb44, 0xfb46, 0x0002}, {0xfb47, 0xfbb1, 0x0001}, {0xfbd3, 0xfc5d, 0x0001},
    {0xfc64, 0xfd3d, 0x0001}, {0xfd50, 0xfd8f, 0x0001}, {0xfd92, 0xfdc7, 0x0001}, {0xfdf0, 0xfdf9, 0x0001},
    {0xfe00, 0xfe0f, 0x0001}, {0xfe20, 0xfe2f, 0x0001}, {0xfe33, 0xfe34, 0x0001}, {0xfe4d, 0xfe4f, 0x0001},
    {0xfe71, 0xfe73, 0x0002}, {0xfe77, 0xfe7f, 0x0002}, {0xfe80, 0xfefc, 0x0001}, {0xff10, 0xff19, 0x0001},
    {0xff21, 0xff3a, 0x0001}, {0xff3f, 0xff41, 0x0002}, {0xff42, 0xff5a, 0x0001}, {0xff65, 0xffbe, 0x0001},
    {0xffc2, 0xffc7, 0x0001}, {0xffca, 0xffcf, 0x0001}, {0xffd2, 0xffd7, 0x0001}, {0xffda, 0xffdc, 0x0001}
};


//
//	xidContinue32
//

#if defined(IMGUI_USE_WCHAR32)

static Range32 xidContinue32[] = {
    {0x10000, 0x1000b, 0x0001}, {0x1000d, 0x10026, 0x0001}, {0x10028, 0x1003a, 0x0001}, {0x1003c, 0x1003d, 0x0001},
    {0x1003f, 0x1004d, 0x0001}, {0x10050, 0x1005d, 0x0001}, {0x10080, 0x100fa, 0x0001}, {0x10140, 0x10174, 0x0001},
    {0x101fd, 0x10280, 0x0083}, {0x10281, 0x1029c, 0x0001}, {0x102a0, 0x102d0, 0x0001}, {0x102e0, 0x10300, 0x0020},
    {0x10301, 0x1031f, 0x0001}, {0x1032d, 0x1034a, 0x0001}, {0x10350, 0x1037a, 0x0001}, {0x10380, 0x1039d, 0x0001},
    {0x103a0, 0x103c3, 0x0001}, {0x103c8, 0x103cf, 0x0001}, {0x103d1, 0x103d5, 0x0001}, {0x10400, 0x1049d, 0x0001},
    {0x104a0, 0x104a9, 0x0001}, {0x104b0, 0x104d3, 0x0001}, {0x104d8, 0x104fb, 0x0001}, {0x10500, 0x10527, 0x0001},
    {0x10530, 0x10563, 0x0001}, {0x10570, 0x1057a, 0x0001}, {0x1057c, 0x1058a, 0x0001}, {0x1058c, 0x10592, 0x0001},
    {0x10594, 0x10595, 0x0001}, {0x10597, 0x105a1, 0x0001}, {0x105a3, 0x105b1, 0x0001}, {0x105b3, 0x105b9, 0x0001},
    {0x105bb, 0x105bc, 0x0001}, {0x105c0, 0x105f3, 0x0001}, {0x10600, 0x10736, 0x0001}, {0x10740, 0x10755, 0x0001},
    {0x10760, 0x10767, 0x0001}, {0x10780, 0x10785, 0x0001}, {0x10787, 0x107b0, 0x0001}, {0x107b2, 0x107ba, 0x0001},
    {0x10800, 0x10805, 0x0001}, {0x10808, 0x1080a, 0x0002}, {0x1080b, 0x10835, 0x0001}, {0x10837, 0x10838, 0x0001},
    {0x1083c, 0x1083f, 0x0003}, {0x10840, 0x10855, 0x0001}, {0x10860, 0x10876, 0x0001}, {0x10880, 0x1089e, 0x0001},
    {0x108e0, 0x108f2, 0x0001}, {0x108f4, 0x108f5, 0x0001}, {0x10900, 0x10915, 0x0001}, {0x10920, 0x10939, 0x0001},
    {0x10940, 0x10959, 0x0001}, {0x10980, 0x109b7, 0x0001}, {0x109be, 0x109bf, 0x0001}, {0x10a00, 0x10a03, 0x0001},
    {0x10a05, 0x10a06, 0x0001}, {0x10a0c, 0x10a13, 0x0001}, {0x10a15, 0x10a17, 0x0001}, {0x10a19, 0x10a35, 0x0001},
    {0x10a38, 0x10a3a, 0x0001}, {0x10a3f, 0x10a60, 0x0021}, {0x10a61, 0x10a7c, 0x0001}, {0x10a80, 0x10a9c, 0x0001},
    {0x10ac0, 0x10ac7, 0x0001}, {0x10ac9, 0x10ae6, 0x0001}, {0x10b00, 0x10b35, 0x0001}, {0x10b40, 0x10b55, 0x0001},
    {0x10b60, 0x10b72, 0x0001}, {0x10b80, 0x10b91, 0x0001}, {0x10c00, 0x10c48, 0x0001}, {0x10c80, 0x10cb2, 0x0001},
    {0x10cc0, 0x10cf2, 0x0001}, {0x10d00, 0x10d27, 0x0001}, {0x10d30, 0x10d39, 0x0001}, {0x10d40, 0x10d65, 0x0001},
    {0x10d69, 0x10d6d, 0x0001}, {0x10d6f, 0x10d85, 0x0001}, {0x10e80, 0x10ea9, 0x0001}, {0x10eab, 0x10eac, 0x0001},
    {0x10eb0, 0x10eb1, 0x0001}, {0x10ec2, 0x10ec7, 0x0001}, {0x10efa, 0x10f1c, 0x0001}, {0x10f27, 0x10f30, 0x0009},
    {0x10f31, 0x10f50, 0x0001}, {0x10f70, 0x10f85, 0x0001}, {0x10fb0, 0x10fc4, 0x0001}, {0x10fe0, 0x10ff6, 0x0001},
    {0x11000, 0x11046, 0x0001}, {0x11066, 0x11075, 0x0001}, {0x1107f, 0x110ba, 0x0001}, {0x110c2, 0x110d0, 0x000e},
    {0x110d1, 0x110e8, 0x0001}, {0x110f0, 0x110f9, 0x0001}, {0x11100, 0x11134, 0x0001}, {0x11136, 0x1113f, 0x0001},
    {0x11144, 0x11147, 0x0001}, {0x11150, 0x11173, 0x0001}, {0x11176, 0x11180, 0x000a}, {0x11181, 0x111c4, 0x0001},
    {0x111c9, 0x111cc, 0x0001}, {0x111ce, 0x111da, 0x0001}, {0x111dc, 0x11200, 0x0024}, {0x11201, 0x11211, 0x0001},
    {0x11213, 0x11237, 0x0001}, {0x1123e, 0x11241, 0x0001}, {0x11280, 0x11286, 0x0001}, {0x11288, 0x1128a, 0x0002},
    {0x1128b, 0x1128d, 0x0001}, {0x1128f, 0x1129d, 0x0001}, {0x1129f, 0x112a8, 0x0001}, {0x112b0, 0x112ea, 0x0001},
    {0x112f0, 0x112f9, 0x0001}, {0x11300, 0x11303, 0x0001}, {0x11305, 0x1130c, 0x0001}, {0x1130f, 0x11310, 0x0001},
    {0x11313, 0x11328, 0x0001}, {0x1132a, 0x11330, 0x0001}, {0x11332, 0x11333, 0x0001}, {0x11335, 0x11339, 0x0001},
    {0x1133b, 0x11344, 0x0001}, {0x11347, 0x11348, 0x0001}, {0x1134b, 0x1134d, 0x0001}, {0x11350, 0x11357, 0x0007},
    {0x1135d, 0x11363, 0x0001}, {0x11366, 0x1136c, 0x0001}, {0x11370, 0x11374, 0x0001}, {0x11380, 0x11389, 0x0001},
    {0x1138b, 0x1138e, 0x0003}, {0x11390, 0x113b5, 0x0001}, {0x113b7, 0x113c0, 0x0001}, {0x113c2, 0x113c5, 0x0003},
    {0x113c7, 0x113ca, 0x0001}, {0x113cc, 0x113d3, 0x0001}, {0x113e1, 0x113e2, 0x0001}, {0x11400, 0x1144a, 0x0001},
    {0x11450, 0x11459, 0x0001}, {0x1145e, 0x11461, 0x0001}, {0x11480, 0x114c5, 0x0001}, {0x114c7, 0x114d0, 0x0009},
    {0x114d1, 0x114d9, 0x0001}, {0x11580, 0x115b5, 0x0001}, {0x115b8, 0x115c0, 0x0001}, {0x115d8, 0x115dd, 0x0001},
    {0x11600, 0x11640, 0x0001}, {0x11644, 0x11650, 0x000c}, {0x11651, 0x11659, 0x0001}, {0x11680, 0x116b8, 0x0001},
    {0x116c0, 0x116c9, 0x0001}, {0x116d0, 0x116e3, 0x0001}, {0x11700, 0x1171a, 0x0001}, {0x1171d, 0x1172b, 0x0001},
    {0x11730, 0x11739, 0x0001}, {0x11740, 0x11746, 0x0001}, {0x11800, 0x1183a, 0x0001}, {0x118a0, 0x118e9, 0x0001},
    {0x118ff, 0x11906, 0x0001}, {0x11909, 0x1190c, 0x0003}, {0x1190d, 0x11913, 0x0001}, {0x11915, 0x11916, 0x0001},
    {0x11918, 0x11935, 0x0001}, {0x11937, 0x11938, 0x0001}, {0x1193b, 0x11943, 0x0001}, {0x11950, 0x11959, 0x0001},
    {0x119a0, 0x119a7, 0x0001}, {0x119aa, 0x119d7, 0x0001}, {0x119da, 0x119e1, 0x0001}, {0x119e3, 0x119e4, 0x0001},
    {0x11a00, 0x11a3e, 0x0001}, {0x11a47, 0x11a50, 0x0009}, {0x11a51, 0x11a99, 0x0001}, {0x11a9d, 0x11ab0, 0x0013},
    {0x11ab1, 0x11af8, 0x0001}, {0x11b60, 0x11b67, 0x0001}, {0x11bc0, 0x11be0, 0x0001}, {0x11bf0, 0x11bf9, 0x0001},
    {0x11c00, 0x11c08, 0x0001}, {0x11c0a, 0x11c36, 0x0001}, {0x11c38, 0x11c40, 0x0001}, {0x11c50, 0x11c59, 0x0001},
    {0x11c72, 0x11c8f, 0x0001}, {0x11c92, 0x11ca7, 0x0001}, {0x11ca9, 0x11cb6, 0x0001}, {0x11d00, 0x11d06, 0x0001},
    {0x11d08, 0x11d09, 0x0001}, {0x11d0b, 0x11d36, 0x0001}, {0x11d3a, 0x11d3c, 0x0002}, {0x11d3d, 0x11d3f, 0x0002},
    {0x11d40, 0x11d47, 0x0001}, {0x11d50, 0x11d59, 0x0001}, {0x11d60, 0x11d65, 0x0001}, {0x11d67, 0x11d68, 0x0001},
    {0x11d6a, 0x11d8e, 0x0001}, {0x11d90, 0x11d91, 0x0001}, {0x11d93, 0x11d98, 0x0001}, {0x11da0, 0x11da9, 0x0001},
    {0x11db0, 0x11ddb, 0x0001}, {0x11de0, 0x11de9, 0x0001}, {0x11ee0, 0x11ef6, 0x0001}, {0x11f00, 0x11f10, 0x0001},
    {0x11f12, 0x11f3a, 0x0001}, {0x11f3e, 0x11f42, 0x0001}, {0x11f50, 0x11f5a, 0x0001}, {0x11fb0, 0x12000, 0x0050},
    {0x12001, 0x12399, 0x0001}, {0x12400, 0x1246e, 0x0001}, {0x12480, 0x12543, 0x0001}, {0x12f90, 0x12ff0, 0x0001},
    {0x13000, 0x1342f, 0x0001}, {0x13440, 0x13455, 0x0001}, {0x13460, 0x143fa, 0x0001}, {0x14400, 0x14646, 0x0001},
    {0x16100, 0x16139, 0x0001}, {0x16800, 0x16a38, 0x0001}, {0x16a40, 0x16a5e, 0x0001}, {0x16a60, 0x16a69, 0x0001},
    {0x16a70, 0x16abe, 0x0001}, {0x16ac0, 0x16ac9, 0x0001}, {0x16ad0, 0x16aed, 0x0001}, {0x16af0, 0x16af4, 0x0001},
    {0x16b00, 0x16b36, 0x0001}, {0x16b40, 0x16b43, 0x0001}, {0x16b50, 0x16b59, 0x0001}, {0x16b63, 0x16b77, 0x0001},
    {0x16b7d, 0x16b8f, 0x0001}, {0x16d40, 0x16d6c, 0x0001}, {0x16d70, 0x16d79, 0x0001}, {0x16e40, 0x16e7f, 0x0001},
    {0x16ea0, 0x16eb8, 0x0001}, {0x16ebb, 0x16ed3, 0x0001}, {0x16f00, 0x16f4a, 0x0001}, {0x16f4f, 0x16f87, 0x0001},
    {0x16f8f, 0x16f9f, 0x0001}, {0x16fe0, 0x16fe1, 0x0001}, {0x16fe3, 0x16fe4, 0x0001}, {0x16ff0, 0x16ff6, 0x0001},
    {0x17000, 0x18cd5, 0x0001}, {0x18cff, 0x18d1e, 0x0001}, {0x18d80, 0x18df2, 0x0001}, {0x1aff0, 0x1aff3, 0x0001},
    {0x1aff5, 0x1affb, 0x0001}, {0x1affd, 0x1affe, 0x0001}, {0x1b000, 0x1b122, 0x0001}, {0x1b132, 0x1b150, 0x001e},
    {0x1b151, 0x1b152, 0x0001}, {0x1b155, 0x1b164, 0x000f}, {0x1b165, 0x1b167, 0x0001}, {0x1b170, 0x1b2fb, 0x0001},
    {0x1bc00, 0x1bc6a, 0x0001}, {0x1bc70, 0x1bc7c, 0x0001}, {0x1bc80, 0x1bc88, 0x0001}, {0x1bc90, 0x1bc99, 0x0001},
    {0x1bc9d, 0x1bc9e, 0x0001}, {0x1ccf0, 0x1ccf9, 0x0001}, {0x1cf00, 0x1cf2d, 0x0001}, {0x1cf30, 0x1cf46, 0x0001},
    {0x1d165, 0x1d169, 0x0001}, {0x1d16d, 0x1d172, 0x0001}, {0x1d17b, 0x1d182, 0x0001}, {0x1d185, 0x1d18b, 0x0001},
    {0x1d1aa, 0x1d1ad, 0x0001}, {0x1d242, 0x1d244, 0x0001}, {0x1d400, 0x1d454, 0x0001}, {0x1d456, 0x1d49c, 0x0001},
    {0x1d49e, 0x1d49f, 0x0001}, {0x1d4a2, 0x1d4a5, 0x0003}, {0x1d4a6, 0x1d4a9, 0x0003}, {0x1d4aa, 0x1d4ac, 0x0001},
    {0x1d4ae, 0x1d4b9, 0x0001}, {0x1d4bb, 0x1d4bd, 0x0002}, {0x1d4be, 0x1d4c3, 0x0001}, {0x1d4c5, 0x1d505, 0x0001},
    {0x1d507, 0x1d50a, 0x0001}, {0x1d50d, 0x1d514, 0x0001}, {0x1d516, 0x1d51c, 0x0001}, {0x1d51e, 0x1d539, 0x0001},
    {0x1d53b, 0x1d53e, 0x0001}, {0x1d540, 0x1d544, 0x0001}, {0x1d546, 0x1d54a, 0x0004}, {0x1d54b, 0x1d550, 0x0001},
    {0x1d552, 0x1d6a5, 0x0001}, {0x1d6a8, 0x1d6c0, 0x0001}, {0x1d6c2, 0x1d6da, 0x0001}, {0x1d6dc, 0x1d6fa, 0x0001},
    {0x1d6fc, 0x1d714, 0x0001}, {0x1d716, 0x1d734, 0x0001}, {0x1d736, 0x1d74e, 0x0001}, {0x1d750, 0x1d76e, 0x0001},
    {0x1d770, 0x1d788, 0x0001}, {0x1d78a, 0x1d7a8, 0x0001}, {0x1d7aa, 0x1d7c2, 0x0001}, {0x1d7c4, 0x1d7cb, 0x0001},
    {0x1d7ce, 0x1d7ff, 0x0001}, {0x1da00, 0x1da36, 0x0001}, {0x1da3b, 0x1da6c, 0x0001}, {0x1da75, 0x1da84, 0x000f},
    {0x1da9b, 0x1da9f, 0x0001}, {0x1daa1, 0x1daaf, 0x0001}, {0x1df00, 0x1df1e, 0x0001}, {0x1df25, 0x1df2a, 0x0001},
    {0x1e000, 0x1e006, 0x0001}, {0x1e008, 0x1e018, 0x0001}, {0x1e01b, 0x1e021, 0x0001}, {0x1e023, 0x1e024, 0x0001},
    {0x1e026, 0x1e02a, 0x0001}, {0x1e030, 0x1e06d, 0x0001}, {0x1e08f, 0x1e100, 0x0071}, {0x1e101, 0x1e12c, 0x0001},
    {0x1e130, 0x1e13d, 0x0001}, {0x1e140, 0x1e149, 0x0001}, {0x1e14e, 0x1e290, 0x0142}, {0x1e291, 0x1e2ae, 0x0001},
    {0x1e2c0, 0x1e2f9, 0x0001}, {0x1e4d0, 0x1e4f9, 0x0001}, {0x1e5d0, 0x1e5fa, 0x0001}, {0x1e6c0, 0x1e6de, 0x0001},
    {0x1e6e0, 0x1e6f5, 0x0001}, {0x1e6fe, 0x1e6ff, 0x0001}, {0x1e7e0, 0x1e7e6, 0x0001}, {0x1e7e8, 0x1e7eb, 0x0001},
    {0x1e7ed, 0x1e7ee, 0x0001}, {0x1e7f0, 0x1e7fe, 0x0001}, {0x1e800, 0x1e8c4, 0x0001}, {0x1e8d0, 0x1e8d6, 0x0001},
    {0x1e900, 0x1e94b, 0x0001}, {0x1e950, 0x1e959, 0x0001}, {0x1ee00, 0x1ee03, 0x0001}, {0x1ee05, 0x1ee1f, 0x0001},
    {0x1ee21, 0x1ee22, 0x0001}, {0x1ee24, 0x1ee27, 0x0003}, {0x1ee29, 0x1ee32, 0x0001}, {0x1ee34, 0x1ee37, 0x0001},
    {0x1ee39, 0x1ee3b, 0x0002}, {0x1ee42, 0x1ee47, 0x0005}, {0x1ee49, 0x1ee4d, 0x0002}, {0x1ee4e, 0x1ee4f, 0x0001},
    {0x1ee51, 0x1ee52, 0x0001}, {0x1ee54, 0x1ee57, 0x0003}, {0x1ee59, 0x1ee61, 0x0002}, {0x1ee62, 0x1ee64, 0x0002},
    {0x1ee67, 0x1ee6a, 0x0001}, {0x1ee6c, 0x1ee72, 0x0001}, {0x1ee74, 0x1ee77, 0x0001}, {0x1ee79, 0x1ee7c, 0x0001},
    {0x1ee7e, 0x1ee80, 0x0002}, {0x1ee81, 0x1ee89, 0x0001}, {0x1ee8b, 0x1ee9b, 0x0001}, {0x1eea1, 0x1eea3, 0x0001},
    {0x1eea5, 0x1eea9, 0x0001}, {0x1eeab, 0x1eebb, 0x0001}, {0x1fbf0, 0x1fbf9, 0x0001}, {0x20000, 0x2a6df, 0x0001},
    {0x2a700, 0x2b81d, 0x0001}, {0x2b820, 0x2cead, 0x0001}, {0x2ceb0, 0x2ebe0, 0x0001}, {0x2ebf0, 0x2ee5d, 0x0001},
    {0x2f800, 0x2fa1d, 0x0001}, {0x30000, 0x3134a, 0x0001}, {0x31350, 0x33479, 0x0001}, {0xe0100, 0xe01ef, 0x0001}
};

#endif


//
//	case16
//

static CaseRange16 case16[] = {
    {0x0041, 0x005a,      0,     32}, {0x0061, 0x007a,    -32,      0}, {0x00b5, 0x00b5,    743,      0},
    {0x00c0, 0x00d6,      0,     32}, {0x00d8, 0x00de,      0,     32}, {0x00e0, 0x00f6,    -32,      0},
    {0x00f8, 0x00fe,    -32,      0}, {0x00ff, 0x00ff,    121,      0}, {0x0100, 0x012f,  65535,  65535},
    {0x0130, 0x0130,      0,   -199}, {0x0131, 0x0131,   -232,      0}, {0x0132, 0x0177,  65535,  65535},
    {0x0178, 0x0178,      0,   -121}, {0x0179, 0x017e,  65535,  65535}, {0x017f, 0x017f,   -300,      0},
    {0x0180, 0x0180,    195,      0}, {0x0181, 0x0181,      0,    210}, {0x0182, 0x0185,  65535,  65535},
    {0x0186, 0x0186,      0,    206}, {0x0187, 0x0188,  65535,  65535}, {0x0189, 0x018a,      0,    205},
    {0x018b, 0x018c,  65535,  65535}, {0x018e, 0x018e,      0,     79}, {0x018f, 0x018f,      0,    202},
    {0x0190, 0x0190,      0,    203}, {0x0191, 0x0192,  65535,  65535}, {0x0193, 0x0193,      0,    205},
    {0x0194, 0x0194,      0,    207}, {0x0195, 0x0195,     97,      0}, {0x0196, 0x0196,      0,    211},
    {0x0197, 0x0197,      0,    209}, {0x0198, 0x0199,  65535,  65535}, {0x019a, 0x019a,    163,      0},
    {0x019b, 0x019b,  42561,      0}, {0x019c, 0x019c,      0,    211}, {0x019d, 0x019d,      0,    213},
    {0x019e, 0x019e,    130,      0}, {0x019f, 0x019f,      0,    214}, {0x01a0, 0x01a5,  65535,  65535},
    {0x01a6, 0x01a6,      0,    218}, {0x01a7, 0x01a8,  65535,  65535}, {0x01a9, 0x01a9,      0,    218},
    {0x01ac, 0x01ad,  65535,  65535}, {0x01ae, 0x01ae,      0,    218}, {0x01af, 0x01b0,  65535,  65535},
    {0x01b1, 0x01b2,      0,    217}, {0x01b3, 0x01b6,  65535,  65535}, {0x01b7, 0x01b7,      0,    219},
    {0x01b8, 0x01bd,  65535,  65535}, {0x01bf, 0x01bf,     56,      0}, {0x01c4, 0x01c4,      0,      2},
    {0x01c6, 0x01c6,     -2,      0}, {0x01c7, 0x01c7,      0,      2}, {0x01c9, 0x01c9,     -2,      0},
    {0x01ca, 0x01ca,      0,      2}, {0x01cc, 0x01cc,     -2,      0}, {0x01cd, 0x01dc,  65535,  65535},
    {0x01dd, 0x01dd,    -79,      0}, {0x01de, 0x01ef,  65535,  65535}, {0x01f1, 0x01f1,      0,      2},
    {0x01f3, 0x01f3,     -2,      0}, {0x01f4, 0x01f5,  65535,  65535}, {0x01f6, 0x01f6,      0,    -97},
    {0x01f7, 0x01f7,      0,    -56}, {0x01f8, 0x021f,  65535,  65535}, {0x0220, 0x0220,      0,   -130},
    {0x0222, 0x0233,  65535,  65535}, {0x023a, 0x023a,      0,  10795}, {0x023b, 0x023c,  65535,  65535},
    {0x023d, 0x023d,      0,   -163}, {0x023e, 0x023e,      0,  10792}, {0x023f, 0x0240,  10815,      0},
    {0x0241, 0x0242,  65535,  65535}, {0x0243, 0x0243,      0,   -195}, {0x0244, 0x0244,      0,     69},
    {0x0245, 0x0245,      0,     71}, {0x0246, 0x024f,  65535,  65535}, {0x0250, 0x0250,  10783,      0},
    {0x0251, 0x0251,  10780,      0}, {0x0252, 0x0252,  10782,      0}, {0x0253, 0x0253,   -210,      0},
    {0x0254, 0x0254,   -206,      0}, {0x0256, 0x0257,   -205,      0}, {0x0259, 0x0259,   -202,      0},
    {0x025b, 0x025b,   -203,      0}, {0x025c, 0x025c,  42319,      0}, {0x0260, 0x0260,   -205,      0},
    {0x0261, 0x0261,  42315,      0}, {0x0263, 0x0263,   -207,      0}, {0x0264, 0x0264,  42343,      0},
    {0x0265, 0x0265,  42280,      0}, {0x0266, 0x0266,  42308,      0}, {0x0268, 0x0268,   -209,      0},
    {0x0269, 0x0269,   -211,      0}, {0x026a, 0x026a,  42308,      0}, {0x026b, 0x026b,  10743,      0},
    {0x026c, 0x026c,  42305,      0}, {0x026f, 0x026f,   -211,      0}, {0x0271, 0x0271,  10749,      0},
    {0x0272, 0x0272,   -213,      0}, {0x0275, 0x0275,   -214,      0}, {0x027d, 0x027d,  10727,      0},
    {0x0280, 0x0280,   -218,      0}, {0x0282, 0x0282,  42307,      0}, {0x0283, 0x0283,   -218,      0},
    {0x0287, 0x0287,  42282,      0}, {0x0288, 0x0288,   -218,      0}, {0x0289, 0x0289,    -69,      0},
    {0x028a, 0x028b,   -217,      0}, {0x028c, 0x028c,    -71,      0}, {0x0292, 0x0292,   -219,      0},
    {0x029d, 0x029d,  42261,      0}, {0x029e, 0x029e,  42258,      0}, {0x0370, 0x0377,  65535,  65535},
    {0x037b, 0x037d,    130,      0}, {0x037f, 0x037f,      0,    116}, {0x0386, 0x0386,      0,     38},
    {0x0388, 0x038a,      0,     37}, {0x038c, 0x038c,      0,     64}, {0x038e, 0x038f,      0,     63},
    {0x0391, 0x03a1,      0,     32}, {0x03a3, 0x03ab,      0,     32}, {0x03ac, 0x03ac,    -38,      0},
    {0x03ad, 0x03af,    -37,      0}, {0x03b1, 0x03c1,    -32,      0}, {0x03c2, 0x03c2,    -31,      0},
    {0x03c3, 0x03cb,    -32,      0}, {0x03cc, 0x03cc,    -64,      0}, {0x03cd, 0x03ce,    -63,      0},
    {0x03cf, 0x03cf,      0,      8}, {0x03d0, 0x03d0,    -62,      0}, {0x03d1, 0x03d1,    -57,      0},
    {0x03d5, 0x03d5,    -47,      0}, {0x03d6, 0x03d6,    -54,      0}, {0x03d7, 0x03d7,     -8,      0},
    {0x03d8, 0x03ef,  65535,  65535}, {0x03f0, 0x03f0,    -86,      0}, {0x03f1, 0x03f1,    -80,      0},
    {0x03f2, 0x03f2,      7,      0}, {0x03f3, 0x03f3,   -116,      0}, {0x03f4, 0x03f4,      0,    -60},
    {0x03f5, 0x03f5,    -96,      0}, {0x03f7, 0x03f8,  65535,  65535}, {0x03f9, 0x03f9,      0,     -7},
    {0x03fa, 0x03fb,  65535,  65535}, {0x03fd, 0x03ff,      0,   -130}, {0x0400, 0x040f,      0,     80},
    {0x0410, 0x042f,      0,     32}, {0x0430, 0x044f,    -32,      0}, {0x0450, 0x045f,    -80,      0},
    {0x0460, 0x04bf,  65535,  65535}, {0x04c0, 0x04c0,      0,     15}, {0x04c1, 0x04ce,  65535,  65535},
    {0x04cf, 0x04cf,    -15,      0}, {0x04d0, 0x052f,  65535,  65535}, {0x0531, 0x0556,      0,     48},
    {0x0561, 0x0586,    -48,      0}, {0x10a0, 0x10c5,      0,   7264}, {0x10c7, 0x10c7,      0,   7264},
    {0x10cd, 0x10cd,      0,   7264}, {0x10d0, 0x10fa,   3008,      0}, {0x10fd, 0x10ff,   3008,      0},
    {0x13a0, 0x13ef,      0,  38864}, {0x13f0, 0x13f5,      0,      8}, {0x13f8, 0x13fd,     -8,      0},
    {0x1c80, 0x1c80,  -6254,      0}, {0x1c81, 0x1c81,  -6253,      0}, {0x1c82, 0x1c82,  -6244,      0},
    {0x1c83, 0x1c84,  -6242,      0}, {0x1c85, 0x1c85,  -6243,      0}, {0x1c86, 0x1c86,  -6236,      0},
    {0x1c87, 0x1c87,  -6181,      0}, {0x1c88, 0x1c88,  35266,      0}, {0x1c89, 0x1c8a,  65535,  65535},
    {0x1c90, 0x1cba,      0,  -3008}, {0x1cbd, 0x1cbf,      0,  -3008}, {0x1d79, 0x1d79,  35332,      0},
    {0x1d7d, 0x1d7d,   3814,      0}, {0x1d8e, 0x1d8e,  35384,      0}, {0x1e00, 0x1e95,  65535,  65535},
    {0x1e9b, 0x1e9b,    -59,      0}, {0x1e9e, 0x1e9e,      0,  -7615}, {0x1ea0, 0x1eff,  65535,  65535},
    {0x1f00, 0x1f07,      8,      0}, {0x1f08, 0x1f0f,      0,     -8}, {0x1f10, 0x1f15,      8,      0},
    {0x1f18, 0x1f1d,      0,     -8}, {0x1f20, 0x1f27,      8,      0}, {0x1f28, 0x1f2f,      0,     -8},
    {0x1f30, 0x1f37,      8,      0}, {0x1f38, 0x1f3f,      0,     -8}, {0x1f40, 0x1f45,      8,      0},
    {0x1f48, 0x1f4d,      0,     -8}, {0x1f51, 0x1f51,      8,      0}, {0x1f53, 0x1f53,      8,      0},
    {0x1f55, 0x1f55,      8,      0}, {0x1f57, 0x1f57,      8,      0}, {0x1f59, 0x1f59,      0,     -8},
    {0x1f5b, 0x1f5b,      0,     -8}, {0x1f5d, 0x1f5d,      0,     -8}, {0x1f5f, 0x1f5f,      0,     -8},
    {0x1f60, 0x1f67,      8,      0}, {0x1f68, 0x1f6f,      0,     -8}, {0x1f70, 0x1f71,     74,      0},
    {0x1f72, 0x1f75,     86,      0}, {0x1f76, 0x1f77,    100,      0}, {0x1f78, 0x1f79,    128,      0},
    {0x1f7a, 0x1f7b,    112,      0}, {0x1f7c, 0x1f7d,    126,      0}, {0x1f80, 0x1f87,      8,      0},
    {0x1f90, 0x1f97,      8,      0}, {0x1fa0, 0x1fa7,      8,      0}, {0x1fb0, 0x1fb1,      8,      0},
    {0x1fb3, 0x1fb3,      9,      0}, {0x1fb8, 0x1fb9,      0,     -8}, {0x1fba, 0x1fbb,      0,    -74},
    {0x1fbe, 0x1fbe,  -7205,      0}, {0x1fc3, 0x1fc3,      9,      0}, {0x1fc8, 0x1fcb,      0,    -86},
    {0x1fd0, 0x1fd1,      8,      0}, {0x1fd8, 0x1fd9,      0,     -8}, {0x1fda, 0x1fdb,      0,   -100},
    {0x1fe0, 0x1fe1,      8,      0}, {0x1fe5, 0x1fe5,      7,      0}, {0x1fe8, 0x1fe9,      0,     -8},
    {0x1fea, 0x1feb,      0,   -112}, {0x1fec, 0x1fec,      0,     -7}, {0x1ff3, 0x1ff3,      9,      0},
    {0x1ff8, 0x1ff9,      0,   -128}, {0x1ffa, 0x1ffb,      0,   -126}, {0x2126, 0x2126,      0,  -7517},
    {0x212a, 0x212a,      0,  -8383}, {0x212b, 0x212b,      0,  -8262}, {0x2132, 0x2132,      0,     28},
    {0x214e, 0x214e,    -28,      0}, {0x2183, 0x2184,  65535,  65535}, {0x2c00, 0x2c2f,      0,     48},
    {0x2c30, 0x2c5f,    -48,      0}, {0x2c60, 0x2c61,  65535,  65535}, {0x2c62, 0x2c62,      0, -10743},
    {0x2c63, 0x2c63,      0,  -3814}, {0x2c64, 0x2c64,      0, -10727}, {0x2c65, 0x2c65, -10795,      0},
    {0x2c66, 0x2c66, -10792,      0}, {0x2c67, 0x2c6c,  65535,  65535}, {0x2c6d, 0x2c6d,      0, -10780},
    {0x2c6e, 0x2c6e,      0, -10749}, {0x2c6f, 0x2c6f,      0, -10783}, {0x2c70, 0x2c70,      0, -10782},
    {0x2c72, 0x2c76,  65535,  65535}, {0x2c7e, 0x2c7f,      0, -10815}, {0x2c80, 0x2cf3,  65535,  65535},
    {0x2d00, 0x2d25,  -7264,      0}, {0x2d27, 0x2d27,  -7264,      0}, {0x2d2d, 0x2d2d,  -7264,      0},
    {0xa640, 0xa77c,  65535,  65535}, {0xa77d, 0xa77d,      0, -35332}, {0xa77e, 0xa78c,  65535,  65535},
    {0xa78d, 0xa78d,      0, -42280}, {0xa790, 0xa793,  65535,  65535}, {0xa794, 0xa794,     48,      0},
    {0xa796, 0xa7a9,  65535,  65535}, {0xa7aa, 0xa7aa,      0, -42308}, {0xa7ab, 0xa7ab,      0, -42319},
    {0xa7ac, 0xa7ac,      0, -42315}, {0xa7ad, 0xa7ad,      0, -42305}, {0xa7ae, 0xa7ae,      0, -42308},
    {0xa7b0, 0xa7b0,      0, -42258}, {0xa7b1, 0xa7b1,      0, -42282}, {0xa7b2, 0xa7b2,      0, -42261},
    {0xa7b3, 0xa7b3,      0,    928}, {0xa7b4, 0xa7c3,  65535,  65535}, {0xa7c4, 0xa7c4,      0,    -48},
    {0xa7c5, 0xa7c5,      0, -42307}, {0xa7c6, 0xa7c6,      0, -35384}, {0xa7c7, 0xa7ca,  65535,  65535},
    {0xa7cb, 0xa7cb,      0, -42343}, {0xa7cc, 0xa7db,  65535,  65535}, {0xa7dc, 0xa7dc,      0, -42561},
    {0xa7f5, 0xa7f6,  65535,  65535}, {0xab53, 0xab53,   -928,      0}, {0xab70, 0xabbf, -38864,      0},
    {0xff21, 0xff3a,      0,     32}, {0xff41, 0xff5a,    -32,      0}
};


//
//	case32
//

#if defined(IMGUI_USE_WCHAR32)

static CaseRange32 case32[] = {
    {0x10400, 0x10427,   0, 40}, {0x10428, 0x1044f, -40,  0}, {0x104b0, 0x104d3,   0, 40}, {0x104d8, 0x104fb, -40,  0},
    {0x10570, 0x1057a,   0, 39}, {0x1057c, 0x1058a,   0, 39}, {0x1058c, 0x10592,   0, 39}, {0x10594, 0x10595,   0, 39},
    {0x10597, 0x105a1, -39,  0}, {0x105a3, 0x105b1, -39,  0}, {0x105b3, 0x105b9, -39,  0}, {0x105bb, 0x105bc, -39,  0},
    {0x10c80, 0x10cb2,   0, 64}, {0x10cc0, 0x10cf2, -64,  0}, {0x10d50, 0x10d65,   0, 32}, {0x10d70, 0x10d85, -32,  0},
    {0x118a0, 0x118bf,   0, 32}, {0x118c0, 0x118df, -32,  0}, {0x16e40, 0x16e5f,   0, 32}, {0x16e60, 0x16e7f, -32,  0},
    {0x16ea0, 0x16eb8,   0, 27}, {0x16ebb, 0x16ed3, -27,  0}, {0x1e900, 0x1e921,   0, 34}, {0x1e922, 0x1e943, -34,  0}
};

#endif


//
//	rangeContains
//

template <typename T, typename C>
bool rangeContains(const T& table, C codepoint) {
    auto low = std::begin(table);
    auto high = std::end(table);

    while (low <= high) {
        auto mid = low + (high - low) / 2;

        if (codepoint >= mid->low && codepoint <= mid->high) {
            return (mid->stride == 1) || ((codepoint - mid->low) % mid->stride == 0);

        } else if (codepoint < mid->low) {
            high = mid - 1;

        } else {
            low = mid + 1;
        }
    }

    return false;
}


//
//	caseRangeFind
//

template <typename T, typename C>
const CaseRange<C>* caseRangeFind(const T& table, C codepoint) {
    auto low = std::begin(table);
    auto high = std::end(table);

    while (low <= high) {
        auto mid = low + (high - low) / 2;

        if (codepoint >= mid->low && codepoint <= mid->high) {
            return mid;

        } else if (codepoint < mid->low) {
            high = mid - 1;

        } else {
            low = mid + 1;
        }
    }

    return nullptr;
}


//
//	caseRangeToUpper
//

template <typename T, typename C>
C caseRangeToUpper(const T& table, C codepoint) {
    auto caseRange = caseRangeFind(table, codepoint);

    if (!caseRange || caseRange->toUpper == 0) {
        return codepoint;

    } else if (caseRange->toUpper == 0xffff) {
        return codepoint & ~0x1;
    }

    else {
        return static_cast<C>(static_cast<int32_t>(codepoint) + caseRange->toUpper);
    }
}


//
//	caseRangeToLower
//

template <typename T, typename C>
C caseRangeToLower(const T& table, C codepoint) {
    auto caseRange = caseRangeFind(table, codepoint);

    if (!caseRange || caseRange->toLower == 0) {
        return codepoint;

    } else if (caseRange->toLower == 0xffff) {
        return codepoint | 0x1;
    }

    else {
        return static_cast<C>(static_cast<int32_t>(codepoint) + caseRange->toLower);
    }
}


//
//	Internal type conversions because "char" is signed
//

static inline ImWchar uch(char c) {
    return static_cast<ImWchar>(c);
}

static inline char sch(ImWchar i) {
    return static_cast<char>(i);
}


//
//	skipBOM
//

std::string_view::const_iterator TextEditor::CodePoint::skipBOM(std::string_view::const_iterator i, std::string_view::const_iterator end) {
    // skip Byte Order Mark (BOM) just in case there is one

    // Note: the standard states that:
    // Use of a BOM is neither required nor recommended for UTF-8
    static constexpr char bom1 = static_cast<char>(0xEF);
    static constexpr char bom2 = static_cast<char>(0xBB);
    static constexpr char bom3 = static_cast<char>(0xBF);
    return ((end - i) >= 3 && i[0] == bom1 && i[1] == bom2 && i[2] == bom3) ? i + 3 : i;
}


//
//	TextEditor::CodePoint::read
//

std::string_view::const_iterator TextEditor::CodePoint::read(std::string_view::const_iterator i, std::string_view::const_iterator end, ImWchar *codepoint) {
    // parse a UTF-8 sequence into a unicode codepoint
    if (i < end && (uch(*i) & 0x80) == 0) {
        *codepoint = uch(*i);
        i++;

    } else if (i + 1 < end && (uch(*i) & 0xE0) == 0xC0) {
        *codepoint = ((uch(*i) & 0x1f) << 6) | (uch(*(i + 1)) & 0x3f);
        i += 2;

    } else if (i + 2 < end && (uch(*i) & 0xF0) == 0xE0) {
        *codepoint = ((uch(*i) & 0x0f) << 12) | ((uch(*(i + 1)) & 0x3f) << 6) | (uch(*(i + 2)) & 0x3f);
        i += 3;

    } else if (i + 3 < end && (uch(*i) & 0xF8) == 0xF0) {
        #if defined(IMGUI_USE_WCHAR32)
        *codepoint = ((uch(*i) & 0x07) << 18) | ((uch(*(i + 1)) & 0x3f) << 12) | ((uch(*(i + 2)) & 0x3f) << 6) | (uch(*(i + 3)) & 0x3f);
        #else
        *codepoint = IM_UNICODE_CODEPOINT_INVALID;
        #endif
        i += 4;

    } else {
        *codepoint = IM_UNICODE_CODEPOINT_INVALID;
        i++;
    }

    return i;
}


//
//	TextEditor::CodePoint::write
//

size_t TextEditor::CodePoint::write(char* start, ImWchar codepoint) {
    // generate UTF-8 sequence from a unicode codepoint
    auto i = start;

    if (codepoint < 0x80) {
        *i++ = sch(codepoint);

    } else if (codepoint < 0x800) {
        *i++ = sch(0xc0 | ((codepoint >> 6) & 0x1f));
        *i++ = sch(0x80 | (codepoint & 0x3f));

        #if defined(IMGUI_USE_WCHAR32)
    } else if (codepoint < 0x10000) {
        *i++ = sch(0xe0 | ((codepoint >> 12) & 0x0f));
        *i++ = sch(0x80 | ((codepoint >> 6) & 0x3f));
        *i++ = sch(0x80 | (codepoint & 0x3f));

    } else if (codepoint >= 0x110000) {
        codepoint = IM_UNICODE_CODEPOINT_INVALID;
        *i++ = sch(0xe0 | ((codepoint >> 12) & 0x0f));
        *i++ = sch(0x80 | ((codepoint >> 6) & 0x3f));
        *i++ = sch(0x80 | (codepoint & 0x3f));

    } else {
        *i++ = sch(0xf0 | ((codepoint >> 18) & 0x07));
        *i++ = sch(0x80 | ((codepoint >> 12) & 0x3f));
        *i++ = sch(0x80 | ((codepoint >> 6) & 0x3f));
        *i++ = sch(0x80 | (codepoint & 0x3f));

        #else
    } else {
        *i++ = sch(0xe0 | ((codepoint >> 12) & 0x0f));
        *i++ = sch(0x80 | ((codepoint >> 6) & 0x3f));
        *i++ = sch(0x80 | (codepoint & 0x3f));
        #endif
    }

    return i - start;
}


//
//	TextEditor::CodePoint::isLetter
//

bool TextEditor::CodePoint::isLetter(ImWchar codepoint) {
    if (codepoint < 0x7f) {
        return static_cast<unsigned>((codepoint | 32) - 'a') < 26;

        #if defined(IMGUI_USE_WCHAR32)
    } else if (codepoint >= 0x10000) {
        return rangeContains(letters32, static_cast<ImWchar32>(codepoint));
        #endif

    } else {
        return rangeContains(letters16, static_cast<ImWchar16>(codepoint));
    }
}


//
//	TextEditor::CodePoint::isNumber
//

bool TextEditor::CodePoint::isNumber(ImWchar codepoint) {
    if (codepoint < 0x7f) {
        return static_cast<unsigned>(codepoint - '0') < 10;

        #if defined(IMGUI_USE_WCHAR32)
    } else if (codepoint >= 0x10000) {
        return rangeContains(numbers32, static_cast<ImWchar32>(codepoint));
        #endif

    } else {
        return rangeContains(numbers16, static_cast<ImWchar16>(codepoint));
    }
}


//
//	TextEditor::CodePoint::isXidStart
//

bool TextEditor::CodePoint::isXidStart(ImWchar codepoint) {
    if (codepoint < 0x7f) {
        return codepoint == '_' || static_cast<unsigned>((codepoint | 32) - 'a') < 26;

        #if defined(IMGUI_USE_WCHAR32)
    } else if (codepoint >= 0x10000) {
        return rangeContains(xidStart32, static_cast<ImWchar32>(codepoint));
        #endif

    } else {
        return rangeContains(xidStart16, static_cast<ImWchar16>(codepoint));
    }
}


//
//	TextEditor::CodePoint::isWhiteSpace
//

bool TextEditor::CodePoint::isWhiteSpace(ImWchar codepoint) {
    if (codepoint < 0x7f) {
        return codepoint == ' ' || static_cast<unsigned>(codepoint - '\t') < 5;

        #if defined(IMGUI_USE_WCHAR32)
    } else if (codepoint >= 0x10000) {
        return false;
        #endif

    } else {
        return rangeContains(whitespace16, static_cast<ImWchar16>(codepoint));
    }
}


//
//	TextEditor::CodePoint::isWord
//

bool TextEditor::CodePoint::isWord(ImWchar codepoint) {
    if (codepoint < 0x7f) {
        return
        (static_cast<unsigned>((codepoint | 32) - 'a') < 26) ||
        (static_cast<unsigned>(codepoint - '0') < 10) ||
        codepoint == '_';

        #if defined(IMGUI_USE_WCHAR32)
    } else if (codepoint >= 0x10000) {
        return
        rangeContains(letters32, static_cast<ImWchar32>(codepoint)) ||
        rangeContains(numbers32, static_cast<ImWchar32>(codepoint)) ||
        codepoint == '_';
        #endif

    } else {
        return
        rangeContains(letters16, static_cast<ImWchar16>(codepoint)) ||
        rangeContains(numbers16, static_cast<ImWchar16>(codepoint)) ||
        codepoint == '_';
    }
}


//
//	TextEditor::CodePoint::isXidContinue
//

bool TextEditor::CodePoint::isXidContinue(ImWchar codepoint) {
    if (codepoint < 0x7f) {
        return codepoint == '_' || (static_cast<unsigned>((codepoint | 32) - 'a') < 26) || (static_cast<unsigned>(codepoint - '0') < 10);

        #if defined(IMGUI_USE_WCHAR32)
    } else if (codepoint >= 0x10000) {
        return rangeContains(xidContinue32, static_cast<ImWchar16>(codepoint));
        #endif

    } else {
        return rangeContains(xidContinue16, static_cast<ImWchar16>(codepoint));
    }
}


//
//	TextEditor::CodePoint::isLower
//

bool TextEditor::CodePoint::isLower(ImWchar codepoint) {
    if (codepoint < 0x7f) {
        return static_cast<unsigned>(codepoint - 'a') < 26;

        #if defined(IMGUI_USE_WCHAR32)
    } else if (codepoint >= 0x10000) {
        return rangeContains(lower32, static_cast<ImWchar32>(codepoint));
        #endif

    } else {
        return rangeContains(lower16, static_cast<ImWchar16>(codepoint));
    }
}


//
//	TextEditor::CodePoint::isUpper
//

bool TextEditor::CodePoint::isUpper(ImWchar codepoint) {
    if (codepoint < 0x7f) {
        return static_cast<unsigned>(codepoint - 'A') < 26;

        #if defined(IMGUI_USE_WCHAR32)
    } else if (codepoint >= 0x10000) {
        return rangeContains(upper32, static_cast<ImWchar32>(codepoint));
        #endif

    } else {
        return rangeContains(upper16, static_cast<ImWchar16>(codepoint));
    }
}


//
//	TextEditor::CodePoint::toUpper
//

ImWchar TextEditor::CodePoint::toUpper(ImWchar codepoint) {
    if (codepoint < 0x7f) {
        return (static_cast<unsigned>(codepoint - 'a') < 26) ? codepoint & 0x5f : codepoint;

        #if defined(IMGUI_USE_WCHAR32)
    } else if (codepoint >= 0x10000) {
        return caseRangeToUpper(case32, static_cast<char32_t>(codepoint));
        #endif

    } else {
        return caseRangeToUpper(case16, static_cast<char16_t>(codepoint));
    }
}


//
//	TextEditor::CodePoint::toLower
//

ImWchar TextEditor::CodePoint::toLower(ImWchar codepoint) {
    if (codepoint < 0x7f) {
        return (static_cast<unsigned>(codepoint - 'A') < 26) ? codepoint | 32 : codepoint;

        #if defined(IMGUI_USE_WCHAR32)
    } else if (codepoint >= 0x10000) {
        return caseRangeToLower(case32, static_cast<char32_t>(codepoint));
        #endif

    } else {
        return caseRangeToLower(case16, static_cast<char16_t>(codepoint));
    }
}


//
//	getCStyleIdentifier
//

static TextEditor::Iterator getCStyleIdentifier(TextEditor::Iterator start, TextEditor::Iterator end) {
    if (start < end && TextEditor::CodePoint::isXidStart(*start)) {
        start++;

        while (start < end && TextEditor::CodePoint::isXidContinue(*start)) {
            start++;
        }
    }

    return start;
}


//
//	getCStyleNumber
//

static TextEditor::Iterator getCStyleNumber(TextEditor::Iterator start, TextEditor::Iterator end) {
    TextEditor::Iterator i = start;
    TextEditor::Iterator marker;


    {
        ImWchar yych;
        unsigned int yyaccept = 0;
        yych = i < end ? *i : 0;
        switch (yych) {
            case '.': goto yy3;
            case '0': goto yy4;
            case '1':
            case '2':
            case '3':
            case '4':
            case '5':
            case '6':
            case '7':
            case '8':
            case '9': goto yy6;
            default:
                if (i >= end) goto yy82;
                goto yy1;
        }
        yy1:
        ++i;
        yy2:
        { return start; }
        yy3:
        ++i;
        yych = i < end ? *i : 0;
        switch (yych) {
            case '0':
            case '1':
            case '2':
            case '3':
            case '4':
            case '5':
            case '6':
            case '7':
            case '8':
            case '9': goto yy8;
            default: goto yy2;
        }
        yy4:
        yyaccept = 0;
        ++i;
        marker = i;
        yych = i < end ? *i : 0;
        switch (yych) {
            case 0x00: goto yy5;
            case 'B':
            case 'b': goto yy16;
            case 'X':
            case 'x': goto yy20;
            default: goto yy13;
        }
        yy5:
        { return i; }
        yy6:
        yyaccept = 1;
        ++i;
        marker = i;
        yych = i < end ? *i : 0;
        switch (yych) {
            case '.': goto yy10;
            case '0':
            case '1':
            case '2':
            case '3':
            case '4':
            case '5':
            case '6':
            case '7':
            case '8':
            case '9': goto yy6;
            case 'E':
            case 'e': goto yy17;
            case 'L': goto yy22;
            case 'U':
            case 'u': goto yy23;
            case 'l': goto yy24;
            default: goto yy7;
        }
        yy7:
        { return i; }
        yy8:
        yyaccept = 2;
        ++i;
        marker = i;
        yych = i < end ? *i : 0;
        switch (yych) {
            case '0':
            case '1':
            case '2':
            case '3':
            case '4':
            case '5':
            case '6':
            case '7':
            case '8':
            case '9': goto yy8;
            case 'E':
            case 'e': goto yy25;
            case 'F':
            case 'L':
            case 'f':
            case 'l': goto yy26;
            default: goto yy9;
        }
        yy9:
        { return i; }
        yy10:
        yyaccept = 3;
        ++i;
        marker = i;
        yych = i < end ? *i : 0;
        switch (yych) {
            case '0':
            case '1':
            case '2':
            case '3':
            case '4':
            case '5':
            case '6':
            case '7':
            case '8':
            case '9': goto yy8;
            case 'E':
            case 'e': goto yy27;
            case 'F':
            case 'L':
            case 'f':
            case 'l': goto yy28;
            default: goto yy11;
        }
        yy11:
        { return i; }
        yy12:
        yyaccept = 0;
        ++i;
        marker = i;
        yych = i < end ? *i : 0;
        yy13:
        switch (yych) {
            case '.': goto yy10;
            case '0':
            case '1':
            case '2':
            case '3':
            case '4':
            case '5':
            case '6':
            case '7': goto yy12;
            case '8':
            case '9': goto yy14;
            case 'E':
            case 'e': goto yy17;
            case 'L': goto yy18;
            case 'U':
            case 'u': goto yy19;
            case 'l': goto yy21;
            default: goto yy5;
        }
        yy14:
        ++i;
        yych = i < end ? *i : 0;
        switch (yych) {
            case '.': goto yy10;
            case '0':
            case '1':
            case '2':
            case '3':
            case '4':
            case '5':
            case '6':
            case '7':
            case '8':
            case '9': goto yy14;
            case 'E':
            case 'e': goto yy17;
            default: goto yy15;
        }
        yy15:
        i = marker;
        switch (yyaccept) {
            case 0: goto yy5;
            case 1: goto yy7;
            case 2: goto yy9;
            case 3: goto yy11;
            default: goto yy40;
        }
        yy16:
        ++i;
        yych = i < end ? *i : 0;
        switch (yych) {
            case '0':
            case '1': goto yy29;
            default: goto yy15;
        }
        yy17:
        ++i;
        yych = i < end ? *i : 0;
        switch (yych) {
            case '+':
            case '-': goto yy31;
            case '0':
            case '1':
            case '2':
            case '3':
            case '4':
            case '5':
            case '6':
            case '7':
            case '8':
            case '9': goto yy32;
            default: goto yy15;
        }
        yy18:
        ++i;
        yych = i < end ? *i : 0;
        switch (yych) {
            case 'L': goto yy34;
            case 'U':
            case 'u': goto yy35;
            default: goto yy5;
        }
        yy19:
        ++i;
        yych = i < end ? *i : 0;
        switch (yych) {
            case 'L': goto yy36;
            case 'l': goto yy37;
            default: goto yy5;
        }
        yy20:
        ++i;
        yych = i < end ? *i : 0;
        switch (yych) {
            case '.': goto yy38;
            case '0':
            case '1':
            case '2':
            case '3':
            case '4':
            case '5':
            case '6':
            case '7':
            case '8':
            case '9':
            case 'A':
            case 'B':
            case 'C':
            case 'D':
            case 'E':
            case 'F':
            case 'a':
            case 'b':
            case 'c':
            case 'd':
            case 'e':
            case 'f': goto yy39;
            default: goto yy15;
        }
        yy21:
        ++i;
        yych = i < end ? *i : 0;
        switch (yych) {
            case 'U':
            case 'u': goto yy35;
            case 'l': goto yy34;
            default: goto yy5;
        }
        yy22:
        ++i;
        yych = i < end ? *i : 0;
        switch (yych) {
            case 'L': goto yy41;
            case 'U':
            case 'u': goto yy42;
            default: goto yy7;
        }
        yy23:
        ++i;
        yych = i < end ? *i : 0;
        switch (yych) {
            case 'L': goto yy43;
            case 'l': goto yy44;
            default: goto yy7;
        }
        yy24:
        ++i;
        yych = i < end ? *i : 0;
        switch (yych) {
            case 'U':
            case 'u': goto yy42;
            case 'l': goto yy41;
            default: goto yy7;
        }
        yy25:
        ++i;
        yych = i < end ? *i : 0;
        switch (yych) {
            case '+':
            case '-': goto yy45;
            case '0':
            case '1':
            case '2':
            case '3':
            case '4':
            case '5':
            case '6':
            case '7':
            case '8':
            case '9': goto yy46;
            default: goto yy15;
        }
        yy26:
        ++i;
        goto yy9;
        yy27:
        ++i;
        yych = i < end ? *i : 0;
        switch (yych) {
            case '+':
            case '-': goto yy47;
            case '0':
            case '1':
            case '2':
            case '3':
            case '4':
            case '5':
            case '6':
            case '7':
            case '8':
            case '9': goto yy48;
            default: goto yy15;
        }
        yy28:
        ++i;
        goto yy11;
        yy29:
        ++i;
        yych = i < end ? *i : 0;
        switch (yych) {
            case '0':
            case '1': goto yy29;
            case 'L': goto yy49;
            case 'U':
            case 'u': goto yy50;
            case 'l': goto yy51;
            default: goto yy30;
        }
        yy30:
        { return i; }
        yy31:
        ++i;
        yych = i < end ? *i : 0;
        switch (yych) {
            case '0':
            case '1':
            case '2':
            case '3':
            case '4':
            case '5':
            case '6':
            case '7':
            case '8':
            case '9': goto yy32;
            default: goto yy15;
        }
        yy32:
        ++i;
        yych = i < end ? *i : 0;
        switch (yych) {
            case '0':
            case '1':
            case '2':
            case '3':
            case '4':
            case '5':
            case '6':
            case '7':
            case '8':
            case '9': goto yy32;
            case 'F':
            case 'L':
            case 'f':
            case 'l': goto yy52;
            default: goto yy33;
        }
        yy33:
        { return i; }
        yy34:
        ++i;
        yych = i < end ? *i : 0;
        switch (yych) {
            case 'U':
            case 'u': goto yy35;
            default: goto yy5;
        }
        yy35:
        ++i;
        goto yy5;
        yy36:
        ++i;
        yych = i < end ? *i : 0;
        switch (yych) {
            case 'L': goto yy35;
            default: goto yy5;
        }
        yy37:
        ++i;
        yych = i < end ? *i : 0;
        switch (yych) {
            case 'l': goto yy35;
            default: goto yy5;
        }
        yy38:
        ++i;
        yych = i < end ? *i : 0;
        switch (yych) {
            case 0x00:
            case 'P':
            case 'p': goto yy15;
            default: goto yy54;
        }
        yy39:
        yyaccept = 4;
        ++i;
        marker = i;
        yych = i < end ? *i : 0;
        switch (yych) {
            case '.': goto yy55;
            case '0':
            case '1':
            case '2':
            case '3':
            case '4':
            case '5':
            case '6':
            case '7':
            case '8':
            case '9':
            case 'A':
            case 'B':
            case 'C':
            case 'D':
            case 'E':
            case 'F':
            case 'a':
            case 'b':
            case 'c':
            case 'd':
            case 'e':
            case 'f': goto yy39;
            case 'L': goto yy56;
            case 'P':
            case 'p': goto yy57;
            case 'U':
            case 'u': goto yy58;
            case 'l': goto yy59;
            default: goto yy40;
        }
        yy40:
        { return i; }
        yy41:
        ++i;
        yych = i < end ? *i : 0;
        switch (yych) {
            case 'U':
            case 'u': goto yy42;
            default: goto yy7;
        }
        yy42:
        ++i;
        goto yy7;
        yy43:
        ++i;
        yych = i < end ? *i : 0;
        switch (yych) {
            case 'L': goto yy42;
            default: goto yy7;
        }
        yy44:
        ++i;
        yych = i < end ? *i : 0;
        switch (yych) {
            case 'l': goto yy42;
            default: goto yy7;
        }
        yy45:
        ++i;
        yych = i < end ? *i : 0;
        switch (yych) {
            case '0':
            case '1':
            case '2':
            case '3':
            case '4':
            case '5':
            case '6':
            case '7':
            case '8':
            case '9': goto yy46;
            default: goto yy15;
        }
        yy46:
        ++i;
        yych = i < end ? *i : 0;
        switch (yych) {
            case '0':
            case '1':
            case '2':
            case '3':
            case '4':
            case '5':
            case '6':
            case '7':
            case '8':
            case '9': goto yy46;
            case 'F':
            case 'L':
            case 'f':
            case 'l': goto yy26;
            default: goto yy9;
        }
        yy47:
        ++i;
        yych = i < end ? *i : 0;
        switch (yych) {
            case '0':
            case '1':
            case '2':
            case '3':
            case '4':
            case '5':
            case '6':
            case '7':
            case '8':
            case '9': goto yy48;
            default: goto yy15;
        }
        yy48:
        ++i;
        yych = i < end ? *i : 0;
        switch (yych) {
            case '0':
            case '1':
            case '2':
            case '3':
            case '4':
            case '5':
            case '6':
            case '7':
            case '8':
            case '9': goto yy48;
            case 'F':
            case 'L':
            case 'f':
            case 'l': goto yy28;
            default: goto yy11;
        }
        yy49:
        ++i;
        yych = i < end ? *i : 0;
        switch (yych) {
            case 'L': goto yy60;
            case 'U':
            case 'u': goto yy61;
            default: goto yy30;
        }
        yy50:
        ++i;
        yych = i < end ? *i : 0;
        switch (yych) {
            case 'L': goto yy62;
            case 'l': goto yy63;
            default: goto yy30;
        }
        yy51:
        ++i;
        yych = i < end ? *i : 0;
        switch (yych) {
            case 'U':
            case 'u': goto yy61;
            case 'l': goto yy60;
            default: goto yy30;
        }
        yy52:
        ++i;
        goto yy33;
        yy53:
        ++i;
        yych = i < end ? *i : 0;
        yy54:
        switch (yych) {
            case '0':
            case '1':
            case '2':
            case '3':
            case '4':
            case '5':
            case '6':
            case '7':
            case '8':
            case '9':
            case 'A':
            case 'B':
            case 'C':
            case 'D':
            case 'E':
            case 'F':
            case 'a':
            case 'b':
            case 'c':
            case 'd':
            case 'e':
            case 'f': goto yy53;
            case 'P':
            case 'p': goto yy64;
            default: goto yy15;
        }
        yy55:
        ++i;
        yych = i < end ? *i : 0;
        switch (yych) {
            case 0x00: goto yy15;
            case 'P':
            case 'p': goto yy65;
            default: goto yy54;
        }
        yy56:
        ++i;
        yych = i < end ? *i : 0;
        switch (yych) {
            case 'L': goto yy66;
            case 'U':
            case 'u': goto yy67;
            default: goto yy40;
        }
        yy57:
        ++i;
        yych = i < end ? *i : 0;
        switch (yych) {
            case '+':
            case '-': goto yy68;
            case '0':
            case '1':
            case '2':
            case '3':
            case '4':
            case '5':
            case '6':
            case '7':
            case '8':
            case '9': goto yy69;
            default: goto yy15;
        }
        yy58:
        ++i;
        yych = i < end ? *i : 0;
        switch (yych) {
            case 'L': goto yy71;
            case 'l': goto yy72;
            default: goto yy40;
        }
        yy59:
        ++i;
        yych = i < end ? *i : 0;
        switch (yych) {
            case 'U':
            case 'u': goto yy67;
            case 'l': goto yy66;
            default: goto yy40;
        }
        yy60:
        ++i;
        yych = i < end ? *i : 0;
        switch (yych) {
            case 'U':
            case 'u': goto yy61;
            default: goto yy30;
        }
        yy61:
        ++i;
        goto yy30;
        yy62:
        ++i;
        yych = i < end ? *i : 0;
        switch (yych) {
            case 'L': goto yy61;
            default: goto yy30;
        }
        yy63:
        ++i;
        yych = i < end ? *i : 0;
        switch (yych) {
            case 'l': goto yy61;
            default: goto yy30;
        }
        yy64:
        ++i;
        yych = i < end ? *i : 0;
        switch (yych) {
            case '+':
            case '-': goto yy73;
            case '0':
            case '1':
            case '2':
            case '3':
            case '4':
            case '5':
            case '6':
            case '7':
            case '8':
            case '9': goto yy74;
            default: goto yy15;
        }
        yy65:
        ++i;
        yych = i < end ? *i : 0;
        switch (yych) {
            case '+':
            case '-': goto yy76;
            case '0':
            case '1':
            case '2':
            case '3':
            case '4':
            case '5':
            case '6':
            case '7':
            case '8':
            case '9': goto yy77;
            default: goto yy15;
        }
        yy66:
        ++i;
        yych = i < end ? *i : 0;
        switch (yych) {
            case 'U':
            case 'u': goto yy67;
            default: goto yy40;
        }
        yy67:
        ++i;
        goto yy40;
        yy68:
        ++i;
        yych = i < end ? *i : 0;
        switch (yych) {
            case '0':
            case '1':
            case '2':
            case '3':
            case '4':
            case '5':
            case '6':
            case '7':
            case '8':
            case '9': goto yy69;
            default: goto yy15;
        }
        yy69:
        ++i;
        yych = i < end ? *i : 0;
        switch (yych) {
            case '0':
            case '1':
            case '2':
            case '3':
            case '4':
            case '5':
            case '6':
            case '7':
            case '8':
            case '9': goto yy69;
            case 'F':
            case 'L':
            case 'f':
            case 'l': goto yy79;
            default: goto yy70;
        }
        yy70:
        { return i; }
        yy71:
        ++i;
        yych = i < end ? *i : 0;
        switch (yych) {
            case 'L': goto yy67;
            default: goto yy40;
        }
        yy72:
        ++i;
        yych = i < end ? *i : 0;
        switch (yych) {
            case 'l': goto yy67;
            default: goto yy40;
        }
        yy73:
        ++i;
        yych = i < end ? *i : 0;
        switch (yych) {
            case '0':
            case '1':
            case '2':
            case '3':
            case '4':
            case '5':
            case '6':
            case '7':
            case '8':
            case '9': goto yy74;
            default: goto yy15;
        }
        yy74:
        ++i;
        yych = i < end ? *i : 0;
        switch (yych) {
            case '0':
            case '1':
            case '2':
            case '3':
            case '4':
            case '5':
            case '6':
            case '7':
            case '8':
            case '9': goto yy74;
            case 'F':
            case 'L':
            case 'f':
            case 'l': goto yy80;
            default: goto yy75;
        }
        yy75:
        { return i; }
        yy76:
        ++i;
        yych = i < end ? *i : 0;
        switch (yych) {
            case '0':
            case '1':
            case '2':
            case '3':
            case '4':
            case '5':
            case '6':
            case '7':
            case '8':
            case '9': goto yy77;
            default: goto yy15;
        }
        yy77:
        ++i;
        yych = i < end ? *i : 0;
        switch (yych) {
            case '0':
            case '1':
            case '2':
            case '3':
            case '4':
            case '5':
            case '6':
            case '7':
            case '8':
            case '9': goto yy77;
            case 'F':
            case 'L':
            case 'f':
            case 'l': goto yy81;
            default: goto yy78;
        }
        yy78:
        { return i; }
        yy79:
        ++i;
        goto yy70;
        yy80:
        ++i;
        goto yy75;
        yy81:
        ++i;
        goto yy78;
        yy82:
        { return start; }
    }

}


//
//	isCStylePunctuation
//

static bool isCStylePunctuation(ImWchar character) {
    static bool punctuation[128] = {
        false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false,
        false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false,
        false,  true, false, false, false,  true,  true, false,  true,  true,  true,  true,  true,  true,  true,  true,
        false, false, false, false, false, false, false, false, false, false,  true,  true,  true,  true,  true,  true,
        false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false,
        false, false, false, false, false, false, false, false, false, false, false,  true, false,  true,  true, false,
        false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false,
        false, false, false, false, false, false, false, false, false, false, false,  true,  true,  true,  true, false,
    };

    return character < 127 ? punctuation[character] : false;
}


//
//	TextEditor::Language::C
//

const TextEditor::Language* TextEditor::Language::C() {
    static bool initialized = false;
    static TextEditor::Language language;

    if (!initialized) {
        language.name = "C";
        language.preprocess = '#';
        language.singleLineComment = "//";
        language.commentStart = "/*";
        language.commentEnd = "*/";
        language.hasSingleQuotedStrings = true;
        language.hasDoubleQuotedStrings = true;
        language.stringEscape = '\\';

        static const char* const keywords[] = {
            "break", "case", "continue", "default", "do", "else", "for", "goto", "if", "return", "sizeof",
            "switch", "while", "_Alignas", "_Alignof", "_Atomic", "_Bool", "_Complex", "_Generic",
            "_Imaginary", "_Noreturn", "_Static_assert", "_Thread_local"
        };

        static const char* const declarations[] = {
            "auto", "char", "const", "double", "enum", "extern", "float", "inline", "int", "long", "register",
            "restrict", "short", "signed", "static", "struct", "typedef", "union", "unsigned", "void", "volatile"
        };

        for (auto& keyword : keywords) { language.keywords.insert(keyword); }
        for (auto& declaration : declarations) { language.declarations.insert(declaration); }

        language.isPunctuation = isCStylePunctuation;
        language.getIdentifier = getCStyleIdentifier;
        language.getNumber = getCStyleNumber;
        initialized = true;
    }

    return &language;
}


//
//	TextEditor::Language::Cpp
//

const TextEditor::Language* TextEditor::Language::Cpp() {
    static bool initialized = false;
    static TextEditor::Language language;

    if (!initialized) {
        language.name = "C++";
        language.preprocess = '#';
        language.singleLineComment = "//";
        language.commentStart = "/*";
        language.commentEnd = "*/";
        language.hasSingleQuotedStrings = true;
        language.hasDoubleQuotedStrings = true;
        language.stringEscape = '\\';

        static const char* const keywords[] = {
            "alignas", "alignof", "and", "and_eq", "asm", "atomic_cancel", "atomic_commit", "atomic_noexcept",
            "bitand", "bitor", "break", "case", "catch", "compl", "const_cast", "continue", "default", "delete",
            "do", "dynamic_cast", "else", "explicit", "export", "extern", "false", "for", "goto", "if", "import",
            "new", "noexcept", "not", "not_eq", "nullptr", "operator", "or", "or_eq", "reinterpret_cast", "requires",
            "return", "sizeof", "static_assert", "static_cast", "switch", "synchronized", "this", "thread_local",
            "throw", "true", "try", "while", "xor", "xor_eq"
        };

        static const char* const declarations[] = {
            "auto", "bool", "char", "char16_t", "char32_t", "class", "concept", "const", "constexpr", "decltype",
            "double", "explicit", "export", "extern", "enum", "extern", "float", "friend", "inline", "int", "long",
            "module", "mutable", "namespace", "private", "protected", "public", "register", "restrict", "short",
            "signed", "static", "struct", "template", "typedef", "typeid", "typename", "union", "using", "unsigned",
            "virtual", "void", "volatile", "wchar_t"
        };

        for (auto& keyword : keywords) { language.keywords.insert(keyword); }
        for (auto& declaration : declarations) { language.declarations.insert(declaration); }

        language.isPunctuation = isCStylePunctuation;
        language.getIdentifier = getCStyleIdentifier;
        language.getNumber = getCStyleNumber;
        initialized = true;
    }

    return &language;
}


//
//	getCsStyleNumber
//

static TextEditor::Iterator getCsStyleNumber(TextEditor::Iterator start, TextEditor::Iterator end) {
    TextEditor::Iterator i = start;
    TextEditor::Iterator marker;


    {
        ImWchar yych;
        unsigned int yyaccept = 0;
        yych = i < end ? *i : 0;
        switch (yych) {
            case '.': goto yy3;
            case '0': goto yy4;
            case '1':
            case '2':
            case '3':
            case '4':
            case '5':
            case '6':
            case '7':
            case '8':
            case '9': goto yy6;
            default:
                if (i >= end) goto yy49;
                goto yy1;
        }
        yy1:
        ++i;
        yy2:
        { return start; }
        yy3:
        ++i;
        yych = i < end ? *i : 0;
        switch (yych) {
            case '0':
            case '1':
            case '2':
            case '3':
            case '4':
            case '5':
            case '6':
            case '7':
            case '8':
            case '9': goto yy8;
            default: goto yy2;
        }
        yy4:
        yyaccept = 0;
        ++i;
        marker = i;
        yych = i < end ? *i : 0;
        switch (yych) {
            case 0x00: goto yy5;
            case 'B':
            case 'b': goto yy12;
            case 'X':
            case 'x': goto yy15;
            default: goto yy7;
        }
        yy5:
        { return i; }
        yy6:
        yyaccept = 0;
        ++i;
        marker = i;
        yych = i < end ? *i : 0;
        yy7:
        switch (yych) {
            case '.': goto yy10;
            case '0':
            case '1':
            case '2':
            case '3':
            case '4':
            case '5':
            case '6':
            case '7':
            case '8':
            case '9':
            case '_': goto yy6;
            case 'L': goto yy13;
            case 'U':
            case 'u': goto yy14;
            case 'l': goto yy16;
            default: goto yy5;
        }
        yy8:
        yyaccept = 1;
        ++i;
        marker = i;
        yych = i < end ? *i : 0;
        switch (yych) {
            case '0':
            case '1':
            case '2':
            case '3':
            case '4':
            case '5':
            case '6':
            case '7':
            case '8':
            case '9':
            case '_': goto yy8;
            case 'E':
            case 'e': goto yy17;
            case 'F':
            case 'L':
            case 'f':
            case 'l': goto yy18;
            default: goto yy9;
        }
        yy9:
        { return i; }
        yy10:
        ++i;
        yych = i < end ? *i : 0;
        switch (yych) {
            case '0':
            case '1':
            case '2':
            case '3':
            case '4':
            case '5':
            case '6':
            case '7':
            case '8':
            case '9': goto yy19;
            default: goto yy11;
        }
        yy11:
        i = marker;
        switch (yyaccept) {
            case 0: goto yy5;
            case 1: goto yy9;
            default: goto yy20;
        }
        yy12:
        ++i;
        yych = i < end ? *i : 0;
        switch (yych) {
            case '0':
            case '1': goto yy21;
            default: goto yy11;
        }
        yy13:
        ++i;
        yych = i < end ? *i : 0;
        switch (yych) {
            case 'L': goto yy23;
            case 'U':
            case 'u': goto yy24;
            default: goto yy5;
        }
        yy14:
        ++i;
        yych = i < end ? *i : 0;
        switch (yych) {
            case 'L': goto yy25;
            case 'l': goto yy26;
            default: goto yy5;
        }
        yy15:
        ++i;
        yych = i < end ? *i : 0;
        switch (yych) {
            case '0':
            case '1':
            case '2':
            case '3':
            case '4':
            case '5':
            case '6':
            case '7':
            case '8':
            case '9':
            case 'A':
            case 'B':
            case 'C':
            case 'D':
            case 'E':
            case 'F':
            case 'a':
            case 'b':
            case 'c':
            case 'd':
            case 'e':
            case 'f': goto yy27;
            default: goto yy11;
        }
        yy16:
        ++i;
        yych = i < end ? *i : 0;
        switch (yych) {
            case 'U':
            case 'u': goto yy24;
            case 'l': goto yy23;
            default: goto yy5;
        }
        yy17:
        ++i;
        yych = i < end ? *i : 0;
        switch (yych) {
            case '+':
            case '-': goto yy29;
            case '0':
            case '1':
            case '2':
            case '3':
            case '4':
            case '5':
            case '6':
            case '7':
            case '8':
            case '9':
            case '_': goto yy30;
            default: goto yy11;
        }
        yy18:
        ++i;
        goto yy9;
        yy19:
        yyaccept = 2;
        ++i;
        marker = i;
        yych = i < end ? *i : 0;
        switch (yych) {
            case '0':
            case '1':
            case '2':
            case '3':
            case '4':
            case '5':
            case '6':
            case '7':
            case '8':
            case '9':
            case '_': goto yy19;
            case 'E':
            case 'e': goto yy31;
            case 'F':
            case 'L':
            case 'f':
            case 'l': goto yy32;
            default: goto yy20;
        }
        yy20:
        { return i; }
        yy21:
        ++i;
        yych = i < end ? *i : 0;
        switch (yych) {
            case '0':
            case '1':
            case '_': goto yy21;
            case 'L': goto yy33;
            case 'U':
            case 'u': goto yy34;
            case 'l': goto yy35;
            default: goto yy22;
        }
        yy22:
        { return i; }
        yy23:
        ++i;
        yych = i < end ? *i : 0;
        switch (yych) {
            case 'U':
            case 'u': goto yy24;
            default: goto yy5;
        }
        yy24:
        ++i;
        goto yy5;
        yy25:
        ++i;
        yych = i < end ? *i : 0;
        switch (yych) {
            case 'L': goto yy24;
            default: goto yy5;
        }
        yy26:
        ++i;
        yych = i < end ? *i : 0;
        switch (yych) {
            case 'l': goto yy24;
            default: goto yy5;
        }
        yy27:
        ++i;
        yych = i < end ? *i : 0;
        switch (yych) {
            case '0':
            case '1':
            case '2':
            case '3':
            case '4':
            case '5':
            case '6':
            case '7':
            case '8':
            case '9':
            case 'A':
            case 'B':
            case 'C':
            case 'D':
            case 'E':
            case 'F':
            case '_':
            case 'a':
            case 'b':
            case 'c':
            case 'd':
            case 'e':
            case 'f': goto yy27;
            case 'L': goto yy36;
            case 'U':
            case 'u': goto yy37;
            case 'l': goto yy38;
            default: goto yy28;
        }
        yy28:
        { return i; }
        yy29:
        ++i;
        yych = i < end ? *i : 0;
        switch (yych) {
            case '0':
            case '1':
            case '2':
            case '3':
            case '4':
            case '5':
            case '6':
            case '7':
            case '8':
            case '9':
            case '_': goto yy30;
            default: goto yy11;
        }
        yy30:
        ++i;
        yych = i < end ? *i : 0;
        switch (yych) {
            case '0':
            case '1':
            case '2':
            case '3':
            case '4':
            case '5':
            case '6':
            case '7':
            case '8':
            case '9':
            case '_': goto yy30;
            case 'F':
            case 'L':
            case 'f':
            case 'l': goto yy18;
            default: goto yy9;
        }
        yy31:
        ++i;
        yych = i < end ? *i : 0;
        switch (yych) {
            case '+':
            case '-': goto yy39;
            case '0':
            case '1':
            case '2':
            case '3':
            case '4':
            case '5':
            case '6':
            case '7':
            case '8':
            case '9':
            case '_': goto yy40;
            default: goto yy11;
        }
        yy32:
        ++i;
        goto yy20;
        yy33:
        ++i;
        yych = i < end ? *i : 0;
        switch (yych) {
            case 'L': goto yy41;
            case 'U':
            case 'u': goto yy42;
            default: goto yy22;
        }
        yy34:
        ++i;
        yych = i < end ? *i : 0;
        switch (yych) {
            case 'L': goto yy43;
            case 'l': goto yy44;
            default: goto yy22;
        }
        yy35:
        ++i;
        yych = i < end ? *i : 0;
        switch (yych) {
            case 'U':
            case 'u': goto yy42;
            case 'l': goto yy41;
            default: goto yy22;
        }
        yy36:
        ++i;
        yych = i < end ? *i : 0;
        switch (yych) {
            case 'L': goto yy45;
            case 'U':
            case 'u': goto yy46;
            default: goto yy28;
        }
        yy37:
        ++i;
        yych = i < end ? *i : 0;
        switch (yych) {
            case 'L': goto yy47;
            case 'l': goto yy48;
            default: goto yy28;
        }
        yy38:
        ++i;
        yych = i < end ? *i : 0;
        switch (yych) {
            case 'U':
            case 'u': goto yy46;
            case 'l': goto yy45;
            default: goto yy28;
        }
        yy39:
        ++i;
        yych = i < end ? *i : 0;
        switch (yych) {
            case '0':
            case '1':
            case '2':
            case '3':
            case '4':
            case '5':
            case '6':
            case '7':
            case '8':
            case '9':
            case '_': goto yy40;
            default: goto yy11;
        }
        yy40:
        ++i;
        yych = i < end ? *i : 0;
        switch (yych) {
            case '0':
            case '1':
            case '2':
            case '3':
            case '4':
            case '5':
            case '6':
            case '7':
            case '8':
            case '9':
            case '_': goto yy40;
            case 'F':
            case 'L':
            case 'f':
            case 'l': goto yy32;
            default: goto yy20;
        }
        yy41:
        ++i;
        yych = i < end ? *i : 0;
        switch (yych) {
            case 'U':
            case 'u': goto yy42;
            default: goto yy22;
        }
        yy42:
        ++i;
        goto yy22;
        yy43:
        ++i;
        yych = i < end ? *i : 0;
        switch (yych) {
            case 'L': goto yy42;
            default: goto yy22;
        }
        yy44:
        ++i;
        yych = i < end ? *i : 0;
        switch (yych) {
            case 'l': goto yy42;
            default: goto yy22;
        }
        yy45:
        ++i;
        yych = i < end ? *i : 0;
        switch (yych) {
            case 'U':
            case 'u': goto yy46;
            default: goto yy28;
        }
        yy46:
        ++i;
        goto yy28;
        yy47:
        ++i;
        yych = i < end ? *i : 0;
        switch (yych) {
            case 'L': goto yy46;
            default: goto yy28;
        }
        yy48:
        ++i;
        yych = i < end ? *i : 0;
        switch (yych) {
            case 'l': goto yy46;
            default: goto yy28;
        }
        yy49:
        { return start; }
    }

}


//
//	TextEditor::Language::Cs
//

const TextEditor::Language* TextEditor::Language::Cs() {
    static bool initialized = false;
    static TextEditor::Language language;

    if (!initialized) {
        language.name = "C#";
        language.preprocess = '#';
        language.singleLineComment = "//";
        language.commentStart = "/*";
        language.commentEnd = "*/";
        language.hasSingleQuotedStrings = true;
        language.hasDoubleQuotedStrings = true;
        language.stringEscape = '\\';

        static const char* const keywords[] = {
            "abstract", "as", "base", "bool", "break", "byte", "case", "catch", "char", "checked", "class", "const", "continue",
            "decimal", "default", "delegate", "do", "double", "else", "enum", "event", "explicit", "extern", "false", "finally",
            "fixed", "float", "for", "foreach", "goto", "if", "implicit", "in", "in (generic modifier)", "int", "interface",
            "internal", "is", "lock", "long", "namespace", "new", "null", "object", "operator", "out", "override", "params",
            "private", "protected", "public", "readonly", "ref", "return", "sbyte", "sealed", "short", "sizeof", "stackalloc",
            "static", "string", "struct", "switch", "this", "throw", "true", "try", "typeof", "uint", "ulong", "unchecked",
            "unsafe", "ushort", "using", "using static", "void", "volatile", "while"
        };

        for (auto& keyword : keywords) { language.keywords.insert(keyword); }

        language.isPunctuation = isCStylePunctuation;
        language.getIdentifier = getCStyleIdentifier;
        language.getNumber = getCsStyleNumber;
        initialized = true;
    }

    return &language;
}


//
//	TextEditor::Language::AngelScript
//

const TextEditor::Language* TextEditor::Language::AngelScript() {
    static bool initialized = false;
    static TextEditor::Language language;

    if (!initialized) {
        language.name = "AngelScript";
        language.preprocess = '#';
        language.singleLineComment = "//";
        language.commentStart = "/*";
        language.commentEnd = "*/";
        language.hasSingleQuotedStrings = true;
        language.hasDoubleQuotedStrings = true;
        language.stringEscape = '\\';

        static const char* const keywords[] = {
            "and", "abstract", "auto", "bool", "break", "case", "cast", "class", "const", "continue", "default",
            "do", "double", "else", "enum", "false", "final", "float", "for", "from", "funcdef", "function", "get",
            "if", "import", "in", "inout", "int", "interface", "int8", "int16", "int32", "int64", "is", "mixin",
            "namespace", "not", "null", "or", "out", "override", "private", "protected", "return", "set", "shared",
            "super", "switch", "this ", "true", "typedef", "uint", "uint8", "uint16", "uint32", "uint64", "void",
            "while", "xor"
        };

        for (auto& keyword : keywords) { language.keywords.insert(keyword); }

        language.isPunctuation = isCStylePunctuation;
        language.getIdentifier = getCStyleIdentifier;
        language.getNumber = getCStyleNumber;
        initialized = true;
    }

    return &language;
}


//
//	getLuaStyleNumber
//

static TextEditor::Iterator getLuaStyleNumber(TextEditor::Iterator start, TextEditor::Iterator end) {
    TextEditor::Iterator i = start;
    TextEditor::Iterator marker;


    {
        ImWchar yych;
        yych = i < end ? *i : 0;
        switch (yych) {
            case '.': goto yy3;
            case '0': goto yy5;
            case '1':
            case '2':
            case '3':
            case '4':
            case '5':
            case '6':
            case '7':
            case '8':
            case '9': goto yy6;
            case 'E':
            case 'e': goto yy8;
            default:
                if (i >= end) goto yy1;
                goto yy2;
        }
        yy1:
        { return i; }
        yy2:
        ++i;
        { return start; }
        yy3:
        ++i;
        yych = i < end ? *i : 0;
        switch (yych) {
            case '0':
            case '1':
            case '2':
            case '3':
            case '4':
            case '5':
            case '6':
            case '7':
            case '8':
            case '9': goto yy3;
            case 'E':
            case 'e': goto yy8;
            default: goto yy4;
        }
        yy4:
        { return i; }
        yy5:
        ++i;
        marker = i;
        yych = i < end ? *i : 0;
        switch (yych) {
            case 0x00: goto yy1;
            case 'X':
            case 'x': goto yy9;
            default: goto yy7;
        }
        yy6:
        ++i;
        yych = i < end ? *i : 0;
        yy7:
        switch (yych) {
            case '.': goto yy3;
            case '0':
            case '1':
            case '2':
            case '3':
            case '4':
            case '5':
            case '6':
            case '7':
            case '8':
            case '9': goto yy6;
            case 'E':
            case 'e': goto yy8;
            default: goto yy1;
        }
        yy8:
        ++i;
        yych = i < end ? *i : 0;
        switch (yych) {
            case 0x00: goto yy4;
            case '+':
            case '-': goto yy11;
            default: goto yy12;
        }
        yy9:
        ++i;
        yych = i < end ? *i : 0;
        switch (yych) {
            case '0':
            case '1':
            case '2':
            case '3':
            case '4':
            case '5':
            case '6':
            case '7':
            case '8':
            case '9':
            case 'A':
            case 'B':
            case 'C':
            case 'D':
            case 'E':
            case 'F':
            case 'a':
            case 'b':
            case 'c':
            case 'd':
            case 'e':
            case 'f': goto yy13;
            default: goto yy10;
        }
        yy10:
        i = marker;
        goto yy1;
        yy11:
        ++i;
        yych = i < end ? *i : 0;
        yy12:
        switch (yych) {
            case '0':
            case '1':
            case '2':
            case '3':
            case '4':
            case '5':
            case '6':
            case '7':
            case '8':
            case '9': goto yy11;
            default: goto yy4;
        }
        yy13:
        ++i;
        yych = i < end ? *i : 0;
        switch (yych) {
            case '.': goto yy15;
            case '0':
            case '1':
            case '2':
            case '3':
            case '4':
            case '5':
            case '6':
            case '7':
            case '8':
            case '9':
            case 'A':
            case 'B':
            case 'C':
            case 'D':
            case 'E':
            case 'F':
            case 'a':
            case 'b':
            case 'c':
            case 'd':
            case 'e':
            case 'f': goto yy13;
            case 'P':
            case 'p': goto yy16;
            default: goto yy14;
        }
        yy14:
        { return i; }
        yy15:
        ++i;
        yych = i < end ? *i : 0;
        switch (yych) {
            case '0':
            case '1':
            case '2':
            case '3':
            case '4':
            case '5':
            case '6':
            case '7':
            case '8':
            case '9':
            case 'A':
            case 'B':
            case 'C':
            case 'D':
            case 'E':
            case 'F':
            case 'a':
            case 'b':
            case 'c':
            case 'd':
            case 'e':
            case 'f': goto yy15;
            case 'P':
            case 'p': goto yy16;
            default: goto yy14;
        }
        yy16:
        ++i;
        yych = i < end ? *i : 0;
        switch (yych) {
            case 0x00: goto yy14;
            case '+':
            case '-': goto yy17;
            default: goto yy18;
        }
        yy17:
        ++i;
        yych = i < end ? *i : 0;
        yy18:
        switch (yych) {
            case '0':
            case '1':
            case '2':
            case '3':
            case '4':
            case '5':
            case '6':
            case '7':
            case '8':
            case '9':
            case 'A':
            case 'B':
            case 'C':
            case 'D':
            case 'E':
            case 'F':
            case 'a':
            case 'b':
            case 'c':
            case 'd':
            case 'e':
            case 'f': goto yy17;
            default: goto yy14;
        }
    }

}


//
//	isLuaStylePunctuation
//	[]{}!%#^&*()-+=~|<>?:/;,.
//

static bool isLuaStylePunctuation(ImWchar character) {
    static bool punctuation[128] = {
        false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false,
        false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false,
        false,  true, false,  true, false,  true,  true, false,  true,  true,  true,  true,  true,  true,  true,  true,
        false, false, false, false, false, false, false, false, false, false,  true,  true,  true,  true,  true,  true,
        false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false,
        false, false, false, false, false, false, false, false, false, false, false,  true, false,  true,  true, false,
        false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false,
        false, false, false, false, false, false, false, false, false, false, false,  true,  true,  true,  true, false,
    };

    return character < 127 ? punctuation[character] : false;
}

//
// TextEditor::Language::WattleScript
//
const TextEditor::Language* TextEditor::Language::WattleScript() 
{
    static bool initialized = false;
    static TextEditor::Language language;

    if (!initialized) {
        language.name = "WattleScript";
        language.preprocess = '#';
        language.singleLineComment = "//";
        language.commentStart = "/*";
        language.commentEnd = "*/";
        language.hasSingleQuotedStrings = true;
        language.hasDoubleQuotedStrings = true;
        language.stringEscape = '\\';

        static const char* const keywords[] = {
            "and", "break", "do", "else", "elseif", "end", "false", "for", "function", "if", 
			"in", "local", "nil", "not", "or", "repeat", "return", "then", "true", "until", 
			"while", "let", "var", "of", "continue", "null", "switch", "case", "class", "enum",
			"new", "mixin", "static", "private", "public", "sealed"
        };

        for (auto& keyword : keywords) { language.keywords.insert(keyword); }

        language.isPunctuation = isLuaStylePunctuation;
        language.getIdentifier = getCStyleIdentifier;
        language.getNumber = getLuaStyleNumber;
        initialized = true;
    }

    return &language;
}


//
//	TextEditor::Language::Lua
//

const TextEditor::Language* TextEditor::Language::Lua() {
    static bool initialized = false;
    static TextEditor::Language language;

    if (!initialized) {
        language.name = "Lua";
        language.singleLineComment = "--";
        language.commentStart = "--[[";
        language.commentEnd = "]]";
        language.hasSingleQuotedStrings = true;
        language.hasDoubleQuotedStrings = true;
        language.otherStringStart = "[[";
        language.otherStringEnd = "]]";
        language.stringEscape = '\\';

        static const char* const keywords[] = {
            "and", "break", "do", "else", "elseif", "end", "false", "for", "function", "goto", "if", "in", "local", "nil",
            "not", "or", "repeat", "return", "then", "true", "until", "while"
        };

        for (auto& keyword : keywords) { language.keywords.insert(keyword); }

        language.isPunctuation = isLuaStylePunctuation;
        language.getIdentifier = getCStyleIdentifier;
        language.getNumber = getLuaStyleNumber;
        initialized = true;
    }

    return &language;
}


//
//	getPythonStyleNumber
//

static TextEditor::Iterator getPythonStyleNumber(TextEditor::Iterator start, TextEditor::Iterator end) {
    TextEditor::Iterator i = start;
    TextEditor::Iterator marker;


    {
        ImWchar yych;
        yych = i < end ? *i : 0;
        switch (yych) {
            case '0': goto yy2;
            case '1':
            case '2':
            case '3':
            case '4':
            case '5':
            case '6':
            case '7':
            case '8':
            case '9': goto yy4;
            default:
                if (i >= end) goto yy18;
                goto yy1;
        }
        yy1:
        ++i;
        { return start; }
        yy2:
        ++i;
        marker = i;
        yych = i < end ? *i : 0;
        switch (yych) {
            case 0x00: goto yy3;
            case 'B':
            case 'b': goto yy9;
            case 'O':
            case 'o': goto yy11;
            case 'X':
            case 'x': goto yy12;
            default: goto yy5;
        }
        yy3:
        {
            return i;
        }
        yy4:
        ++i;
        marker = i;
        yych = i < end ? *i : 0;
        yy5:
        switch (yych) {
            case '+':
            case '-':
            case 'E':
            case 'e': goto yy6;
            case '.': goto yy8;
            case '0':
            case '1':
            case '2':
            case '3':
            case '4':
            case '5':
            case '6':
            case '7':
            case '8':
            case '9':
            case '_': goto yy4;
            case 'J':
            case 'j': goto yy10;
            default: goto yy3;
        }
        yy6:
        ++i;
        yych = i < end ? *i : 0;
        switch (yych) {
            case '0':
            case '1':
            case '2':
            case '3':
            case '4':
            case '5':
            case '6':
            case '7':
            case '8':
            case '9': goto yy13;
            default: goto yy7;
        }
        yy7:
        i = marker;
        goto yy3;
        yy8:
        ++i;
        yych = i < end ? *i : 0;
        switch (yych) {
            case '0':
            case '1':
            case '2':
            case '3':
            case '4':
            case '5':
            case '6':
            case '7':
            case '8':
            case '9': goto yy14;
            default: goto yy7;
        }
        yy9:
        ++i;
        yych = i < end ? *i : 0;
        switch (yych) {
            case '0':
            case '1':
            case '_': goto yy15;
            default: goto yy7;
        }
        yy10:
        ++i;
        goto yy3;
        yy11:
        ++i;
        yych = i < end ? *i : 0;
        switch (yych) {
            case '0':
            case '1':
            case '2':
            case '3':
            case '4':
            case '5':
            case '6':
            case '7':
            case '_': goto yy16;
            default: goto yy7;
        }
        yy12:
        ++i;
        yych = i < end ? *i : 0;
        switch (yych) {
            case '0':
            case '1':
            case '2':
            case '3':
            case '4':
            case '5':
            case '6':
            case '7':
            case '8':
            case '9':
            case 'A':
            case 'B':
            case 'C':
            case 'D':
            case 'E':
            case 'F':
            case '_':
            case 'a':
            case 'b':
            case 'c':
            case 'd':
            case 'e':
            case 'f': goto yy17;
            default: goto yy7;
        }
        yy13:
        ++i;
        yych = i < end ? *i : 0;
        switch (yych) {
            case '0':
            case '1':
            case '2':
            case '3':
            case '4':
            case '5':
            case '6':
            case '7':
            case '8':
            case '9':
            case '_': goto yy13;
            case 'J':
            case 'j': goto yy10;
            default: goto yy3;
        }
        yy14:
        ++i;
        marker = i;
        yych = i < end ? *i : 0;
        switch (yych) {
            case '+':
            case '-':
            case 'E':
            case 'e': goto yy6;
            case '0':
            case '1':
            case '2':
            case '3':
            case '4':
            case '5':
            case '6':
            case '7':
            case '8':
            case '9':
            case '_': goto yy14;
            case 'J':
            case 'j': goto yy10;
            default: goto yy3;
        }
        yy15:
        ++i;
        yych = i < end ? *i : 0;
        switch (yych) {
            case '0':
            case '1':
            case '_': goto yy15;
            default: goto yy3;
        }
        yy16:
        ++i;
        yych = i < end ? *i : 0;
        switch (yych) {
            case '0':
            case '1':
            case '2':
            case '3':
            case '4':
            case '5':
            case '6':
            case '7':
            case '_': goto yy16;
            default: goto yy3;
        }
        yy17:
        ++i;
        yych = i < end ? *i : 0;
        switch (yych) {
            case '0':
            case '1':
            case '2':
            case '3':
            case '4':
            case '5':
            case '6':
            case '7':
            case '8':
            case '9':
            case 'A':
            case 'B':
            case 'C':
            case 'D':
            case 'E':
            case 'F':
            case '_':
            case 'a':
            case 'b':
            case 'c':
            case 'd':
            case 'e':
            case 'f': goto yy17;
            default: goto yy3;
        }
        yy18:
        { return start; }
    }

}


//
//	TextEditor::Language::Python
//

const TextEditor::Language* TextEditor::Language::Python() {
    static bool initialized = false;
    static TextEditor::Language language;

    if (!initialized) {
        language.name = "Python";
        language.singleLineComment = "#";
        language.hasSingleQuotedStrings = true;
        language.hasDoubleQuotedStrings = true;
        language.otherStringStart = "\"\"\"";
        language.otherStringEnd = "\"\"\"";
        language.otherStringAltStart = "'''";
        language.otherStringAltEnd = "'''";
        language.stringEscape = '\\';

        static const char* const keywords[] = {
            "False", "await", "else", "import", "pass", "None", "break", "except", "in", "raise", "True",
            "class", "finally", "is", "return", "and", "continue", "for", "lambda", "try", "as", "def",
            "from", "nonlocal", "while", "assert", "del", "global", "not", "with", "async", "elif",
            "if", "or", "yield"
        };

        for (auto& keyword : keywords) { language.keywords.insert(keyword); }

        language.isPunctuation = isCStylePunctuation;
        language.getIdentifier = getCStyleIdentifier;
        language.getNumber = getPythonStyleNumber;
        initialized = true;
    }

    return &language;
}


//
//	TextEditor::Language::Glsl
//

const TextEditor::Language* TextEditor::Language::Glsl() {
    static bool initialized = false;
    static TextEditor::Language language;

    if (!initialized) {
        language.name = "GLSL";
        language.preprocess = '#';
        language.singleLineComment = "//";
        language.commentStart = "/*";
        language.commentEnd = "*/";
        language.hasSingleQuotedStrings = true;
        language.hasDoubleQuotedStrings = true;
        language.stringEscape = '\\';

        // source: https://registry.khronos.org/OpenGL/specs/gl/GLSLangSpec.4.60.html
        static const char* const keywords[] = {
            "atomic_uint", "attribute", "bool", "break", "buffer", "bvec2", "bvec3", "bvec4", "case", "centroid",
            "coherent", "const", "continue", "default", "discard", "dmat2", "dmat2x2", "dmat2x3", "dmat2x4", "dmat3",
            "dmat3x2", "dmat3x3", "dmat3x4", "dmat4", "dmat4x2", "dmat4x3", "dmat4x4", "do", "double", "dvec2", "dvec3",
            "dvec4", "else", "false", "flat", "float", "for", "highp", "if", "iimage1D", "iimage1DArray", "iimage2D",
            "iimage2DArray", "iimage2DMS", "iimage2DMSArray", "iimage2DRect", "iimage3D", "iimageBuffer", "iimageCube",
            "iimageCubeArray", "image1D", "image1DArray", "image2D", "image2DArray", "image2DMS", "image2DMSArray",
            "image2DRect", "image3D", "imageBuffer", "imageCube", "imageCubeArray", "in", "inout", "int", "invariant",
            "isampler1D", "isampler1DArray", "isampler2D", "isampler2DArray", "isampler2DMS", "isampler2DMSArray",
            "isampler2DRect", "isampler3D", "isamplerBuffer", "isamplerCube", "isamplerCubeArray", "ivec2", "ivec3",
            "ivec4", "layout", "lowp", "mat2", "mat2x2", "mat2x3", "mat2x4", "mat3", "mat3x2", "mat3x3", "mat3x4",
            "mat4", "mat4x2", "mat4x3", "mat4x4", "mediump", "noperspective", "out", "patch", "precise", "precision",
            "readonly", "restrict", "return", "sample", "sampler1D", "sampler1DArray", "sampler1DArrayShadow",
            "sampler1DShadow", "sampler2D", "sampler2DArray", "sampler2DArrayShadow", "sampler2DMS", "sampler2DMSArray",
            "sampler2DRect", "sampler2DRectShadow", "sampler2DShadow", "sampler3D", "samplerBuffer", "samplerCube",
            "samplerCubeArray", "samplerCubeArrayShadow", "samplerCubeShadow", "shared", "smooth", "struct", "subroutine",
            "switch", "true", "uimage1D", "uimage1DArray", "uimage2D", "uimage2DArray", "uimage2DMS", "uimage2DMSArray",
            "uimage2DRect", "uimage3D", "uimageBuffer", "uimageCube", "uimageCubeArray", "uint", "uniform", "usampler1D",
            "usampler1DArray", "usampler2D", "usampler2DArray", "usampler2DMS", "usampler2DMSArray", "usampler2DRect",
            "usampler3D", "usamplerBuffer", "usamplerCube", "usamplerCubeArray", "uvec2", "uvec3", "uvec4", "varying",
            "vec2", "vec3", "vec4", "void", "volatile", "while", "writeonly"
        };

        for (auto& keyword : keywords) { language.keywords.insert(keyword); }

        language.isPunctuation = isCStylePunctuation;
        language.getIdentifier = getCStyleIdentifier;
        language.getNumber = getCStyleNumber;
        initialized = true;
    }

    return &language;
}


//
//	TextEditor::Language::Hlsl
//

const TextEditor::Language* TextEditor::Language::Hlsl() {
    static bool initialized = false;
    static TextEditor::Language language;

    if (!initialized) {
        language.name = "HLSL";
        language.preprocess = '#';
        language.singleLineComment = "//";
        language.commentStart = "/*";
        language.commentEnd = "*/";
        language.hasSingleQuotedStrings = true;
        language.hasDoubleQuotedStrings = true;
        language.stringEscape = '\\';

        static const char* const keywords[] = {
            "AppendStructuredBuffer", "asm", "asm_fragment", "BlendState", "bool", "break", "Buffer",
            "ByteAddressBuffer", "case", "cbuffer", "centroid", "class", "column_major", "compile",
            "compile_fragment", "CompileShader", "const", "continue", "ComputeShader", "ConsumeStructuredBuffer",
            "default", "DepthStencilState", "DepthStencilView", "discard", "do", "double", "DomainShader", "dword",
            "else", "export", "extern", "false", "float", "for", "fxgroup", "GeometryShader", "groupshared", "half",
            "Hullshader", "if", "in", "inline", "inout", "InputPatch", "int", "interface", "line", "lineadj",
            "linear", "LineStream", "matrix", "min16float", "min10float", "min16int", "min12int", "min16uint",
            "namespace", "nointerpolation", "noperspective", "NULL", "out", "OutputPatch", "packoffset",
            "pass", "pixelfragment", "PixelShader", "point", "PointStream", "precise", "RasterizerState",
            "RenderTargetView", "return", "register", "row_major", "RWBuffer", "RWByteAddressBuffer",
            "RWStructuredBuffer", "RWTexture1D", "RWTexture1DArray", "RWTexture2D", "RWTexture2DArray",
            "RWTexture3D", "sample", "sampler", "SamplerState", "SamplerComparisonState", "shared",
            "snorm", "stateblock", "stateblock_state", "static", "string", "struct", "switch", "StructuredBuffer",
            "tbuffer", "technique", "technique10", "technique11", "texture", "Texture1D", "Texture1DArray",
            "Texture2D", "Texture2DArray", "Texture2DMS", "Texture2DMSArray", "Texture3D", "TextureCube",
            "TextureCubeArray", "true", "typedef", "triangle", "triangleadj", "TriangleStream", "uint",
            "uniform", "unorm", "unsigned", "vector", "vertexfragment", "VertexShader", "void", "volatile", "while",
            "bool1", "bool2", "bool3", "bool4", "double1", "double2", "double3", "double4", "float1", "float2",
            "float3", "float4", "int1", "int2", "int3", "int4", "in", "out", "inout", "uint1", "uint2", "uint3",
            "uint4", "dword1", "dword2", "dword3", "dword4", "half1", "half2", "half3", "half4", "float1x1",
            "float2x1", "float3x1", "float4x1", "float1x2", "float2x2", "float3x2", "float4x2",
            "float1x3", "float2x3", "float3x3", "float4x3", "float1x4", "float2x4", "float3x4", "float4x4",
            "half1x1", "half2x1", "half3x1", "half4x1", "half1x2", "half2x2", "half3x2", "half4x2",
            "half1x3", "half2x3", "half3x3", "half4x3", "half1x4", "half2x4", "half3x4", "half4x4",
        };

        for (auto& keyword : keywords) { language.keywords.insert(keyword); }

        language.isPunctuation = isCStylePunctuation;
        language.getIdentifier = getCStyleIdentifier;
        language.getNumber = getCStyleNumber;
        initialized = true;
    }

    return &language;
}


//
//	tokenizeJson
//

static TextEditor::Iterator tokenizeJson(TextEditor::Iterator start, TextEditor::Iterator end, TextEditor::Color& color) {
    TextEditor::Iterator i = start;
    TextEditor::Iterator marker;


    {
        ImWchar yych;
        unsigned int yyaccept = 0;
        yych = i < end ? *i : 0;
        switch (yych) {
            case ',':
            case ':':
            case '[':
            case ']':
            case '{':
            case '}': goto yy3;
            case '-': goto yy4;
            case '0': goto yy5;
            case '1':
            case '2':
            case '3':
            case '4':
            case '5':
            case '6':
            case '7':
            case '8':
            case '9': goto yy7;
            case 'f': goto yy8;
            case 'n': goto yy9;
            case 't': goto yy10;
            default:
                if (i >= end) goto yy24;
                goto yy1;
        }
        yy1:
        ++i;
        yy2:
        { return start; }
        yy3:
        ++i;
        {
            color = TextEditor::Color::punctuation;
            return i;
        }
        yy4:
        ++i;
        yych = i < end ? *i : 0;
        switch (yych) {
            case '0': goto yy5;
            case '1':
            case '2':
            case '3':
            case '4':
            case '5':
            case '6':
            case '7':
            case '8':
            case '9': goto yy7;
            default: goto yy2;
        }
        yy5:
        yyaccept = 0;
        ++i;
        marker = i;
        yych = i < end ? *i : 0;
        switch (yych) {
            case '.': goto yy11;
            case 'E':
            case 'e': goto yy13;
            default: goto yy6;
        }
        yy6:
        {
            color = TextEditor::Color::number;
            return i;
        }
        yy7:
        yyaccept = 0;
        ++i;
        marker = i;
        yych = i < end ? *i : 0;
        switch (yych) {
            case '.': goto yy11;
            case '0':
            case '1':
            case '2':
            case '3':
            case '4':
            case '5':
            case '6':
            case '7':
            case '8':
            case '9': goto yy7;
            case 'E':
            case 'e': goto yy13;
            default: goto yy6;
        }
        yy8:
        yyaccept = 1;
        ++i;
        marker = i;
        yych = i < end ? *i : 0;
        switch (yych) {
            case 'a': goto yy14;
            default: goto yy2;
        }
        yy9:
        yyaccept = 1;
        ++i;
        marker = i;
        yych = i < end ? *i : 0;
        switch (yych) {
            case 'u': goto yy15;
            default: goto yy2;
        }
        yy10:
        yyaccept = 1;
        ++i;
        marker = i;
        yych = i < end ? *i : 0;
        switch (yych) {
            case 'r': goto yy16;
            default: goto yy2;
        }
        yy11:
        ++i;
        yych = i < end ? *i : 0;
        switch (yych) {
            case '0':
            case '1':
            case '2':
            case '3':
            case '4':
            case '5':
            case '6':
            case '7':
            case '8':
            case '9': goto yy17;
            default: goto yy12;
        }
        yy12:
        i = marker;
        if (yyaccept == 0) goto yy6;
        else goto yy2;
        yy13:
        ++i;
        yych = i < end ? *i : 0;
        switch (yych) {
            case '+':
            case '-': goto yy18;
            case '0':
            case '1':
            case '2':
            case '3':
            case '4':
            case '5':
            case '6':
            case '7':
            case '8':
            case '9': goto yy19;
            default: goto yy12;
        }
        yy14:
        ++i;
        yych = i < end ? *i : 0;
        switch (yych) {
            case 'l': goto yy20;
            default: goto yy12;
        }
        yy15:
        ++i;
        yych = i < end ? *i : 0;
        switch (yych) {
            case 'l': goto yy21;
            default: goto yy12;
        }
        yy16:
        ++i;
        yych = i < end ? *i : 0;
        switch (yych) {
            case 'u': goto yy22;
            default: goto yy12;
        }
        yy17:
        yyaccept = 0;
        ++i;
        marker = i;
        yych = i < end ? *i : 0;
        switch (yych) {
            case '0':
            case '1':
            case '2':
            case '3':
            case '4':
            case '5':
            case '6':
            case '7':
            case '8':
            case '9': goto yy17;
            case 'E':
            case 'e': goto yy13;
            default: goto yy6;
        }
        yy18:
        ++i;
        yych = i < end ? *i : 0;
        switch (yych) {
            case '0':
            case '1':
            case '2':
            case '3':
            case '4':
            case '5':
            case '6':
            case '7':
            case '8':
            case '9': goto yy19;
            default: goto yy12;
        }
        yy19:
        ++i;
        yych = i < end ? *i : 0;
        switch (yych) {
            case '0':
            case '1':
            case '2':
            case '3':
            case '4':
            case '5':
            case '6':
            case '7':
            case '8':
            case '9': goto yy19;
            default: goto yy6;
        }
        yy20:
        ++i;
        yych = i < end ? *i : 0;
        switch (yych) {
            case 's': goto yy22;
            default: goto yy12;
        }
        yy21:
        ++i;
        yych = i < end ? *i : 0;
        switch (yych) {
            case 'l': goto yy23;
            default: goto yy12;
        }
        yy22:
        ++i;
        yych = i < end ? *i : 0;
        switch (yych) {
            case 'e': goto yy23;
            default: goto yy12;
        }
        yy23:
        ++i;
        {
            color = TextEditor::Color::identifier;
            return i;
        }
        yy24:
        { return start; }
    }

}


//
//	TextEditor::Language::Json
//

const TextEditor::Language* TextEditor::Language::Json() {
    static bool initialized = false;
    static TextEditor::Language language;

    if (!initialized) {
        language.name = "JSON";
        language.hasDoubleQuotedStrings = true;
        language.stringEscape = '\\';

        static const char* const keywords[] = {
            "false", "null", "true"
        };

        for (auto& keyword : keywords) { language.keywords.insert(keyword); }

        language.customTokenizer = tokenizeJson;
        initialized = true;
    }

    return &language;
}


//
//	tokenizeMarkdown
//

static TextEditor::Iterator tokenizeMarkdown(TextEditor::Iterator start, TextEditor::Iterator end, TextEditor::Color& color) {
    TextEditor::Iterator i = start;
    TextEditor::Iterator marker;


    {
        ImWchar yych;
        yych = i < end ? *i : 0;
        switch (yych) {
            case '!': goto yy3;
            case '#': goto yy4;
            case '*': goto yy6;
            case '+': goto yy7;
            case '-': goto yy8;
            case '0':
            case '1':
            case '2':
            case '3':
            case '4':
            case '5':
            case '6':
            case '7':
            case '8':
            case '9': goto yy10;
            case ':':
            case '|': goto yy11;
            case '<': goto yy12;
            case '[': goto yy13;
            case '`': goto yy14;
            case '~': goto yy15;
            default:
                if (i >= end) goto yy37;
                goto yy1;
        }
        yy1:
        ++i;
        yy2:
        { return start; }
        yy3:
        ++i;
        marker = i;
        yych = i < end ? *i : 0;
        switch (yych) {
            case '[': goto yy16;
            default: goto yy2;
        }
        yy4:
        ++i;
        yych = i < end ? *i : 0;
        switch (yych) {
            case '\n': goto yy5;
            default:
                if (i >= end) goto yy5;
                goto yy4;
        }
        yy5:
        {
            color = TextEditor::Color::declaration;
            return i;
        }
        yy6:
        ++i;
        marker = i;
        yych = i < end ? *i : 0;
        switch (yych) {
            case ' ': goto yy11;
            case '*': goto yy20;
            default:
                if (i >= end) goto yy2;
                goto yy18;
        }
        yy7:
        ++i;
        yych = i < end ? *i : 0;
        switch (yych) {
            case ' ': goto yy11;
            default: goto yy2;
        }
        yy8:
        ++i;
        yych = i < end ? *i : 0;
        switch (yych) {
            case ' ': goto yy11;
            default: goto yy9;
        }
        yy9:
        {
            color = TextEditor::Color::punctuation;
            return i;
        }
        yy10:
        ++i;
        marker = i;
        yych = i < end ? *i : 0;
        switch (yych) {
            case '.': goto yy21;
            case '0':
            case '1':
            case '2':
            case '3':
            case '4':
            case '5':
            case '6':
            case '7':
            case '8':
            case '9': goto yy22;
            default: goto yy2;
        }
        yy11:
        ++i;
        goto yy9;
        yy12:
        ++i;
        marker = i;
        yych = i < end ? *i : 0;
        switch (yych) {
            case 'A':
            case 'B':
            case 'C':
            case 'D':
            case 'E':
            case 'F':
            case 'G':
            case 'H':
            case 'I':
            case 'J':
            case 'K':
            case 'L':
            case 'M':
            case 'N':
            case 'O':
            case 'P':
            case 'Q':
            case 'R':
            case 'S':
            case 'T':
            case 'U':
            case 'V':
            case 'W':
            case 'X':
            case 'Y':
            case 'Z':
            case 'a':
            case 'b':
            case 'c':
            case 'd':
            case 'e':
            case 'f':
            case 'g':
            case 'h':
            case 'i':
            case 'j':
            case 'k':
            case 'l':
            case 'm':
            case 'n':
            case 'o':
            case 'p':
            case 'q':
            case 'r':
            case 's':
            case 't':
            case 'u':
            case 'v':
            case 'w':
            case 'x':
            case 'y':
            case 'z': goto yy23;
            default: goto yy2;
        }
        yy13:
        ++i;
        marker = i;
        yych = i < end ? *i : 0;
        if (yych <= 0x00) {
            if (i >= end) goto yy2;
            goto yy16;
        }
        goto yy17;
        yy14:
        ++i;
        marker = i;
        yych = i < end ? *i : 0;
        if (yych <= 0x00) {
            if (i >= end) goto yy2;
            goto yy25;
        }
        goto yy26;
        yy15:
        ++i;
        marker = i;
        yych = i < end ? *i : 0;
        switch (yych) {
            case '~': goto yy28;
            default: goto yy2;
        }
        yy16:
        ++i;
        yych = i < end ? *i : 0;
        yy17:
        switch (yych) {
            case ']': goto yy24;
            default:
                if (i >= end) goto yy19;
                goto yy16;
        }
        yy18:
        ++i;
        yych = i < end ? *i : 0;
        switch (yych) {
            case ' ': goto yy19;
            case '*': goto yy29;
            default:
                if (i >= end) goto yy19;
                goto yy18;
        }
        yy19:
        i = marker;
        goto yy2;
        yy20:
        ++i;
        yych = i < end ? *i : 0;
        switch (yych) {
            case '*': goto yy19;
            default:
                if (i >= end) goto yy19;
                goto yy30;
        }
        yy21:
        ++i;
        yych = i < end ? *i : 0;
        switch (yych) {
            case ' ': goto yy11;
            default: goto yy19;
        }
        yy22:
        ++i;
        yych = i < end ? *i : 0;
        switch (yych) {
            case '.': goto yy21;
            case '0':
            case '1':
            case '2':
            case '3':
            case '4':
            case '5':
            case '6':
            case '7':
            case '8':
            case '9': goto yy22;
            default: goto yy19;
        }
        yy23:
        ++i;
        yych = i < end ? *i : 0;
        switch (yych) {
            case '>': goto yy31;
            case 'A':
            case 'B':
            case 'C':
            case 'D':
            case 'E':
            case 'F':
            case 'G':
            case 'H':
            case 'I':
            case 'J':
            case 'K':
            case 'L':
            case 'M':
            case 'N':
            case 'O':
            case 'P':
            case 'Q':
            case 'R':
            case 'S':
            case 'T':
            case 'U':
            case 'V':
            case 'W':
            case 'X':
            case 'Y':
            case 'Z':
            case 'a':
            case 'b':
            case 'c':
            case 'd':
            case 'e':
            case 'f':
            case 'g':
            case 'h':
            case 'i':
            case 'j':
            case 'k':
            case 'l':
            case 'm':
            case 'n':
            case 'o':
            case 'p':
            case 'q':
            case 'r':
            case 's':
            case 't':
            case 'u':
            case 'v':
            case 'w':
            case 'x':
            case 'y':
            case 'z': goto yy23;
            default: goto yy19;
        }
        yy24:
        ++i;
        yych = i < end ? *i : 0;
        switch (yych) {
            case '(': goto yy32;
            default: goto yy19;
        }
        yy25:
        ++i;
        yych = i < end ? *i : 0;
        yy26:
        switch (yych) {
            case '`': goto yy27;
            default:
                if (i >= end) goto yy19;
                goto yy25;
        }
        yy27:
        ++i;
        {
            color = TextEditor::Color::string;
            return i;
        }
        yy28:
        ++i;
        yych = i < end ? *i : 0;
        switch (yych) {
            case '~': goto yy19;
            default:
                if (i >= end) goto yy19;
                goto yy33;
        }
        yy29:
        ++i;
        {
            color = TextEditor::Color::number;
            return i;
        }
        yy30:
        ++i;
        yych = i < end ? *i : 0;
        switch (yych) {
            case '*': goto yy34;
            default:
                if (i >= end) goto yy19;
                goto yy30;
        }
        yy31:
        ++i;
        {
            color = TextEditor::Color::keyword;
            return i;
        }
        yy32:
        ++i;
        yych = i < end ? *i : 0;
        switch (yych) {
            case ')': goto yy35;
            default:
                if (i >= end) goto yy19;
                goto yy32;
        }
        yy33:
        ++i;
        yych = i < end ? *i : 0;
        switch (yych) {
            case '~': goto yy36;
            default:
                if (i >= end) goto yy19;
                goto yy33;
        }
        yy34:
        ++i;
        yych = i < end ? *i : 0;
        switch (yych) {
            case '*': goto yy29;
            default: goto yy19;
        }
        yy35:
        ++i;
        {
            color = TextEditor::Color::identifier;
            return i;
        }
        yy36:
        ++i;
        yych = i < end ? *i : 0;
        switch (yych) {
            case '~': goto yy29;
            default: goto yy19;
        }
        yy37:
        { return start; }
    }

}


//
//	TextEditor::Language::Markdown
//

const TextEditor::Language* TextEditor::Language::Markdown() {
    static bool initialized = false;
    static TextEditor::Language language;

    if (!initialized) {
        language.name = "Markdown";
        language.commentStart = "<!--";
        language.commentEnd = "-->";

        language.customTokenizer = tokenizeMarkdown;
        initialized = true;
    }

    return &language;
}


//
//	TextEditor::Language::Sql
//

const TextEditor::Language* TextEditor::Language::Sql() {
    static bool initialized = false;
    static TextEditor::Language language;

    if (!initialized) {
        language.name = "SQL";
        language.caseSensitive = false;
        language.singleLineComment = "--";
        language.commentStart = "/*";
        language.commentEnd = "*/";
        language.hasSingleQuotedStrings = true;
        language.hasDoubleQuotedStrings = true;
        language.stringEscape = '\\';

        static const char* const keywords[] = {
            "abs", "absent", "acos", "all", "allocate", "alter", "and", "any", "any_value", "are", "array", "array_agg",
            "array_max_cardinality", "as", "asensitive", "asin", "asymmetric", "at", "atan", "atomic", "authorization",
            "avg", "begin", "begin_frame", "begin_partition", "between", "bigint", "binary", "blob", "boolean", "both",
            "btrim", "by", "call", "called", "cardinality", "cascaded", "case", "cast", "ceil", "ceiling", "char",
            "character", "character_length", "char_length", "check", "classifier", "clob", "close", "coalesce", "collate",
            "collect", "column", "commit", "condition", "connect", "constraint", "contains", "convert", "copy", "corr",
            "corresponding", "cos", "cosh", "count", "covar_pop", "covar_samp", "create", "cross", "cube", "cume_dist",
            "current", "current_catalog", "current_date", "current_default_transform_group", "current_path", "current_role",
            "current_row", "current_schema", "current_time", "current_timestamp", "current_transform_group_for_type",
            "current_user", "cursor", "cycle", "date", "day", "deallocate", "dec", "decfloat", "decimal", "declare",
            "default", "define", "delete", "dense_rank", "deref", "describe", "deterministic", "disconnect", "distinct",
            "double", "drop", "dynamic", "each", "element", "else", "empty", "end", "end-exec", "end_frame", "end_partition",
            "equals", "escape", "every", "except", "exec", "execute", "exists", "exp", "external", "extract", "false", "fetch",
            "filter", "first_value", "float", "floor", "for", "foreign", "frame_row", "free", "from", "full", "function",
            "fusion", "get", "global", "grant", "greatest", "group", "grouping", "groups", "having", "hold", "hour",
            "identity", "in", "indicator", "initial", "inner", "inout", "insensitive", "insert", "int", "integer",
            "intersect", "intersection", "interval", "into", "is", "join", "json", "json_array", "json_arrayagg",
            "json_exists", "json_object", "json_objectagg", "json_query", "json_scalar", "json_serialize", "json_table",
            "json_table_primitive", "json_value", "lag", "language", "large", "last_value", "lateral", "lead", "leading",
            "least", "left", "like", "like_regex", "limit", "listagg", "ln", "local", "localtime", "localtimestamp", "log", "log10",
            "lower", "lpad", "ltrim", "match", "matches", "match_number", "match_recognize", "max", "member", "merge", "method",
            "min", "minute", "mod", "modifies", "module", "month", "multiset", "national", "natural", "nchar", "nclob", "new",
            "no", "none", "normalize", "not", "nth_value", "ntile", "null", "nullif", "numeric", "occurrences_regex",
            "octet_length", "of", "offset", "old", "omit", "on", "one", "only", "open", "or", "order", "out", "outer", "over",
            "overlaps", "overlay", "parameter", "partition", "pattern", "per", "percent", "percentile_cont", "percentile_disc",
            "percent_rank", "period", "portion", "position", "position_regex", "power", "precedes", "precision", "prepare", "primary",
            "procedure", "ptf", "range", "rank", "reads", "real", "recursive", "ref", "references", "referencing", "regr_avgx",
            "regr_avgy", "regr_count", "regr_intercept", "regr_r2", "regr_slope", "regr_sxx", "regr_sxy", "regr_syy", "release",
            "result", "return", "returns", "revoke", "right", "rollback", "rollup", "row", "rows", "row_number", "rpad", "running",
            "savepoint", "scope", "scroll", "search", "second", "seek", "select", "sensitive", "session_user", "set", "show", "similar",
            "sin", "sinh", "skip", "smallint", "some", "specific", "specifictype", "sql", "sqlexception", "sqlstate", "sqlwarning", "sqrt",
            "start", "static", "stddev_pop", "stddev_samp", "submultiset", "subset", "substring", "substring_regex", "succeeds",
            "sum", "symmetric", "system", "system_time", "system_user", "table", "tablesample", "tan", "tanh", "then", "time",
            "timestamp", "timezone_hour", "timezone_minute", "to", "trailing", "translate", "translate_regex", "translation",
            "treat", "trigger", "trim", "trim_array", "true", "truncate", "uescape", "union", "unique", "unknown", "unnest",
            "update", "upper", "user", "using", "value", "values", "value_of", "varbinary", "varchar", "varying", "var_pop",
            "var_samp", "versioning", "when", "whenever", "where", "width_bucket", "window", "with", "within", "without", "year"
        };

        for (auto& keyword : keywords) { language.keywords.insert(keyword); }

        language.isPunctuation = isCStylePunctuation;
        language.getIdentifier = getCStyleIdentifier;
        language.getNumber = getCStyleNumber;
        initialized = true;
    }

    return &language;
}
