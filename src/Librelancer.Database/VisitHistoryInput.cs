using LibreLancer.Entities.Character;

namespace LibreLancer.Database;

public record struct VisitHistoryInput(VisitHistoryKind Kind, uint Hash);
