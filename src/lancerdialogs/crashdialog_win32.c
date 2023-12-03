#include "crashdialog_win32.h"
#ifdef __MINGW32__
// Explicitly setting NTDDI version, this is necessary for the MinGW compiler
#define NTDDI_VERSION NTDDI_VISTA
#define _WIN32_WINNT _WIN32_WINNT_VISTA
#endif

#ifndef UNICODE
#define UNICODE
#endif

//Regular stuff
#define WIN32_LEAN_AND_MEAN
#include <windows.h>
#include <commctrl.h>
#include <string.h>

DLGAPI Win32CrashDialog(const wchar_t *title, const wchar_t *message, const wchar_t *details)
{
	TASKDIALOGCONFIG config;
	TASKDIALOG_BUTTON copyButton;
	copyButton.nButtonID = 7;
	copyButton.pszButtonText = L"Copy Details";
	memset(&config, 0, sizeof(TASKDIALOGCONFIG));
	config.cbSize = sizeof(TASKDIALOGCONFIG);
	config.pszWindowTitle = title;
	config.cButtons = 1;
	config.pButtons = &copyButton;
	config.pszContent = message;
	config.pszExpandedInformation = details;
	config.dwCommonButtons = TDCBF_OK_BUTTON;
	config.dwFlags = TDF_SIZE_TO_CONTENT | TDF_ENABLE_HYPERLINKS | TDF_EXPANDED_BY_DEFAULT;
	int buttonPicked;
	TaskDialogIndirect(&config, &buttonPicked, NULL, NULL);
	if(buttonPicked == 7) {
		if(!OpenClipboard(NULL)) return 0;
		size_t wbuf_length = wcslen(details) + 1;
		HGLOBAL wbuf_handle = GlobalAlloc(GMEM_MOVEABLE, wbuf_length * sizeof(wchar_t));
		if(!wbuf_handle) {
			CloseClipboard();
			return 0;
		}
		wchar_t *wbuf_global = GlobalLock(wbuf_handle);
		wcscpy_s(wbuf_global, wbuf_length, details);
		GlobalUnlock(wbuf_handle);
		EmptyClipboard();
		if(SetClipboardData(CF_UNICODETEXT, wbuf_handle) == NULL)
			GlobalFree(wbuf_handle);
		CloseClipboard();
	}
	return 1;
}
