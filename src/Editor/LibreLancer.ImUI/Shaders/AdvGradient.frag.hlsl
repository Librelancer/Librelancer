struct Input
{
    float T : TEXCOORD0;
    float4 Color1 : TEXCOORD1;
    float4 Color2 : TEXCOORD2;
    int Blend: TEXCOORD3;
};

float4 main(Input input) : SV_Target0
{
    float t = input.T;
    switch (input.Blend)
    {
        case 2: //Ease-In
            return lerp(input.Color1, input.Color2, pow(t, 1.685));
        case 3: //Ease-Out
            return lerp(input.Color1, input.Color2, 1.0 - pow(1.0 - t, 1.685));
        case 4: //Ease-In-Out
            return lerp(input.Color1, input.Color2,  t < 0.5f ? 4 * t * t * t : (t - 1) * (2 * t - 2) * (2 * t - 2) + 1);
        case 5: // Step (Don't use, use direct geometry instead)
            return input.Color1;
        default: // Linear (Don't use, use direct geometry instead)
            return lerp(input.Color1, input.Color2, t);
    }
}
