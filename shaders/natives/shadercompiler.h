#ifndef _SHADER_COMPILER_
#define _SHADER_COMPILER_
#ifdef __cplusplus
extern "C" {
#endif
#ifdef _WIN32
#define SHEXPORT __declspec(dllexport)
#else
#define SHEXPORT
#endif
SHEXPORT void SHInit();
#define SH_KIND_VERTEX (0)
#define SH_KIND_FRAGMENT (1)
#define SH_KIND_GEOMETRY (2)
SHEXPORT const char *SHCompile(const char *source, const char *filename, const char *defines, int kind);
SHEXPORT void SHFreeResult(const char *ptr);
SHEXPORT void SHFinish();
#ifdef __cplusplus
}
#endif
#endif
