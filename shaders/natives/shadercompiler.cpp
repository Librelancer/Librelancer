#include <vector>
#include <utility>
#include "gl2spv.h"
#include "spirv_glsl.hpp"
#include <string.h>
#include "shadercompiler.h"

#ifdef _WIN32
#define strdup _strdup
#endif
int ToGLSL(std::vector<uint32_t>& spirv_binary, std::string& outstr)
{
    try
    {
        spirv_cross::CompilerGLSL glsl(std::move(spirv_binary));
	    spirv_cross::ShaderResources res = glsl.get_shader_resources();
        spirv_cross::CompilerGLSL::Options options;
        options.version = 150;
        options.es = false;
        options.enable_420pack_extension = false;
        glsl.set_common_options(options);

        std::string source = glsl.compile();
        source.erase(0, source.find("\n") + 1); //remove version directive
        outstr = "#ifdef GL_ES\nprecision highp float;\nprecision highp int;\n#endif\n" + source;
        return 1;
    }
    catch(const std::exception& e)
    {
      fprintf(stderr, "Compiler threw an exception: %s\n", e.what());
      return 0;
    }    
}

SHEXPORT void SHInit()
{
    GlslInit();
}

SHEXPORT const char *SHCompile(const char *source, const char *filename, const char *defines, int kind)
{
    std::vector<uint32_t> spv;
    const char *kindString = kind == SH_KIND_VERTEX ? "vertex"
                             : kind == SH_KIND_FRAGMENT ? "fragment"
                             : "geometry";
    if(!CompileShader(source, filename, defines, kind, spv)) {
        fprintf(stderr, "%s shader compilation failed\n", kindString);
        return NULL;
    }
    std::string outGlsl;
    if(!ToGLSL(spv, outGlsl)) {
        fprintf(stderr, "%s shader compilation failed\n", kindString);
        return NULL;
    }
    return strdup(outGlsl.c_str());
}

SHEXPORT void SHFreeResult(const char *ptr)
{
  free((void*)ptr);
}

SHEXPORT void SHFinish()
{
    GlslDestroy();
}
