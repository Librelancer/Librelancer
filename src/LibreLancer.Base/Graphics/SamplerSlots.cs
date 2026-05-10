namespace LibreLancer.Graphics;

public sealed class SamplerSlots
{
    private SamplerState[] slots = new SamplerState[8];
    private RenderContext rc;
    internal SamplerSlots(RenderContext rc)
    {
        this.rc = rc;
        for (int i = 0; i < slots.Length; i++)
            slots[i] = SamplerState.LinearRepeat;
    }

    public SamplerState this[int index]
    {
        get => slots[index];
        set
        {
            slots[index] = value;
            rc.Backend.SetSamplerState(index, value);
        }
    }
}
