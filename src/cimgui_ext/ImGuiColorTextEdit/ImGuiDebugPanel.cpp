#include "TextEditor.h"

void TextEditor::ImGuiDebugPanel(const std::string& panelName)
{
	ImGui::Begin(panelName.c_str());

	if (ImGui::CollapsingHeader("Editor state info"))
	{
		ImGui::Checkbox("Panning", &mPanning);
		ImGui::Checkbox("Dragging selection", &mDraggingSelection);
		ImGui::DragInt("Cursor count", &mState.mCurrentCursor);
		for (int i = 0; i <= mState.mCurrentCursor; i++)
		{
			static Coordinates sanitizedStart, sanitizedEnd;
			sanitizedStart = SanitizeCoordinates(mState.mCursors[i].mInteractiveStart);
			sanitizedEnd = SanitizeCoordinates(mState.mCursors[i].mInteractiveStart);
			ImGui::DragInt2("Interactive start", &mState.mCursors[i].mInteractiveStart.mLine);
			ImGui::DragInt2("Interactive end", &mState.mCursors[i].mInteractiveEnd.mLine);
			ImGui::Text("Sanitized start: %d, %d", sanitizedStart.mLine, sanitizedStart.mColumn);
			ImGui::Text("Sanitized end:   %d, %d", sanitizedEnd.mLine, sanitizedEnd.mColumn);
		}
	}
	if (ImGui::CollapsingHeader("Lines"))
	{
		for (int i = 0; i < mLines.size(); i++)
		{
			ImGui::Text("%zu", mLines[i].size());
		}
	}
	if (ImGui::CollapsingHeader("Undo"))
	{
		static std::string numberOfRecordsText;
		numberOfRecordsText = "Number of records: " + std::to_string(mUndoBuffer.size());
		ImGui::Text("%s", numberOfRecordsText.c_str());
		ImGui::DragInt("Undo index", &mState.mCurrentCursor);
		for (int i = 0; i < mUndoBuffer.size(); i++)
		{
			if (ImGui::CollapsingHeader(std::to_string(i).c_str()))
			{

				ImGui::Text("Operations");
				for (int j = 0; j < mUndoBuffer[i].mOperations.size(); j++)
				{
					ImGui::Text("%s", mUndoBuffer[i].mOperations[j].mText.c_str());
					ImGui::Text(mUndoBuffer[i].mOperations[j].mType == UndoOperationType::Add ? "Add" : "Delete");
					ImGui::DragInt2("Start", &mUndoBuffer[i].mOperations[j].mStart.mLine);
					ImGui::DragInt2("End", &mUndoBuffer[i].mOperations[j].mEnd.mLine);
					ImGui::Separator();
				}
			}
		}
	}
	if (ImGui::Button("Run unit tests"))
	{
		UnitTests();
	}
	ImGui::End();
}