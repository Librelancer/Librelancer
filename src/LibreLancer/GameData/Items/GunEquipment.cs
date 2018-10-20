// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and confiditons defined in
// LICENSE, which is part of this source code package

using System;
namespace LibreLancer.GameData.Items
{
    public class GunEquipment : Equipment
    {
        public float TurnRateRadians;
        public float RefireDelay;
        public IDrawable Model;
      
        public override IDrawable GetDrawable()
        {
            return Model;
        }
    }
}
