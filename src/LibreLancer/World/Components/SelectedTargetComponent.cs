namespace LibreLancer.World.Components;

public class SelectedTargetComponent : GameComponent
{
    private GameObject? selected;

    public GameObject? Selected
    {
        get => selected;
        set
        {
            if (!ReferenceEquals(selected, value))
            {
                SelectedPart = null;
            }
            selected = value;
        }
    }

    public uint? SelectedPart;

    public SelectedTargetComponent(GameObject parent) : base(parent) { }
}
