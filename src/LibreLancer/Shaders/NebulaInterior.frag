uniform sampler2D Texture;
uniform vec4 Tint;

in vec2 texcoord;
out vec4 out_frag;
void main(void)
{
	out_frag = texture(Texture, texcoord) * Tint;
}

