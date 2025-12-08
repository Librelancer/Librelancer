namespace LibreLancer.Missions.Events;

public record struct PlayerLaunchedEvent();

public record struct BaseEnteredEvent(string Base);
public record struct SpaceEnteredEvent();
public record struct SpaceExitedEvent();
public record struct TLEnteredEvent(string Ship, string StartRing, string NextRing);

public record struct TLExitedEvent(string Ship, string Ring);

public record struct ProjectileHitEvent(string Target, string Source);

public record struct RTCDoneEvent(string RTC);

public record struct DestroyedEvent(string Object);

public record struct MissionResponseEvent(bool Accept);

public record struct SystemEnteredEvent(string System, string Ship);

public record struct CommCompleteEvent(string Comm);

public record struct ClosePopupEvent(string Button);

public record struct CharSelectEvent(string Character, string Room, string Base);

public record struct LocationEnteredEvent(string Room, string Base);

public record struct PlayerManeuverEvent(ManeuverType Type, string Target);

public record struct LaunchCompleteEvent(string Ship);

public record struct CargoScannedEvent(string ScanningShip, string ScannedShip);

public record struct LootAcquiredEvent(string LootNickname, string AcquirerShip);
