// MIT License - Copyright (c) Lazrius
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

namespace LibreLancer.Entities.Character
{
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations.Schema;

    using LibreLancer.Entities.Abstract;

    public class Character : BaseEntity
    {
        public Character()
        {
            this.Reputations = new HashSet<Reputation>();
            this.VisitEntries = new HashSet<VisitEntry>();
            this.Items = new HashSet<CargoItem>();
        }

        public string Name { get; set; }

        // Is this an admin character?

        public bool IsAdmin { get; set; }

        public uint Rank { get; set; }

        public long Money { get; set; }

        // voice = trent_voice
        public string Voice { get; set; }

        // costume = trent (what you see on bases)
        public string Costume { get; set; }

        // com_costume = trent (what you see when you hail an NPC)
        public string ComCostume { get; set; }

        // Current system if in space / base system is in otherwise
        public string System { get; set; }

        // Current base if docked
        public string Base { get; set; }

        // Current position of ship
        public float X { get; set; }
        public float Y { get; set; }
        public float Z { get; set; }

        // Current rotation of ship
        public float RotationX { get; set; }
        public float RotationY { get; set; }
        public float RotationZ { get; set; }
        public float RotationW { get; set; }

        // Current ship archetype
        public string Ship { get; set; }

        // Current faction affiliation
        public string Affiliation { get; set; }

        // If locked cannot faction affiliation cannot change
        public bool AffiliationLocked { get; set; }

        // Description of the character set
        public string Description { get; set; }

        // The amount of fighters killed
        public long FightersKilled { get; set; }

        // The amount of Freighters/Transports killed
        public long TransportsKilled { get; set; }

        // The amount of capital ships killed
        public long CapitalKills { get; set; }

        // The amount of players
        public long PlayersKilled { get; set; }

        // The amount of Missions completed
        public long MissionsCompleted { get; set; }

        // The amount of Missions Failed
        public long MissionsFailed { get; set; }

        // In vanilla, house = float, rep_group
        // Should be a subtable with that information inside of it. Playername would be the primary key.
        public virtual ICollection<Reputation> Reputations { get; set; }

        // In vanilla, visit = hash of solar nickname, visit value. Vanilla visit values can be found here: https://the-starport.net/freelancer/forum/viewtopic.php?post_id=34251#forumpost34251
        // Should be a subtable with that information inside of it. Playername would be the primary key.
        public virtual ICollection<VisitEntry> VisitEntries { get; set; }

        public virtual ICollection<CargoItem> Items { get; set; }

        public long AccountId { get; set; }

        [ForeignKey("AccountId")]
        // This Character has one account
        public virtual Account Account { get; set; }
    }
}
