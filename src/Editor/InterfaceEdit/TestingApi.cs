// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using LibreLancer;
using LibreLancer.Interface;
using System.Collections.Generic;
using System.Numerics;
using LibreLancer.Client;
using LibreLancer.Infocards;
using LibreLancer.Net;
using LibreLancer.Net.Protocol;
using WattleScript.Interpreter;

namespace InterfaceEdit
{
    public class TestServerList : ITableData
    {
        public int Count => 5;
        public int Selected { get; set; } = -1;
        public string GetContentString(int row, string column)
        {
            return "A";
        }

        public string CurrentDescription()
        {
            if (Selected < 0) return "";
            return "Server Description";
        }

        public bool ValidSelection()
        {
            return Selected > -1;
        }
    }

    public class TestSaveGameList : ITableData
    {
        public int Count => 5;
        public int Selected { get; set; } = -1;

        static string FlTime(DateTime time)
        {
            return $"{time.ToShortDateString()} {time:HH:mm}";
        }
        public string GetContentString(int row, string column)
        {
            if (column == "name")
                return ((char) ('a' + row)).ToString();
            else if (column == "date")
                return FlTime(new DateTime(2019, 7, 3 + row, 12, 05, 10 + row));
            else
                return "n/a";
        }

        public string CurrentDescription()
        {
            if (Selected < 0) return "";
            return ((char) ('a' + Selected)).ToString();
        }

        public bool ValidSelection()
        {
            return Selected > -1;
        }
    }
    public class TestCharacterList : ITableData
    {
        public int Count => 5;
        public int Selected { get; set; } = -1;
        public string GetContentString(int row, string column)
        {
            return "A";
        }

        public string ServerName = "Server";
        public string ServerDescription = "DESCRIPTION";
        public string ServerNews = "Placeholder News";

        public bool ValidSelection()
        {
            return Selected > -1;
        }
    }

    [WattleScriptUserData]
    public class FakeKeyMap : ITableData
    {
        public int Count => 3;
        public int Selected { get; set; } = -1;
        public string GetContentString(int row, string column)
        {
            switch (column)
            {
                case "key": return "Key";
                case "primary": return "Prim";
                case "secondary": return "Sec";
            }
            return "";
        }

        public void SetGroup(int group)
        {
            //No-op
        }

        public void CaptureInput(int index, bool primary, Closure onFinish)
        {
            //No-op
        }

        public void DefaultBindings()
        {
        }

        public void ResetBindings()
        {
        }

        public void Save()
        {
        }

        public void CancelCapture()
        {
        }

        public void ClearCapture()
        {
        }

        public int GetKeyId(int row) => 1109;


        public bool ValidSelection()
        {
            return Selected > -1;
        }
    }

    [WattleScriptUserData]
    public class FakeContactList : IContactListData
    {
        string[] contacts =
        {
            "Contact01", "Contact02", "Contact03", "Contact04",
            "Contact05", "Contact06", "Contact07", "Contact08",
            "Contact09", "Contact10", "Contact11", "Contact12",
            "Contact13", "Contact14", "Contact15", "Contact16",
            "Contact17", "Contact18", "Contact19", "Contact20"
        };

        private int selIndex = -1;
        public int Count => contacts.Length;

        [WattleScriptHidden]
        public int SelectedIndex => selIndex;

        public bool IsSelected(int index) => selIndex == index;

        public void SelectIndex(int index) => selIndex = index;

        public string Get(int index) => contacts[index];

        public RepAttitude GetAttitude(int index) => RepAttitude.Friendly;

        public bool IsWaypoint(int index) => index == 2;

        public void SetFilter(string filter)
        {
            //no-op
        }
    }


    public class TestingApi
    {
        static DateTime startTime = DateTime.UtcNow;
        static TestingApi()
        {
            LuaContext.RegisterType<TestingApi>();
            LuaContext.RegisterType<TestServerList>();
            LuaContext.RegisterType<TestCharacterList>();
            LuaContext.RegisterType<TestSaveGameList>();
            LuaContext.RegisterType<FakeShipDealer>();
            LuaContext.RegisterType<FakeKeyMap>();
            LuaContext.RegisterType<FakeContactList>();
        }

