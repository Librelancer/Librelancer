// MIT License - Copyright (c) Lazrius
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

namespace LibreLancer.Entities.Character
{
    using LibreLancer.Entities.Abstract;

    public class EquipmentEntity : BaseEntity
    {
        public string EquipmentNickname { get; set; }
        public string EquipmentHardpoint { get; set; }
    }
}