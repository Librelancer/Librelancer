using LibreLancer.Data.GameData;
using LibreLancer.Data.GameData.World;

namespace LancerEdit.GameContent;

public class SysAddEncounterParameter : EditorAction
{
    private StarSystem system;
    private EncounterParameters parameter;

    public SysAddEncounterParameter(StarSystem system, EncounterParameters parameter)
    {
        this.system = system;
        this.parameter = parameter;
    }

    public override void Commit()
    {
        system.EncounterParameters.Add(parameter);
    }

    public override void Undo()
    {
        system.EncounterParameters.Remove(parameter);
    }
}
