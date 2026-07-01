using System.Numerics;

namespace LibreLancer.World.Components;

public class ShipControlAccessComponent(GameObject parent) : GameComponent(parent)
{
    public Vector3 Steering;
    public StrafeControls CurrentStrafe = StrafeControls.None;
    public virtual bool CruiseEnabled { get; set; }
    public EngineStates EngineState { get; protected set; }
    // from 0 to 1
    // TODO: I forget how this is configured in .ini files. Constants.ini?
    // Some mods have a per-ship (engine?) cruise speed. Check how this is implemented, and include as native feature.
    public float EnginePower;

    public virtual void SetEngineState(EngineStates es) => EngineState = es;
}
