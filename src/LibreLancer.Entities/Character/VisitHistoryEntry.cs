using LibreLancer.Entities.Abstract;

namespace LibreLancer.Entities.Character;

public class VisitHistoryEntry : BaseEntity
{
    public long CharacterId { get; set; }

    public Character Character { get; set; } = null!;

    public VisitHistoryKind Kind { get; set; }

    public uint Hash { get; set; }
}

public enum VisitHistoryKind
{
    Invalid = 0,
    Base = 1,
    System = 2,
    Jumphole = 3
}
