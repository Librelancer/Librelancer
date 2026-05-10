namespace LibreLancer.Graphics;

public sealed class TextureSlots
{
    private Texture?[] slots = new Texture[8];
    private RenderContext rc;
    internal TextureSlots(RenderContext rc)
    {
        this.rc = rc;
    }

    public Texture? this[int index]
    {
        get => slots[index];
        set
        {
            slots[index] = value;
            rc.Backend.SetTextureSlot(index, value);
        }
    }
}
