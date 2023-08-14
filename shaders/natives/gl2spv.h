#ifndef _GL2SPV_H_
#define _GL2SPV_H_

#include <vector>
#include <stdint.h>
#define GL_KIND_VERTEX (0)
#define GL_KIND_FRAGMENT (1)
#define GL_KIND_GEOMETRY (2)

int CompileShader(const char *inputStr, const char *inputName, const char *defines, int kind, std::vector<uint32_t>& spv);

void GlslInit();
void GlslDestroy();
#endif