        static readonly NavbarButtonInfo cityscape = new NavbarButtonInfo("IDS_HOTSPOT_EXIT", "Cityscape");
        static readonly NavbarButtonInfo bar = new NavbarButtonInfo("IDS_HOTSPOT_BAR", "Bar");
        static readonly NavbarButtonInfo trader = new NavbarButtonInfo("IDS_HOTSPOT_COMMODITYTRADER_ROOM", "Trader");
        static readonly NavbarButtonInfo equip = new NavbarButtonInfo("IDS_HOTSPOT_EQUIPMENTDEALER_ROOM", "Equipment");
        static readonly NavbarButtonInfo shipDealer = new NavbarButtonInfo("IDS_HOTSPOT_SHIPDEALER_ROOM", "ShipDealer");

        public int CurrentRank = 1;
        public double NetWorth = 93884;
        public double NextLevelWorth = 0;
        public double CharacterPlayTime => 3600 + (DateTime.UtcNow - startTime).TotalSeconds;

        public PlayerStats Statistics = new()
        {
            BasesVisited = 3,
            SystemsVisited = 2,
            JumpHolesFound = 1,
            TotalMissions = 4,
            TotalKills = 37,
            FreightersKilled = 32,
            FightersKilled =  2,
            BattleshipsKilled = 1,
            TransportsKilled = 2
        };

        public bool HasBar = true;
        public bool HasTrader = true;
        public bool HasEquip = true;
        public bool HasShipDealer = true;

        public bool HasLaunchAction = true;
        public bool HasRepairAction = false;
        public bool HasMissionVendor = false;
        public bool HasNewsAction = false;
        public bool HasCommodityTraderAction = false;
        public bool HasEquipmentDealerAction = false;
        public bool HasShipDealerAction = false;

        public bool AutoSaveLoadEnabled = false;

        public int ActiveHotspotIndex = 0;

        TestServerList serverList = new TestServerList();
        public TestServerList ServerList() => serverList;

        private TestSaveGameList _testSaveGames = new TestSaveGameList();

        public TestSaveGameList SaveGames() => _testSaveGames;

        private GameSettings settings = new GameSettings();

        public GameSettings GetCurrentSettings() => settings.MakeCopy();

        private FakeContactList contacts = new();

        public FakeContactList GetContactList() => contacts;

        public UiNewCharacter[] GetNewCharacters()
        {
            return new UiNewCharacter[]
            {
                new()
                {
                    StridName = 11051,
                    StridDesc = 11551,
                    Money = 2000,
                    ShipModel = @"DATA\ships\civilian\cv_starflier\cv_starflier.cmp",
                    ShipName = "TestShip",
                    Location = "New York"
                },
                new()
                {
                    StridName = 1249,
                    StridDesc = 1286,
                    Money = 2000,
                    ShipModel = @"DATA\ships\rheinland\rh_elite\rh_elite.cmp",
                    ShipName = @"TestShip2",
                    Location = "New Berlin"
                },
            };
        }

        public void ApplySettings(GameSettings settings)
        {
            this.settings = settings;
        }

        public void LoadSelectedGame()
        {
        }

        public void Resume()
        {
        }

        public void QuitToMenu()
        {
        }

        public void DeleteSelectedGame()
        {
        }

        public bool CanLoadAutoSave() => AutoSaveLoadEnabled;

        public void LoadAutoSave()
        {
        }


        public Infocard _Infocard;

        public Infocard CurrentInfocard() => _Infocard;
        public string CurrentInfoString() => "CURRENT INFORMATION (test)";

        private DisplayFaction[] relations = new DisplayFaction[]
        {
            new DisplayFaction(196851, -0.9f),
            new DisplayFaction(196847, -0.6f),
            new DisplayFaction(196848, -0.5f),
            new DisplayFaction(196850, -0.45f),
            new DisplayFaction(196885, -0.3f),
            new DisplayFaction(196878, -0.2f),
            new DisplayFaction(196888, 0f),
            new DisplayFaction(196874, 0.1f),
            new DisplayFaction(196875, 0.25f),
            new DisplayFaction(196889, 0.3f),
            new DisplayFaction(196884, 0.45f),
            new DisplayFaction(196890, 0.6f),
            new DisplayFaction(196881, 0.75f),
            new DisplayFaction(196887, 0.9f)
        };

        public DisplayFaction[] GetPlayerRelations() => relations;

        public void ConnectSelection()
        {
        }

        public void StartNetworking()
        {
        }

        public UiEquippedWeapon[] GetWeapons()
        {
            return new UiEquippedWeapon[]
            {
                new(true, 263357),
                new(true, 263357),
                new(true, 263357),
                new(true, 263357),
                new(true, 263370),
                new(true, 263370),
                new(true, 263161),
                new(false, 263172),
                new(false, 263754)
            };
        }

        public bool ConnectAddress(string address)
        {
            return false;
        }

