using System.Collections.Generic;

namespace LibreLancer.Data.GameData;

public class Costume : IdentifiableItem
{
    public Bodypart? Head;
    public Bodypart Body;
    public Bodypart? LeftHand;
    public Bodypart? RightHand;
    public Accessory? Accessory;
}
