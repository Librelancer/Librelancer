using System.Numerics;
using LibreLancer.Data;
using LibreLancer.Data.GameData.Items;
using LibreLancer.Data.Schema.Missions;

namespace LibreLancer.Missions;

public class ScriptLoot : NicknameItem
{
    public Equipment Archetype;
    public int StringId;
    public Vector3 Position;
    public string RelPosObj;
    public Vector3 RelPosOffset;
    public Vector3 Velocity;
    public int EquipAmount;
    public float Health;
    public bool CanJettison;

    public static ScriptLoot FromIni(MissionLoot src, GameItemDb db) => new()
    {
        Nickname = src.Nickname,
        Archetype = db.Equipment.Get(src.Archetype),
        StringId = src.StringId,
        Position = src.Position,
        RelPosObj = src.RelPosObj,
        RelPosOffset = src.RelPosOffset,
        Velocity = src.Velocity,
        EquipAmount = src.EquipAmount,
        Health = src.Health,
        CanJettison = src.CanJettison,
    };
}
