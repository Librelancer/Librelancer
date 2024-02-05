using System.Collections.Generic;
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
    public List<Script> Scripts = new List<Script>();
}
