// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package
#include <stdlib.h>
#include <crnlib.h>
#include <crn_mipmapped_texture.h>
#include <crn_texture_comp.h>
#include "crnlibglue.h"
#include <stdio.h>

static bool SetMipmapParameters(crnglue_mipmaps_t mipmaps, crn_mipmap_params& mipparams)
{
    if(mipmaps == CRNGLUE_MIPMAPS_NONE) return false;
    mipparams = crn_mipmap_params();
    switch(mipmaps) {
        default:
        case CRNGLUE_MIPMAPS_BOX:
            mipparams.m_filter = cCRNMipFilterBox;
            break;
        case CRNGLUE_MIPMAPS_TENT:
            mipparams.m_filter = cCRNMipFilterTent;
            break;
        case CRNGLUE_MIPMAPS_LANCZOS4:
            mipparams.m_filter = cCRNMipFilterLanczos4;
            break;
        case CRNGLUE_MIPMAPS_MITCHELL:
            mipparams.m_filter = cCRNMipFilterMitchell;
            break;
        case CRNGLUE_MIPMAPS_KAISER:
            mipparams.m_filter = cCRNMipFilterKaiser;
            break;
    }
    return true;
}

// Functions to enable taking BGRA input from Librelancer

static void swap_channels(unsigned char *buffer, int width, int height)
{
    int len = width * height * 4;
    for(int i = 0; i < len; i+= 4) {
        unsigned char temp = buffer[i + 2];
        buffer[i + 2] = buffer[i];
        buffer[i] = temp;
    }
}

static unsigned char *rgba_input(const unsigned char *input, int width, int height)
{
    unsigned char *newBuffer = (unsigned char*)malloc((size_t)(width * height * 4));
    memcpy(newBuffer, input, (size_t)(width * height * 4));
    swap_channels(newBuffer, width, height);
    return newBuffer;    
}

static unsigned char *channel_input(const unsigned char *input, int width, int height, int channel)
{
    unsigned char *newBuffer = (unsigned char*)malloc((size_t)(width * height * 4));
    memcpy(newBuffer, input, (size_t)(width * height * 4));
    int len = width * height * 4;
    for(int i = 0; i < len; i+= 4) {
        unsigned char temp = newBuffer[i + channel];
        newBuffer[i] = temp;
        newBuffer[i + 1] = temp;
        newBuffer[i + 2] = temp;
    }
    return newBuffer;    
}

CRNEXPORT int CrnGlueCompressDDS(const unsigned char *input, int inWidth, int inHeight, crnglue_format_t format, crnglue_mipmaps_t mipmaps, int highQualitySlow, unsigned char **output, unsigned int *outputSize)
{
	crn_comp_params compression = crn_comp_params();
    crn_mipmap_params mipparams;
	compression.m_file_type = cCRNFileTypeDDS;
	unsigned char *rgba; 
	if(format == CRNGLUE_FORMAT_RGTC1_METALLIC) {
	    rgba = channel_input(input, inWidth, inHeight, 0);
	} else if (format == CRNGLUE_FORMAT_RGTC1_ROUGHNESS) {
	    rgba = channel_input(input, inWidth, inHeight, 1);
    } else {
        rgba = rgba_input(input, inWidth, inHeight);
    }
	switch(format) {
		default:
		case CRNGLUE_FORMAT_DXT1:
			compression.m_format = cCRNFmtDXT1;
			break;
		case CRNGLUE_FORMAT_DXT1A:
			compression.m_format = cCRNFmtDXT1;
			compression.m_flags = cCRNCompFlagPerceptual | cCRNCompFlagHierarchical | cCRNCompFlagUseBothBlockTypes | cCRNCompFlagDXT1AForTransparency;
			break;
		case CRNGLUE_FORMAT_DXT3:
			compression.m_format = cCRNFmtDXT3;
			break;
		case CRNGLUE_FORMAT_DXT5:
			compression.m_format = cCRNFmtDXT5;
			break;
		case CRNGLUE_FORMAT_RGTC2:
		    compression.m_format = cCRNFmtDXN_XY;
		    compression.m_flags = 0;
		    break;
		case CRNGLUE_FORMAT_RGTC1_METALLIC:
		case CRNGLUE_FORMAT_RGTC1_ROUGHNESS:
		    compression.m_format = cCRNFmtDXT5A;
		    compression.m_flags = 0;
		    break;
	}
	compression.m_width = inWidth;
	compression.m_height = inHeight;
	compression.m_pImages[0][0] = (const crn_uint32*)rgba;
	if(!highQualitySlow)
		compression.m_dxt_quality = cCRNDXTQualityNormal;
	if(!SetMipmapParameters(mipmaps, mipparams)) {
		crn_uint32 sz;
		*output = (unsigned char*)crn_compress(compression, sz);
		*outputSize = (unsigned int)sz;
	} else {
		crn_uint32 sz;
		*output = (unsigned char*)crn_compress(compression, mipparams, sz);
		*outputSize = (unsigned int)sz;
	}
	free(rgba);
	return (*output != NULL) ? CRNGLUE_OK : CRNGLUE_ERROR;
}

