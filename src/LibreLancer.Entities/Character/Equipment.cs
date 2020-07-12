using LibreLancer.Entities.Enums;

namespace LibreLancer.Entities.Character
{
    using LibreLancer.Entities.Abstract;

    public class EquipmentEntity : BaseEntity
    {
        public string EquipmentNickname { get; set; }
        public string EquipmentHardpoint { get; set; }
    }
}