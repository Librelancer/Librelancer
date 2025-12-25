// GTAO Bilateral Blur + Composite Fragment Shader
// Applies edge-aware blur using depth for edge detection
// Composites blurred AO with scene color

// AO buffer from GTAO pass (R=AO, A=geometry mask)
Texture2D<float4> AOBuffer : register(t0, TEXTURE_SPACE);

// Depth buffer for edge detection
Texture2D<float> DepthBuffer : register(t1, TEXTURE_SPACE);

// Scene color for final composite
Texture2D<float4> SceneColor : register(t2, TEXTURE_SPACE);

// Samplers
SamplerState Sampler_Point : register(s0, TEXTURE_SPACE);

#include "includes/Camera.hlsl"

// Blur parameters (cbuffer b3, matches BlurParams struct in C#)
cbuffer BlurParams : register(b3, UNIFORM_SPACE)
{
    float2 resolution;       // 0-8: Screen resolution
    float2 invResolution;    // 8-16: 1/resolution
    float depthThreshold;    // 16-20: Edge detection threshold (0 = no blur)
    float _padding1;         // 20-24
    float _padding2;         // 24-28
    float _padding3;         // 28-32
};

struct Input
{
    float2 texCoord : TEXCOORD0;
};

// Gaussian weights for 5-tap blur
static const float weights[5] = { 0.2270270270, 0.1945945946, 0.1216216216, 0.0540540541, 0.0162162162 };
static const float offsets[5] = { 0.0, 1.0, 2.0, 3.0, 4.0 };

// Bilateral weight based on depth difference
float bilateralWeight(float centerDepth, float sampleDepth, float gaussWeight)
{
    float depthDiff = abs(centerDepth - sampleDepth);
    float depthWeight = exp(-depthDiff * depthDiff / (depthThreshold * depthThreshold + 0.001));
    return gaussWeight * depthWeight;
}

float LinearizeDepth(float depth, float near, float far)
{
    return near * far / (far - depth * (far - near));
}

float4 main(Input input) : SV_Target0
{
    float2 uv = input.texCoord;

    // Sample scene color
    float4 sceneColor = SceneColor.Sample(Sampler_Point, uv);

    // Sample center AO and geometry mask
    float4 centerAO = AOBuffer.Sample(Sampler_Point, uv);
    if (centerAO.a < 0.5)
        return sceneColor;

    // If blur disabled (depthThreshold == 0), just composite without blur
    if (depthThreshold < 0.001)
    {
        float ao = centerAO.r;

        // Composite: multiply scene by AO
        float3 result = sceneColor.rgb * ao;
        return float4(result, sceneColor.a);
    }

    // Linearize depth for edge-aware blur
    float near = Projection._34 / Projection._33;
    float far = Projection._34 / (Projection._33 + 1.0);
    float centerDepth = LinearizeDepth(DepthBuffer.Sample(Sampler_Point, uv), near, far);

    // Bilateral blur (separable approximation - blur in both directions simultaneously for simplicity)
    float aoSum = centerAO.r * weights[0];
    float weightSum = weights[0];

    // Horizontal blur
    for (int i = 1; i < 5; i++)
    {
        float2 offset = float2(offsets[i] * invResolution.x, 0.0);

        // Positive direction
        float4 samplePos = AOBuffer.Sample(Sampler_Point, uv + offset);
        float sampleDepthPos = LinearizeDepth(DepthBuffer.Sample(Sampler_Point, uv + offset), near, far);
        float wPos = bilateralWeight(centerDepth, sampleDepthPos, weights[i]);
        aoSum += samplePos.r * wPos;
        weightSum += wPos;

        // Negative direction
        float4 sampleNeg = AOBuffer.Sample(Sampler_Point, uv - offset);
        float sampleDepthNeg = LinearizeDepth(DepthBuffer.Sample(Sampler_Point, uv - offset), near, far);
        float wNeg = bilateralWeight(centerDepth, sampleDepthNeg, weights[i]);
        aoSum += sampleNeg.r * wNeg;
        weightSum += wNeg;
    }

    // Vertical blur
    for (int j = 1; j < 5; j++)
    {
        float2 offset = float2(0.0, offsets[j] * invResolution.y);

        // Positive direction
        float4 samplePos = AOBuffer.Sample(Sampler_Point, uv + offset);
        float sampleDepthPos = LinearizeDepth(DepthBuffer.Sample(Sampler_Point, uv + offset), near, far);
        float wPos = bilateralWeight(centerDepth, sampleDepthPos, weights[j]);
        aoSum += samplePos.r * wPos;
        weightSum += wPos;

        // Negative direction
        float4 sampleNeg = AOBuffer.Sample(Sampler_Point, uv - offset);
        float sampleDepthNeg = LinearizeDepth(DepthBuffer.Sample(Sampler_Point, uv - offset), near, far);
        float wNeg = bilateralWeight(centerDepth, sampleDepthNeg, weights[j]);
        aoSum += sampleNeg.r * wNeg;
        weightSum += wNeg;
    }

    // Normalize
    float ao = aoSum / max(weightSum, 0.001);

    // Clamp to valid range
    ao = saturate(ao);

    // Composite: multiply scene by AO
    float3 result = sceneColor.rgb * ao;

    return float4(result, sceneColor.a);
}