static unsigned char *MakeCopy(void *inptr, int size)
{
    unsigned char *copy = (unsigned char*)malloc(size);
    memcpy((void*)copy, inptr, (size_t)size);
    return copy;
}

CRNEXPORT int CrnGlueGenerateMipmaps(const unsigned char *input, int inWidth, int inHeight, crnglue_mipmaps_t mipmaps, crnglue_mipmap_output_t *output)
{
    crn_mipmap_params mipparams;
    if(!SetMipmapParameters(mipmaps, mipparams)) return CRNGLUE_ERROR;
    unsigned char *rgba = rgba_input(input, inWidth, inHeight);
    crn_comp_params compression = crn_comp_params();
    compression.m_width = inWidth;
	compression.m_height = inHeight;
	compression.m_pImages[0][0] = (const crn_uint32*)rgba;
    //Create work_tex
    crnlib::mipmapped_texture work_tex = crnlib::mipmapped_texture();
    crnlib::image_u8 images[1][1];
    images[0][0].alias((crnlib::color_quad_u8*)compression.m_pImages[0][0], inWidth, inHeight);
    crnlib::face_vec faces(1);
    crnlib::mip_level* pMip = crnlib::crnlib_new<crnlib::mip_level>();
    crnlib::image_u8* pImage = crnlib::crnlib_new<crnlib::image_u8>();
    pImage->swap(images[0][0]);
    pMip->assign(pImage);
    faces[0].push_back(pMip);
    work_tex.assign(faces);
    //Create Mipmaps
    if(!crnlib::create_texture_mipmaps(work_tex, compression, mipparams, true)) {
        free(rgba);
        return CRNGLUE_ERROR;
    }
    free(rgba);
    //Copy output
    output->levelCount = (int)work_tex.get_num_levels();
    output->levels = (crnglue_miplevel_t*)malloc(sizeof(crnglue_miplevel_t) * output->levelCount);
    for(int i = 0; i < output->levelCount; i++) {
        output->levels[i].dataSize = (unsigned int)(work_tex.get_level(0,i)->get_total_pixels() * 4);
        output->levels[i].data = MakeCopy((void*)work_tex.get_level(0, i)->get_image()->get_ptr(), output->levels[i].dataSize);

        output->levels[i].width = (unsigned int)(work_tex.get_level(0,i)->get_width());
        output->levels[i].height = (unsigned int)(work_tex.get_level(0,i)->get_height());
        
        swap_channels(
            output->levels[i].data,
            output->levels[i].width,
            output->levels[i].height
        );
    }
    return CRNGLUE_OK;
}

CRNEXPORT void CrnGlueFreeMipmaps(crnglue_mipmap_output_t *output)
{
    for(int i = 0; i < output->levelCount; i++) {
        free((void*)output->levels[i].data);
    }
    free((void*)output->levels);
}

CRNEXPORT void CrnGlueFreeDDS(void *mem)
{
	crn_free_block(mem);
} 
