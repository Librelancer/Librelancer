vec4 blend_overlay(vec4 base, vec4 blend)
{
	vec3 br = clamp(sign(base.rgb - vec3(0.5)), vec3(0.0), vec3(1.0));
    vec3 multiply = 2.0 * base.rgb * blend.rgb;
    vec3 screen = vec3(1.0) - 2.0 * (vec3(1.0) - base.rgb)*(vec3(1.0) - blend.rgb);
    vec3 overlay = mix(multiply, screen, br);

    return vec4(overlay, 1);
}
