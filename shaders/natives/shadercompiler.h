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
SHEXPORT const char *SHCompile(const char *source, const char *filename, const char *defines, bool vertex);
SHEXPORT void SHFreeResult(const char *ptr);
SHEXPORT void SHFinish();
#ifdef __cplusplus
}
#endif
#endif
