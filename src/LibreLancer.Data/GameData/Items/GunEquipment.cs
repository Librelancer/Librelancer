// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

namespace LibreLancer.Data.GameData.Items;

public class GunEquipment : Equipment
{
    public required Data.Schema.Equipment.Gun Def;
    public required MunitionEquip Munition;
    public ResolvedFx? FlashEffect;
}
