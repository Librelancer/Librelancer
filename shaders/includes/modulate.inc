vec4 modulate2x(vec4 a, vec4 b)
{
    return vec4(2. * a.rgb * b.rgb, a.a * b.a);
}
