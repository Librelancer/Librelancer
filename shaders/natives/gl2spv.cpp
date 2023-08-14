#ifndef _CRT_SECURE_NO_WARNINGS
#define _CRT_SECURE_NO_WARNINGS
#endif

#include <stdio.h>
#include <stdlib.h>
#include <glslang/Public/ShaderLang.h>
#include <glslang/Public/ResourceLimits.h>
#include "SPIRV/GlslangToSpv.h"
#include "gl2spv.h"

const char *glslVersion = "#version 320 es\nprecision highp float;\nprecision highp int;\n";


#define PRINT_LOG if(log && strlen(log) > 0) { \
fprintf(stderr, "%s\n", log); \
}


int CompileShader_Internal(const char *inputStr, const char *inputName, const char *defines, int kind, std::vector<uint32_t>& spv)
{
    EShLanguage stage = kind == GL_KIND_VERTEX ?  EShLangVertex
                        : kind == GL_KIND_FRAGMENT ? EShLangFragment
                        : EShLangGeometry;
    glslang::TShader shader(stage);
    const char* strings[3];
    int lengths[3];
    const char* names[3];
    strings[0] = glslVersion;
    lengths[0] = strlen(glslVersion);
    names[0] = "version string";
    strings[1] = defines;
    lengths[1] = strlen(defines);
    names[1] = "defines";
    strings[2] = inputStr;
    lengths[2] = strlen(inputStr);
    names[2] = inputName;
    shader.setAutoMapBindings(true);
    shader.setAutoMapLocations(true);
    shader.setStringsWithLengthsAndNames(strings, lengths, names, 3);
    shader.setEnvInput(glslang::EShSourceGlsl, stage, glslang::EShClientOpenGL, 150);
    shader.setEnvClient(glslang::EShClientOpenGL, glslang::EShTargetOpenGL_450);
    shader.setEnvTarget(glslang::EShTargetSpv, glslang::EShTargetSpv_1_0);
    const TBuiltInResource *res = GetDefaultResources();
    if(!shader.parse(res, 150, true, EShMsgDefault)) {
        const char *log = shader.getInfoDebugLog();
        PRINT_LOG
        log = shader.getInfoLog();
        PRINT_LOG
        fprintf(stderr, "compile failed\n");
        return 0;
    }

    const char *log = shader.getInfoLog();
    PRINT_LOG

    glslang::TProgram program;
    program.addShader(&shader);
    if(!program.link(EShMsgDefault)) {
        const char *log = program.getInfoDebugLog();
        PRINT_LOG
        log = program.getInfoLog();
        PRINT_LOG
        fprintf(stderr, "link failed\n");
        return 0;
    }
    log = program.getInfoLog();
    if(log && strlen(log) > 0) {
        fprintf(stderr, "%s\n", log);
    }
    glslang::SpvOptions spv_opts;
    spv_opts.validate = true;
    spv::SpvBuildLogger logger;
    glslang::GlslangToSpv(*program.getIntermediate(stage), spv, &logger, &spv_opts);
    if (!logger.getAllMessages().empty())
        fprintf(stderr, "%s\n", logger.getAllMessages().c_str());
    
    return 1;
}

int CompileShader(const char *inputStr, const char *inputName, const char *defines, int kind, std::vector<uint32_t>& spv)
{
    int retval = CompileShader_Internal(inputStr, inputName, defines, kind, spv);
    return retval;
}

void GlslInit() 
{
    glslang::InitializeProcess();
}
void GlslDestroy()
{
    glslang::FinalizeProcess();
}
