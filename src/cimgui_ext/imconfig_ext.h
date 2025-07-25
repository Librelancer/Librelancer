#define IMGUI_DISABLE_OBSOLETE_KEYIO
#define IMGUI_DISABLE_OBSOLETE_FUNCTIONS
#define IMGUI_ENABLE_FREETYPE
#undef NDEBUG
void igCSharpAssert(bool expr, const char *exprString, const char *file, int line);
#define _IGSTR(x) #x
#define IM_ASSERT(_EXPR) igCSharpAssert((_EXPR) ? true : false, _IGSTR(_EXPR), __FILE__, __LINE__)
#ifdef _WIN32
#if BUILDING_CIMGUI
#define CIMGUI_API __declspec(dllexport)
#else
#define CIMGUI_API __declspec(dllimport)
#endif
#else
#define CIMGUI_API __attribute__((visibility("default")))
#endif