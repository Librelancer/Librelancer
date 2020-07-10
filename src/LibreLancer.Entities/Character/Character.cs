using System.ComponentModel.DataAnnotations.Schema;

namespace LibreLancer.Entities.Character
{
    using System.Collections.Generic;

    using LibreLancer.Entities.Abstract;

    public class Character : BaseEntity
    {
        public string Name { get; set; }

        public uint Rank { get; set; }

        public decimal Money { get; set; }

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

        // Current ship archetype
        public string Ship { get; set; }

        // Description of the character set
        public string Description { get; set; }

        // The amount of fighters killed
        public ulong FightersKilled { get; set; }

        // The amount of Freighters/Transports killed
        public ulong TransportsKilled { get; set; }

        // The amount of capital ships killed
        public ulong CapitalKills { get; set; }

        // The amount of players
        public ulong PlayersKilled { get; set; }

        // The amount of Missions completed
        public ulong MissionsCompleted { get; set; }

        // The amount of Missions Failed
        public ulong MissionsFailed { get; set; }

        // In vanilla, house = float, rep_group
        // Should be a subtable with that information inside of it. Playername would be the primary key.
        public virtual ICollection<Reputation> Reputations { get; set; }

        // In vanilla, visit = hash of solar nickname, visit value. Vanilla visit values can be found here: https://the-starport.net/freelancer/forum/viewtopic.php?post_id=34251#forumpost34251
        // Should be a subtable with that information inside of it. Playername would be the primary key.
        public virtual ICollection<VisitEntry> VisitEntries { get; set; }

        public virtual ICollection<EquipmentEntity> Equipment { get; set; }

        public virtual ICollection<CargoItem> Cargo { get; set; }

        public ulong AccountId { get; set; }

        [ForeignKey("AccountId")]
        // This Character has one account
        public virtual Account Account { get; set; }
    }
}
