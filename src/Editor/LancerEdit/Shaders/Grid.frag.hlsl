cbuffer GridParameters : register(b3, UNIFORM_SPACE)
{
    float4 GridColor;
    float4x4 ViewProjection;
    float Near;
    float Far;
    float Scale;
};

struct Input
{
    float3 nearPoint: TEXCOORD0;
    float3 farPoint: TEXCOORD1;
};

struct Output
{
    float4 color: SV_Target0;
    float depth: SV_Depth;
};

float4 grid(float3 fragPos3D, float scale)
{
    float2 coord = fragPos3D.xz * scale;
    float2 derivative = fwidth(coord);
    float2 grid = abs(frac(coord - 0.5) - 0.5) / derivative;
    float lineF = min(grid.x, grid.y);
    float minimumz = min(derivative.y, 1.0);
    float minimumx = min(derivative.x, 1.0);
    float4 color = float4(GridColor.r, GridColor.g, GridColor.b, 1.0 - min(lineF, 1.0));
    // z axis
    if(fragPos3D.x > (-0.1 / scale) * minimumx && fragPos3D.x < (0.1 / scale) * minimumx)
        color.rgb = float3(0.2, 0.2, 1.0);
    // x axis
    if(fragPos3D.z > (-0.1 / scale) * minimumz && fragPos3D.z < (0.1 / scale) * minimumz)
        color.rgb = float3(1.0, 0.2, 0.2);
    color.a *= GridColor.a;
    return color;
}

float computeDepth(float3 pos)
{
    float4 clipSpace = mul(float4(pos.xyz, 1.0), ViewProjection);
    return clipSpace.z / clipSpace.w;
}

float computeLinearDepth(float3 pos)
{
    float clipSpaceDepth = computeDepth(pos) * 2.0 - 1.0;
    float linearDepth = (2.0 * Near * Far) / (Far + Near - clipSpaceDepth * (Far - Near)); // get linear value between 0.01 and 100
    return linearDepth / Far; // normalize
}

#define DepthRange_near 0.0
#define DepthRange_far 1.0
#define DepthRange_diff (DepthRange_far - DepthRange_near)

Output main(Input input)
{
    float t = -input.nearPoint.y / (input.farPoint.y - input.nearPoint.y);
    float3 fragPos3D = input.nearPoint + t * (input.farPoint - input.nearPoint);

    float depth = ((DepthRange_diff * computeDepth(fragPos3D)) +
                DepthRange_near + DepthRange_far) / 2.0;
    float linearDepth = computeLinearDepth(fragPos3D);
    float fading = max(0.0, (0.5 - linearDepth));

    // adding multiple resolution for the grid
    float4 outColor = (grid(fragPos3D, Scale * 10.0) + grid(fragPos3D, Scale))* float(t > 0.0);
    outColor.a *= fading;

    Output output;
    output.color = outColor;
    output.depth = depth;
    return output;
}
