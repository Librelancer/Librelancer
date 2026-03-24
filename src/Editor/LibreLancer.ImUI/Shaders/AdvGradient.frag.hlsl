struct Input
{
    float T : TEXCOORD0;
    float4 Color1 : TEXCOORD1;
    float4 Color2 : TEXCOORD2;
    int Blend: TEXCOORD3;
};

float BlendAuto(float v1, float v2, float x)
{
    return lerp(v1, v2, v1 > v2 ? 1.0f - (1.0f - x) * (1.0f - x) : x * x);
}

float4 main(Input input) : SV_Target0
{
    float t = input.T;
    switch (input.Blend)
    {
        case 2: //Ease-In
            return lerp(input.Color1, input.Color2, t * t);
        case 3: //Ease-Out
            return lerp(input.Color1, input.Color2, 1.0f - (1.0f - t) * (1.0f - t));
        case 4: // Ease smooth
            return lerp(input.Color1, input.Color2,  t * t * (3.0f - 2 * t));
        case 5: // Ease Auto
            return float4(
                BlendAuto(input.Color1.r, input.Color2.r, t),
                BlendAuto(input.Color1.g, input.Color2.g, t),
                BlendAuto(input.Color1.b, input.Color2.b, t),
                BlendAuto(input.Color1.a, input.Color2.a, t)
                );
        default: // Linear (Don't use, use direct geometry instead)
            return lerp(input.Color1, input.Color2, t);
    }
}
