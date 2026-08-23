SamplerState LinearSampler : register(s0, TEXTURE_SPACE);
SamplerState PointSampler : register(s1, TEXTURE_SPACE);
cbuffer Parameters : register(b3, UNIFORM_SPACE)
{
    float4 RenderTargetMetrics;
}

#define SMAA_CUSTOM_SL
#define SMAA_RT_METRICS RenderTargetMetrics

// Interpret Librelancer feature flags as 2 bit number for preset
#if defined(SMAA_PRESET_B)
    #if defined(SMAA_PRESET_A)
        #define SMAA_PRESET_ULTRA
    #else
        #define SMAA_PRESET_HIGH
    #endif
#else
    #if defined(SMAA_PRESET_A)
        #define SMAA_PRESET_MEDIUM
    #else
        #define SMAA_PRESET_LOW
    #endif
#endif

#define SMAATexture2D(tex) Texture2D tex
#define SMAATexturePass2D(tex) tex
#define SMAASampleLevelZero(tex, coord) tex.SampleLevel(LinearSampler, coord, 0)
#define SMAASampleLevelZeroPoint(tex, coord) tex.SampleLevel(PointSampler, coord, 0)
#define SMAASampleLevelZeroOffset(tex, coord, offset) tex.SampleLevel(LinearSampler, coord, 0, offset)
#define SMAASample(tex, coord) tex.Sample(LinearSampler, coord)
#define SMAASamplePoint(tex, coord) tex.Sample(PointSampler, coord)
#define SMAASampleOffset(tex, coord, offset) tex.Sample(LinearSampler, coord, offset)
#define SMAA_FLATTEN [flatten]
#define SMAA_BRANCH [branch]
#define SMAATexture2DMS2(tex) Texture2DMS<float4, 2> tex
#define SMAALoad(tex, pos, sample) tex.Load(pos, sample)
#define SMAAGather(tex, coord) tex.Gather(LinearSampler, coord, 0)
#include "SMAA.hlsl"
