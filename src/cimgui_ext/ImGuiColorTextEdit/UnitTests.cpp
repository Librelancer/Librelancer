#include "TextEditor.h"

void TextEditor::UnitTests()
{
	SetText(" \t  \t   \t \t\n");
	// --- GetCharacterColumn --- //
	{
		// Returns column given line and character index in that line.
		// Column is on the left side of character
		assert(GetCharacterColumn(0, 0) == 0);
		assert(GetCharacterColumn(0, 1) == 1);
		assert(GetCharacterColumn(0, 2) == 4);
		assert(GetCharacterColumn(0, 3) == 5);
		assert(GetCharacterColumn(0, 4) == 6);
		assert(GetCharacterColumn(0, 5) == 8);
		assert(GetCharacterColumn(0, 6) == 9);
		assert(GetCharacterColumn(0, 7) == 10);
		assert(GetCharacterColumn(0, 8) == 11);
		assert(GetCharacterColumn(0, 9) == 12);
		assert(GetCharacterColumn(0, 10) == 13);
		assert(GetCharacterColumn(0, 11) == 16);
		assert(GetCharacterColumn(0, 12) == 16); // out of range
		// empty line
		assert(GetCharacterColumn(1, 0) == 0);
		assert(GetCharacterColumn(1, 1) == 0); // out of range
		assert(GetCharacterColumn(1, 2) == 0); // out of range
		// nonexistent line
		assert(GetCharacterColumn(2, 0) == 0);
		assert(GetCharacterColumn(2, 1) == 0);
		assert(GetCharacterColumn(2, 2) == 0);
	}

	// --- GetCharacterIndexL --- //
	{
		// Returns character index from coordinates, if coordinates are in the middle of a tab character it returns character index of that tab character
		assert(GetCharacterIndexL({ 0, 0 }) == 0);
		assert(GetCharacterIndexL({ 0, 1 }) == 1);
		assert(GetCharacterIndexL({ 0, 2 }) == 1);
		assert(GetCharacterIndexL({ 0, 3 }) == 1);
		assert(GetCharacterIndexL({ 0, 4 }) == 2);
		assert(GetCharacterIndexL({ 0, 5 }) == 3);
		assert(GetCharacterIndexL({ 0, 6 }) == 4);
		assert(GetCharacterIndexL({ 0, 7 }) == 4);
		assert(GetCharacterIndexL({ 0, 8 }) == 5);
		assert(GetCharacterIndexL({ 0, 9 }) == 6);
		assert(GetCharacterIndexL({ 0, 10 }) == 7);
		assert(GetCharacterIndexL({ 0, 11 }) == 8);
		assert(GetCharacterIndexL({ 0, 12 }) == 9);
		assert(GetCharacterIndexL({ 0, 13 }) == 10);
		assert(GetCharacterIndexL({ 0, 14 }) == 10);
		assert(GetCharacterIndexL({ 0, 15 }) == 10);
		assert(GetCharacterIndexL({ 0, 16 }) == 11);
		assert(GetCharacterIndexL({ 0, 17 }) == 11); // out of range
		assert(GetCharacterIndexL({ 0, 18 }) == 11); // out of range
		// empty line
		assert(GetCharacterIndexL({ 1, 0 }) == 0);
		assert(GetCharacterIndexL({ 1, 1 }) == 0); // out of range
		assert(GetCharacterIndexL({ 1, 2 }) == 0); // out of range
		// nonexistent line
		assert(GetCharacterIndexL({ 2, 0 }) == -1);
		assert(GetCharacterIndexL({ 2, 1 }) == -1);
		assert(GetCharacterIndexL({ 2, 2 }) == -1);
	}

	// --- GetCharacterIndexR --- //
	{
		// Returns character index from coordinates, if coordinates are in the middle of a tab character it returns character index of next character
		assert(GetCharacterIndexR({ 0, 0 }) == 0);
		assert(GetCharacterIndexR({ 0, 1 }) == 1);
		assert(GetCharacterIndexR({ 0, 2 }) == 2);
		assert(GetCharacterIndexR({ 0, 3 }) == 2);
		assert(GetCharacterIndexR({ 0, 4 }) == 2);
		assert(GetCharacterIndexR({ 0, 5 }) == 3);
		assert(GetCharacterIndexR({ 0, 6 }) == 4);
		assert(GetCharacterIndexR({ 0, 7 }) == 5);
		assert(GetCharacterIndexR({ 0, 8 }) == 5);
		assert(GetCharacterIndexR({ 0, 9 }) == 6);
		assert(GetCharacterIndexR({ 0, 10 }) == 7);
		assert(GetCharacterIndexR({ 0, 11 }) == 8);
		assert(GetCharacterIndexR({ 0, 12 }) == 9);
		assert(GetCharacterIndexR({ 0, 13 }) == 10);
		assert(GetCharacterIndexR({ 0, 14 }) == 11);
		assert(GetCharacterIndexR({ 0, 15 }) == 11);
		assert(GetCharacterIndexR({ 0, 16 }) == 11);
		assert(GetCharacterIndexR({ 0, 17 }) == 11); // out of range
		assert(GetCharacterIndexR({ 0, 18 }) == 11); // out of range
		// empty line
		assert(GetCharacterIndexR({ 1, 0 }) == 0);
		assert(GetCharacterIndexR({ 1, 1 }) == 0); // out of range
		assert(GetCharacterIndexR({ 1, 2 }) == 0); // out of range
		// nonexistent line
		assert(GetCharacterIndexR({ 2, 0 }) == -1);
		assert(GetCharacterIndexR({ 2, 1 }) == -1);
		assert(GetCharacterIndexR({ 2, 2 }) == -1);
	}

	// --- GetText --- //
	{
		// Gets text from aStart to aEnd, tabs are counted on the start position
		std::string text = GetText({ 0, 0 }, { 0, 1 });
		assert(text.compare(" ") == 0);
		text = GetText({ 0, 1 }, { 0, 2 });
		assert(text.compare("\t") == 0);
		text = GetText({ 0, 2 }, { 0, 3 });
		assert(text.compare("") == 0);
		text = GetText({ 0, 3 }, { 0, 4 });
		assert(text.compare("") == 0);
		text = GetText({ 0, 4 }, { 0, 5 });
		assert(text.compare(" ") == 0);
		text = GetText({ 0, 5 }, { 0, 6 });
		assert(text.compare(" ") == 0);
		text = GetText({ 0, 6 }, { 0, 7 });
		assert(text.compare("\t") == 0);
		text = GetText({ 0, 7 }, { 0, 8 });
		assert(text.compare("") == 0);

		text = GetText({ 0, 0 }, { 0, 8 });
		assert(text.compare(" \t  \t") == 0);
		text = GetText({ 0, 0 }, { 0, 7 });
		assert(text.compare(" \t  \t") == 0);
		text = GetText({ 0, 0 }, { 0, 6 });
		assert(text.compare(" \t  ") == 0);
		text = GetText({ 0, 4 }, { 0, 12 });
		assert(text.compare("  \t   \t") == 0);
		text = GetText({ 0, 4 }, { 0, 13 });
		assert(text.compare("  \t   \t ") == 0);
		text = GetText({ 0, 4 }, { 0, 14 });
		assert(text.compare("  \t   \t \t") == 0);
		text = GetText({ 0, 4 }, { 0, 15 });
		assert(text.compare("  \t   \t \t") == 0);
		text = GetText({ 0, 4 }, { 0, 16 });
		assert(text.compare("  \t   \t \t") == 0);

		text = GetText({ 0, 0 }, { 1, 0 });
		assert(text.compare(" \t  \t   \t \t\n") == 0);
	}

	// --- DeleteRange --- //
	{
		// Deletes from start to end coordinates, any overlapping tabs will be deleted, doesn't allow out of range lines
		DeleteRange({ 0, 0 }, { 0, 0 });
		assert(GetText() == " \t  \t   \t \t\n");
		DeleteRange({ 0, 0 }, { 0, 1 });
		assert(GetText() == "\t  \t   \t \t\n");
		DeleteRange({ 0, 0 }, { 0, 2 });
		assert(GetText() == "  \t   \t \t\n");
		DeleteRange({ 0, 12 }, { 0, 12 });
		assert(GetText() == "  \t   \t \t\n");
		DeleteRange({ 1, 0 }, { 1, 0 });
		assert(GetText() == "  \t   \t \t\n");
		DeleteRange({ 0, 11 }, { 0, 12 });
		assert(GetText() == "  \t   \t \n");
		DeleteRange({ 0, 2 }, { 0, 3 });
		assert(GetText() == "     \t \n");
		DeleteRange({ 0, 6 }, { 0, 7 });
		assert(GetText() == "      \n");
		SetText("a\nb\nc\nd\ne");
		DeleteRange({ 0, 0 }, { 2, 1 });
		assert(GetText() == "\nd\ne");
		DeleteRange({ 1, 1 }, { 2, 0 });
		assert(GetText() == "\nde");
		DeleteRange({ 1, 1 }, { 1, 15 }); // out of range column
		assert(GetText() == "\nd");
		SetText("asdf\nzxcv\nqwer\npo");
		DeleteRange({ 1, 2 }, { 1, 200 }); // out of range column
		assert(GetText() == "asdf\nzx\nqwer\npo");
		DeleteRange({ 0, 500 }, { 2, 500 }); // out of range column
		assert(GetText() == "asdf\npo");
	}

	// --- RemoveGlyphsFromLine --- //
	{
		// 
	}


	SetText("asdf asdf\nasdf\nasdf\tasdf\n zxcv zxcv");
	// --- FindNextOccurrence --- //
	{
		Coordinates outStart, outEnd;
		assert(FindNextOccurrence("asdf", 4, { 0, 0 }, outStart, outEnd) && outStart == Coordinates(0, 0) && outEnd == Coordinates(0, 4));
		assert(FindNextOccurrence("asdf", 4, { 0, 1 }, outStart, outEnd) && outStart == Coordinates(0, 5) && outEnd == Coordinates(0, 9));
		assert(FindNextOccurrence("asdf", 4, { 0, 5 }, outStart, outEnd) && outStart == Coordinates(0, 5) && outEnd == Coordinates(0, 9));
		assert(FindNextOccurrence("asdf", 4, { 0, 6 }, outStart, outEnd) && outStart == Coordinates(1, 0) && outEnd == Coordinates(1, 4));
		assert(FindNextOccurrence("asdf", 4, { 3, 3 }, outStart, outEnd) && outStart == Coordinates(0, 0) && outEnd == Coordinates(0, 4)); // go to line 0 if reach end of file
		assert(FindNextOccurrence("zxcv", 4, { 3, 10 }, outStart, outEnd) && outStart == Coordinates(3, 1) && outEnd == Coordinates(3, 5)); // from behind in same line
		assert(!FindNextOccurrence("lalal", 4, { 3, 5 }, outStart, outEnd)); // not found
	}

	SetText("\t\t\nasd\t\n");
	// --- SanitizeCoordinates --- //
	{
		mTabSize = 4;
		assert(SanitizeCoordinates(Coordinates(1, 200)) == Coordinates(1, 4));
		assert(SanitizeCoordinates(Coordinates(1, 3)) == Coordinates(1, 3));
		assert(SanitizeCoordinates(Coordinates(0, 0)) == Coordinates(0, 0));
		assert(SanitizeCoordinates(Coordinates(0, 1)) == Coordinates(0, 0));
		assert(SanitizeCoordinates(Coordinates(0, 2)) == Coordinates(0, 0));
		assert(SanitizeCoordinates(Coordinates(0, 3)) == Coordinates(0, 4));
		assert(SanitizeCoordinates(Coordinates(0, 4)) == Coordinates(0, 4));
		assert(SanitizeCoordinates(Coordinates(0, 5)) == Coordinates(0, 4));
	}
}