        public void StopNetworking()
        {
        }

        public int CruiseCharge() => 25;

        public void PopulateNavmap(Navmap nav)
        {
        }

        public NavbarButtonInfo[] GetNavbarButtons()
        {
            var l = new List<NavbarButtonInfo>();
            l.Add(cityscape);
            if (HasBar) l.Add(bar);
            if (HasTrader) l.Add(trader);
            if (HasEquip) l.Add(equip);
            if (HasShipDealer) l.Add(shipDealer);
            return l.ToArray();
        }

        private NewsArticle[] articles = new[]
        {
            new NewsArticle()
                {Icon = "critical", Logo = "news_scene2", Headline = 15001, Text = 15002},
            new NewsArticle()
                {Icon = "world", Logo = "news_schultzsky", Headline = 15003, Text = 15004},
            new NewsArticle()
                {Icon = "world", Logo = "news_manhattan", Headline = 15009, Text = 15010},
            new NewsArticle()
                {Icon = "system", Logo = "news_cambridge", Headline = 56152, Text = 56153},
            new NewsArticle() {Icon = "world", Logo = "news_leeds", Headline = 56162, Text = 56163},
            new NewsArticle() {Icon = "system", Logo = "news_leeds", Headline = 56166, Text = 56167},
            new NewsArticle()
                {Icon = "world", Logo = "news_newtokyo", Headline = 56180, Text = 56181},
        };

        public NewsArticle[] GetNewsArticles() => articles;

        public NavbarButtonInfo[] GetActionButtons()
        {
            var l = new List<NavbarButtonInfo>();
            if (HasLaunchAction) l.Add(new NavbarButtonInfo("Launch", "IDS_HOTSPOT_LAUNCH"));
            if (HasRepairAction) l.Add(new NavbarButtonInfo("Repair", "IDS_NN_REPAIR_YOUR_SHIP"));
            if (HasMissionVendor) l.Add(new NavbarButtonInfo("MissionVendor", "IDS_HOTSPOT_MISSIONVENDOR"));
            if (HasNewsAction) l.Add(new NavbarButtonInfo("NewsVendor", "IDS_HOTSPOT_NEWSVENDOR"));
            if (HasCommodityTraderAction) l.Add(new NavbarButtonInfo("CommodityTrader", "IDS_HOTSPOT_COMMODITYTRADER"));
            if (HasEquipmentDealerAction) l.Add(new NavbarButtonInfo("EquipmentDealer", "IDS_HOTSPOT_EQUIPMENTDEALER"));
            if (HasShipDealerAction) l.Add(new NavbarButtonInfo("ShipDealer", "IDS_HOTSPOT_SHIPDEALER"));
            return l.ToArray();
        }

        public Maneuver[] ManeuverData;
        public Maneuver[] GetManeuvers() => ManeuverData;
        public string GetActiveManeuver() => "FreeFlight";

        private ChatSource chats = new ChatSource();

        public ChatSource GetChats() => chats;

        public double GetCredits() => 10000;

        public int GetObjectiveStrid() => 21825;

        public LuaCompatibleDictionary GetManeuversEnabled()
        {
            var dict = new LuaCompatibleDictionary();
            dict.Set("FreeFlight", true);
            dict.Set("Goto", true);
            dict.Set("Dock", true);
            dict.Set("Formation", false);
            return dict;
        }

        public bool HasShip() => true;

        public float GetPlayerHealth() => 0.75f;
        public float GetPlayerShield() => 0.8f;

        public float GetPlayerPower() => 1f;


        public static UIInventoryItem[] scanitems = new[]
            {
                new UIInventoryItem()
                {
                    Hardpoint = "HpWeapon01",
                    IdsHardpoint = 1526,
                    IdsHardpointDescription = 907,
                    Icon = @"equipment\models\commodities\nn_icons\EQUIPICON_gun.3db",
                    IdsName = 263175,
                    IdsInfo = 264175,
                    Volume = 0,
                },
                new UIInventoryItem()
                {
                    Hardpoint = "HpWeapon02",
                    IdsHardpoint = 1527,
                    IdsHardpointDescription = 907
                },
                new UIInventoryItem()
                {
                    Icon = @"Equipment\models\commodities\nn_icons\COMMOD_chemicals.3db",
                    Price = 240,
                    IdsName = 261626,
                    IdsInfo = 65908,
                    Combinable = true,
                    Count = 32,
                    Volume = 1
                },
                new UIInventoryItem()
                {
                    Icon = @"Equipment\models\commodities\nn_icons\COMMOD_metals.3db",
                    Price = 40,
                    IdsName = 261627,
                    IdsInfo = 65908,
                    Combinable = true,
                    Count = 1,
                    Volume = 1
                },
                new UIInventoryItem()
                {
                    Icon = @"equipment\models\commodities\nn_icons\EQUIPICON_gun.3db",
                    Price = 1000,
                    IdsName = 263175,
                    IdsInfo = 264175,
                    Combinable = false,
                    Count = 1,
                    MountIcon = false,
                    CanMount = false
                },
                new UIInventoryItem()
                {
                    Icon = @"equipment\models\commodities\nn_icons\EQUIPICON_gun.3db",
                    Price = 2000,
                    IdsName = 263177,
                    IdsInfo = 264177,
                    Combinable = false,
                    Count = 1,
                    MountIcon = false,
                    CanMount = false
                },
            };

