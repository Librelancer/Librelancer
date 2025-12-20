using System;
using LibreLancer;
using LibreLancer.Data.GameData;
using LibreLancer.Data.GameData.World;

namespace LancerEdit.GameContent;

public class SystemEditData
{

    private StarSystem sys;

    public string Nickname => sys.Nickname;

    public SystemEditData(StarSystem sys)
    {
        this.sys = sys;
        this.SpaceColor = sys.BackgroundColor;
        this.Ambient = sys.AmbientColor;
        this.MusicSpace = sys.MusicSpace;
        this.MusicBattle = sys.MusicBattle;
        this.MusicDanger = sys.MusicDanger;
        this.StarsBasic = sys.StarsBasic;
        this.StarsComplex = sys.StarsComplex;
        this.StarsNebula = sys.StarsNebula;
        this.IdsName = sys.IdsName;
        this.IdsInfo = sys.IdsInfo;
        this.NavMapScale = sys.NavMapScale;
    }

    //Universe
    public int IdsName;
    public int IdsInfo;
    //System Ini
    public float NavMapScale;
    public Color4 SpaceColor;
    public Color3f Ambient;
    public string MusicSpace;
    public string MusicBattle;
    public string MusicDanger;
    public ResolvedModel StarsBasic;
    public ResolvedModel StarsComplex;
    public ResolvedModel StarsNebula;

    static bool ModelsEqual(ResolvedModel a, ResolvedModel b)
    {
        if (a == null && b != null) return false;
        if (b == null && a != null) return false;
        if (a == b) return true;
        return a.ModelFile.Equals(b.ModelFile, StringComparison.OrdinalIgnoreCase);
    }

    public bool IsDirty() =>
        IdsName != sys.IdsName ||
        IdsInfo != sys.IdsInfo ||
        SpaceColor != sys.BackgroundColor ||
        Ambient != sys.AmbientColor ||
        MusicSpace != sys.MusicSpace ||
        MusicBattle != sys.MusicBattle ||
        MusicDanger != sys.MusicDanger ||
        NavMapScale != sys.NavMapScale ||
        !ModelsEqual(StarsBasic, sys.StarsBasic) ||
        !ModelsEqual(StarsComplex, sys.StarsComplex) ||
        !ModelsEqual(StarsNebula, sys.StarsNebula);

    public bool IsUniverseDirty() =>
        IdsName != sys.IdsName ||
        IdsInfo != sys.IdsInfo;


    public void Apply()
    {
        sys.IdsName = IdsName;
        sys.IdsInfo = IdsInfo;
        sys.BackgroundColor = SpaceColor;
        sys.AmbientColor = Ambient;
        sys.MusicSpace = MusicSpace;
        sys.MusicBattle = MusicBattle;
        sys.MusicDanger = MusicDanger;
        sys.StarsBasic = StarsBasic;
        sys.StarsComplex = StarsComplex;
        sys.StarsNebula = StarsNebula;
        sys.NavMapScale = NavMapScale;
    }
}
