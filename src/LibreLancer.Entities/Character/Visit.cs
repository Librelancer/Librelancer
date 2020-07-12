namespace LibreLancer.Entities.Character
{
    using LibreLancer.Entities.Abstract;
    using LibreLancer.Entities.Enums;

    public class VisitEntry : BaseEntity
    {
        public Visit VisitValue { get; set; }
        public string SolarNickname { get; set; }
    }
}