        public class TraderFake
        {
            public static UIInventoryItem[] pitems = new[]
            {
                new UIInventoryItem()
                {
                    Hardpoint = "HpWeapon01",
                    IdsHardpoint = 1526,
                    IdsHardpointDescription = 907,
                    MountIcon = true,
                    Icon = @"equipment\models\commodities\nn_icons\EQUIPICON_gun.3db",
                    IdsName = 263175,
                    IdsInfo = 264175,
                    Volume = 0,
                },
                new UIInventoryItem()
                {
                    Hardpoint = "HpWeapon02",
                    IdsHardpoint = 1527,
                    IdsHardpointDescription = 907
                },
                new UIInventoryItem()
                {
                    Icon = @"Equipment\models\commodities\nn_icons\COMMOD_chemicals.3db",
                    Price = 240,
                    PriceRank = "good",
                    IdsName = 261626,
                    IdsInfo = 65908,
                    Combinable = true,
                    Count = 32,
                    Volume = 1
                },
                new UIInventoryItem()
                {
                    Icon = @"Equipment\models\commodities\nn_icons\COMMOD_metals.3db",
                    Price = 40,
                    PriceRank = "bad",
                    IdsName = 261627,
                    IdsInfo = 65908,
                    Combinable = true,
                    Count = 1,
                    Volume = 1
                },
                new UIInventoryItem()
                {
                    Icon = @"equipment\models\commodities\nn_icons\EQUIPICON_gun.3db",
                    Price = 1000,
                    IdsName = 263175,
                    IdsInfo = 264175,
                    Combinable = false,
                    Count = 1,
                    MountIcon = true,
                    CanMount = true
                },
                new UIInventoryItem()
                {
                    Icon = @"equipment\models\commodities\nn_icons\EQUIPICON_gun.3db",
                    Price = 2000,
                    IdsName = 263177,
                    IdsInfo = 264177,
                    Combinable = false,
                    Count = 1,
                    MountIcon = true,
                    CanMount = false
                },


            };

            public static UIInventoryItem[] titems = new[]
            {
                new UIInventoryItem()
                {
                    Icon = @"Equipment\models\commodities\nn_icons\COMMOD_chemicals.3db",
                    Price = 240,
                    PriceRank = "neutral",
                    IdsName = 261626, //mox
                    IdsInfo = 65908,
                    Combinable = true,
                    Count = 0,
                    Volume = 1
                },
                new UIInventoryItem()
                {
                    Icon = @"Equipment\models\commodities\nn_icons\COMMOD_metals.3db",
                    Price = 20000,
                    PriceRank = "bad",
                    IdsName = 261627, //basic alloy
                    IdsInfo = 65885,
                    Combinable = true,
                    Count = 0,
                    Volume = 1
                }
            };

            public UIInventoryItem[] GetPlayerGoods(string filter) => pitems;
            public UIInventoryItem[] GetTraderGoods(string filter) => titems;

            public float GetHoldSize() => 60;

            public float GetUsedHoldSpace() => 30;

            public void Buy(string good, int count, Closure onSuccess)
            {
            }

            public void Sell(UIInventoryItem item, int count, Closure onSuccess)
            {
                onSuccess.Call();
            }

            public void OnUpdateInventory(Closure handler)
            {
            }

            public void ProcessMount(UIInventoryItem item, Closure onsuccess)
            {
                onsuccess.Call("mount");
            }
        }

        public FakeKeyMap GetKeyMap() => new FakeKeyMap();

        public TraderFake Trader = new TraderFake();
        public FakeShipDealer ShipDealer = new FakeShipDealer();

