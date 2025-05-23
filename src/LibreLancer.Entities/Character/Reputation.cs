// MIT License - Copyright (c) Lazrius
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package
using LibreLancer.Entities.Abstract;

namespace LibreLancer.Entities.Character
{
    public class Reputation : BaseEntity
    {
        public long CharacterId { get; set; }

        public Character Character { get; set; } = null!;
        public float ReputationValue { get; set; }
        public string RepGroup { get; set; }
    }
}
