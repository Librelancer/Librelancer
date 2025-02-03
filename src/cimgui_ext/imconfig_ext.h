#define IMGUI_DISABLE_OBSOLETE_KEYIO
#undef NDEBUG
void igCSharpAssert(bool expr, const char *exprString, const char *file, int line);
#define _IGSTR(x) #x
#define IM_ASSERT(_EXPR) igCSharpAssert((_EXPR) ? true : false, _IGSTR(_EXPR), __FILE__, __LINE__)
