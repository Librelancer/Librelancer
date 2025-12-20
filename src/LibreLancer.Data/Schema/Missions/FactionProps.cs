using System.Collections.Generic;
using LibreLancer.Data.Ini;

namespace LibreLancer.Data.Schema.Missions
{

    [ParsedSection]
    public partial class FactionProps
    {
        [Entry("affiliation", Required = true)]
        public string Affiliation;
        [Entry("legality")] public Legality Legality;
        [Entry("nickname_plurality")] public NicknamePlurality NicknamePlurality;
        [Entry("msg_id_prefix")] public string MsgIdPrefix;
        [Entry("jump_preference")] public JumpPreference JumpPreference;
        [Entry("npc_ship", Multiline = true)] public List<string> NpcShip = new List<string>();
        [Entry("voice", Multiline = true)] public List<string> Voice = new List<string>();
        [Entry("firstname_male")] public ValueRange<int>? FirstNameMale;
        [Entry("firstname_female")] public ValueRange<int>? FirstNameFemale;
        [Entry("lastname")] public ValueRange<int> LastName;
        [Entry("formation_desig")] public ValueRange<int> FormationDesig;
        [Entry("large_ship_desig")] public int LargeShipDesig;
        [Entry("large_ship_names")] public ValueRange<int> LargeShipNames;
        [Entry("scan_announce")] public bool ScanAnnounce;
        [Entry("scan_chance")] public float ScanChance;
        [Entry("mc_costume")] public string McCostume;
        [Entry("rank_desig")] public int[] RankDesig; //Unknown

        public List<SpaceCostume> SpaceCostume = new List<SpaceCostume>();
        public List<ScanForCargo> ScanForCargo = new List<ScanForCargo>();
        public List<FormationKind> Formation = new List<FormationKind>();

        [EntryHandler("scan_for_cargo", MinComponents = 2, Multiline = true)]
        void HandleScanForCargo(Entry e) => ScanForCargo.Add(new ScanForCargo(e));

        [EntryHandler("formation", MinComponents = 2, Multiline = true)]
        void HandleFormation(Entry e) => Formation.Add(new FormationKind(e));

        [EntryHandler("space_costume", MinComponents = 3, Multiline = true)]
        void HandleSpaceCostume(Entry e) => SpaceCostume.Add(new SpaceCostume(e));
    }

    public struct ScanForCargo
    {
        public string Cargo;
        public int Param;

        public ScanForCargo(Entry e)
        {
            Cargo = e[0].ToString();
            Param = e[1].ToInt32();
        }
    }

    public struct SpaceCostume
    {
        public string Head;
        public string Body;
        public string Extra; //comm_br_brighton ?

        public SpaceCostume(Entry e)
        {
            Head = e[0].ToString();
            Body = e[1].ToString();
            Extra = e[2].ToString();
        }
    }

    public struct FormationKind
    {
        public string Kind;
        public string Formation;

        public FormationKind(Entry e)
        {
            Kind = e[0].ToString();
            Formation = e[1].ToString();
        }
    }
}
