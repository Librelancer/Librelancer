// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

#ifndef _CIMGUI_MEMEDIT_H
#define _CIMGUI_MEMEDIT_H_

#ifdef __cplusplus
extern "C" {
#endif
#ifdef _WIN32
#define IGEXPORT __declspec(dllexport)
#else
#define IGEXPORT
#endif 
#include <stddef.h>
typedef void* memoryedit_t;

IGEXPORT memoryedit_t igExtMemoryEditInit();
IGEXPORT void igExtMemoryEditDrawContents(memoryedit_t memedit, void *mem_data_void_ptr, size_t mem_size, size_t base_display_addr);
IGEXPORT void igExtMemoryEditFree(memoryedit_t memedit);
#ifdef __cplusplus
}
#endif
#endif
