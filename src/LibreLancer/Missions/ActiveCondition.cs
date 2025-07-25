using System;
using System.Collections.Generic;
using LibreLancer.Missions.Conditions;

namespace LibreLancer.Missions;

public class ActiveCondition
{
    public ActiveTrigger Trigger;
    public ScriptedCondition Condition;
    public ConditionStorage Storage;
}

public abstract class ConditionStorage
{
}

public class ConditionBoolean : ConditionStorage
{
    public bool Value;
}

public class ConditionDouble : ConditionStorage
{
    public double Value;
}

public class ConditionHashSet : ConditionStorage
{
    public HashSet<string> Values = new(StringComparer.OrdinalIgnoreCase);
}
