using System;
using System.Collections.Generic;
using LibreLancer.Entities.Abstract;

namespace LibreLancer.Entities.Character
{
    public class Account : BaseEntity
    {
        // Login identifier
        public Guid AccountIdentifier { get; set; }

        // Last time this account / save was accessed
        public DateTime LastLogin { get; set; }

        // This account has many characters
        public virtual ICollection<Character> Characters { get; set; }
    }
}
