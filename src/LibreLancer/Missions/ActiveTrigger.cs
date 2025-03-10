using System.Collections.Generic;

namespace LibreLancer.Missions;

public class ActiveTrigger
{
    public ScriptedTrigger Trigger;
    public bool Deactivated;
    public double ActiveTime;
    public List<ActiveCondition> Conditions = new();
    public BitArray128 Satisfied; // Debug
}
