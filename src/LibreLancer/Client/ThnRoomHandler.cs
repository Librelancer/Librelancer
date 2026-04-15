using LibreLancer.Data.GameData.World;
using LibreLancer.Resources;
using LibreLancer.Thn;

namespace LibreLancer.Client;

public static class ThnRoomHandler
{
    public static ThnScriptContext CreateContext(Base currentBase, BaseRoom currentRoom)
    {
        var ctx = new ThnScriptContext(currentRoom.OpenSet());
        if (currentBase.TerrainTiny != null)
        {
            ctx.Substitutions.Add("$terrain_tiny", currentBase.TerrainTiny);
        }

        if (currentBase.TerrainSml != null)
        {
            ctx.Substitutions.Add("$terrain_sml", currentBase.TerrainSml);
        }

        if (currentBase.TerrainMdm != null)
        {
            ctx.Substitutions.Add("$terrain_mdm", currentBase.TerrainMdm);
        }

        if (currentBase.TerrainLrg != null)
        {
            ctx.Substitutions.Add("$terrain_lrg", currentBase.TerrainLrg);
        }

        if (currentBase.TerrainDyna1 != null)
        {
            ctx.Substitutions.Add("$terrain_dyna_01", currentBase.TerrainDyna1);
        }

        if (currentBase.TerrainDyna2 != null)
        {
            ctx.Substitutions.Add("$terrain_dyna_02", currentBase.TerrainDyna2);
        }
        return ctx;
    }

}