        public string SelectionName() => "Selected Object";
        public bool SelectionVisible() => true;

        public float SelectionHealth() => 0.5f;

        public void UseRepairKits() => FLLog.Info("Lua", "Use repair kits pressed!");

        public void UseShieldBatteries() => FLLog.Info("Lua", "Use shield batteries pressed!");

        public float SelectionShield() => 0.75f;

        public string SelectionReputation() => "friendly";

        public Vector2 SelectionPosition() => OverridePosition ?? new Vector2(300,300);

        public int RepairKitCount() => 10;

        public int ShieldBatteryCount() => 12;

        [WattleScriptHidden] public Vector2? OverridePosition = null;

        public TargetShipWireframe SelectionWireframe() => null;

        public int ThrustPercent() => 111;
        public int Speed() => 67;
        public void HotspotPressed(string hotspot)
        {

        }

        public void SetIndicatorLayer(Container container)
        {
        }

        public void SetReticleTemplate(UiWidget template, Closure callback)
        {
        }

        public void SetUnselectedArrowTemplate(UiWidget template, Closure callback)
        {
        }

        public void SetSelectedArrowTemplate(UiWidget template, Closure callback)
        {
        }

        public string ActiveNavbarButton()
        {
            var btns = GetNavbarButtons();
            if (ActiveHotspotIndex >= btns.Length) return btns[0].IDS;
            return btns[ActiveHotspotIndex].IDS;
        }

        [WattleScriptHidden]
        public bool Multiplayer = false;
        public bool IsMultiplayer() => Multiplayer;

        public void NewGame() { }

        public void Exit() { }

        private TestCharacterList clist = new TestCharacterList();
        public TestCharacterList CharacterList()
        {
            return clist;
        }

        private MainWindow win;
        public TestingApi(MainWindow win)
        {
            this.win = win;
            this.settings.RenderContext = win.RenderContext;
        }

        public void RequestNewCharacter()
        {
            win.UiEvent("OpenNewCharacter");
        }

        public void LoadCharacter(){}
        public void DeleteCharacter() { }

        public void NewCharacter(string name, int index) { }


        public bool CanScanSelected() => contacts.SelectedIndex == 0;

        public void ScanSelected()
        {
            if (contacts.SelectedIndex == 0)
            {
                scanHandler.Call(true);
            }
        }

        public void StopScan()
        {

        }

        public bool CanTractorSelected() => contacts.SelectedIndex == 1;
        public bool CanTractorAll() => true;

        public void TractorSelected()
        {
        }

        public void TractorAll()
        {
        }

        public Infocard _ScannedInfocard;
        public Infocard GetScannedShipInfocard()
        {
            return _ScannedInfocard;
        }

        public UIInventoryItem[] GetScannedInventory(string filter) => scanitems;

        public UIInventoryItem[] GetPlayerInventory(string filter) => scanitems;

        private Closure scanHandler;
        public void OnUpdateScannedInventory(Closure handler)
        {
            this.scanHandler = handler;
        }

        public void OnUpdatePlayerInventory(Closure handler)
        {
        }
    }


    [WattleScriptUserData]
    public class FakeShipDealer
    {
        public UISoldShip[] SoldShips() => new[]{
            new UISoldShip()
            {
                IdsName = 237051,
                IdsInfo = 66598,
                Price = 172000,
                Model = @"DATA\ships\rheinland\rh_elite\rh_elite.cmp",
                Icon = @"DATA\Equipment\models\commodities\nn_icons\rh_elite.3db",
                ShipClass = 1,
            },
            new UISoldShip()
            {
                IdsName = 237033,
                IdsInfo = 66567,
                Price = 10400,
                Model = @"DATA\ships\liberty\li_elite\li_elite.cmp",
                Icon = @"DATA\Equipment\models\commodities\nn_icons\li_elite.3db",
                ShipClass = 4,
            }
        };

        public UISoldShip PlayerShip() => new UISoldShip()
        {
            IdsName = 237015,
            IdsInfo = 66527,
            Price = 4800,
            Model = @"DATA\ships\civilian\cv_starflier\cv_starflier.cmp",
            Icon = @"DATA\Equipment\models\commodities\nn_icons\cv_starflier.3db",
        };

        public void StartPurchase(UISoldShip ship, Closure callback)
        {

        }

        public int GetHoldSize() => 60;

        public UIInventoryItem[] GetPlayerGoods(string filter) => TestingApi.TraderFake.pitems;
        public UIInventoryItem[] GetDealerGoods(string filter) => TestingApi.TraderFake.titems;

    }
}
