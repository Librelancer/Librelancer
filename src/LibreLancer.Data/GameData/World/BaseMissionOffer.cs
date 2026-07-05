// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

namespace LibreLancer.Data.GameData.World;

public enum RandomMissionType
{
    DestroyMission
}

public class BaseMissionOffer
{
    public RandomMissionType MissionType;
    public float MinDiff;
    public float MaxDiff;
    public float Weight;
}
