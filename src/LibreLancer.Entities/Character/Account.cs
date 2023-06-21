// MIT License - Copyright (c) Lazrius
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

namespace LibreLancer.Entities.Character
{
    using System;
    using System.Collections.Generic;
    using LibreLancer.Entities.Abstract;

    public class Account : BaseEntity
    {
        public Account()
        {
            this.Characters = new HashSet<Character>();
        }

        // Login identifier
        public Guid AccountIdentifier { get; set; }

        // Last time this account / save was accessed
        public DateTime LastLogin { get; set; }
        
        // Ban expiry, null if not banned
        public DateTime? BanExpiry { get; set; }

        // This account has many characters
        public virtual ICollection<Character> Characters { get; set; }
    }
}
