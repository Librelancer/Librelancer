// MIT License - Copyright (c) Lazrius
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

namespace LibreLancer.Entities.Character
{
    using LibreLancer.Entities.Abstract;
    using LibreLancer.Entities.Enums;

    public class VisitEntry : BaseEntity
    {
        public long CharacterId { get; set; }
        public Character Character { get; set; } = null!;

        public Visit VisitValue { get; set; }
        public uint Hash { get; set; }
    }
}
