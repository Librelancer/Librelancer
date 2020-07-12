namespace LibreLancer.Entities.Character
{
    using LibreLancer.Entities.Abstract;

    public class Reputation : BaseEntity
    {
        public float ReputationValue { get; set; }
        public string RepGroup { get; set; }
    }
}
