struct Input
{
    float4 color: TEXCOORD0;
};

float4 main(Input input) : SV_Target0
{
    return input.color;
}
