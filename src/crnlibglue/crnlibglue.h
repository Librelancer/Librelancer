// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

#ifndef _CRNLIB_GLUE_H
#define _CRNLIB_GLUE_H
#ifdef __cplusplus
extern "C" {
#endif
#ifdef _WIN32
#if BUILDING_CRNLIB
#define CRNEXPORT __declspec(dllexport)
#else
#define CRNEXPORT __declspec(dllimport)
#endif
#else
#define CRNEXPORT __attribute__((visibility("default")))
#endif

typedef enum crnglue_format {
	CRNGLUE_FORMAT_DXT1,
	CRNGLUE_FORMAT_DXT1A,
	CRNGLUE_FORMAT_DXT3,
	CRNGLUE_FORMAT_DXT5,
	CRNGLUE_FORMAT_RGTC2,
	CRNGLUE_FORMAT_RGTC1_METALLIC,
	CRNGLUE_FORMAT_RGTC1_ROUGHNESS
} crnglue_format_t;

typedef enum crnglue_mipmaps {
	CRNGLUE_MIPMAPS_NONE,
	CRNGLUE_MIPMAPS_BOX,
	CRNGLUE_MIPMAPS_TENT,
	CRNGLUE_MIPMAPS_LANCZOS4,
	CRNGLUE_MIPMAPS_MITCHELL,
	CRNGLUE_MIPMAPS_KAISER
} crnglue_mipmaps_t;

typedef struct crnglue_miplevel {
    int width;
    int height;
    unsigned char *data;
    int dataSize;
} crnglue_miplevel_t;

typedef struct crnglue_mipmap_output {
    crnglue_miplevel_t *levels;
    int levelCount;
} crnglue_mipmap_output_t;
#define CRNGLUE_OK (1)
#define CRNGLUE_ERROR (0)

CRNEXPORT int CrnGlueCompressDDS(const unsigned char *input, int inWidth, int inHeight, crnglue_format_t format, crnglue_mipmaps_t mipmaps, int highQualitySlow, unsigned char **output, unsigned int *outputSize);
CRNEXPORT void CrnGlueFreeDDS(void *mem);

CRNEXPORT int CrnGlueGenerateMipmaps(const unsigned char *input, int inWidth, int inHeight, crnglue_mipmaps_t mipmaps, crnglue_mipmap_output_t *output);
CRNEXPORT void CrnGlueFreeMipmaps(crnglue_mipmap_output_t *output);

#ifdef __cplusplus
}
#endif
#endif 
