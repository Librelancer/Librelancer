using System.Collections.Generic;
using LibreLancer.Data.GameData;
using LibreLancer.Utf.Dfm;
using WattleScript.Interpreter;
using Script = LibreLancer.Utf.Anm.Script;

namespace LibreLancer.Interface;

[WattleScriptUserData]
public class CommAppearance
{
    [WattleScriptHidden]
    public DfmFile Head;
    [WattleScriptHidden]
    public DfmFile Body;
    [WattleScriptHidden]
    public Accessory Accessory;
    [WattleScriptHidden]
    public RigidModel AccessoryModel;
    [WattleScriptHidden]
    public List<Script> Scripts = new List<Script>();
    [WattleScriptHidden]
    public bool Male = true;
}
