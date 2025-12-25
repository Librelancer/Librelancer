// Depth Copy Fragment Shader
// Copies G-Buffer depth to the MSAA target's depth buffer for transparent object occlusion
// Uses SV_Depth to output depth value and SV_Target0 for driver compatibility

Texture2D<float> GBuffer_Depth : register(t0, TEXTURE_SPACE);
SamplerState Sampler_Depth : register(s0, TEXTURE_SPACE);

struct Input
{
    float2 texCoord : TEXCOORD0;
};

struct Output
{
    float4 color : SV_Target0;  // Required by some drivers (color write will be disabled)
    float depth : SV_Depth;     // Actual depth output
};

Output main(Input input)
{
    // Sample depth from G-Buffer
    float depthValue = GBuffer_Depth.Sample(Sampler_Depth, input.texCoord);

    Output output;
    output.color = float4(0, 0, 0, 0);  // Will be masked by ColorWrite=false
    output.depth = depthValue;
    return output;
}
