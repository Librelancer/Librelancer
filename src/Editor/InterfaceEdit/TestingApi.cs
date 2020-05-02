// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using LibreLancer;
using LibreLancer.Interface;
using System.Collections.Generic;

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
    public class TestingApi
    {
        static readonly NavbarButtonInfo cityscape = new NavbarButtonInfo("IDS_HOTSPOT_EXIT", "Cityscape");
        static readonly NavbarButtonInfo bar = new NavbarButtonInfo("IDS_HOTSPOT_BAR", "Bar");
        static readonly NavbarButtonInfo trader = new NavbarButtonInfo("IDS_HOTSPOT_COMMODITYTRADER_ROOM", "Trader");
        static readonly NavbarButtonInfo equip = new NavbarButtonInfo("IDS_HOTSPOT_EQUIPMENTDEALER_ROOM", "Equipment");
        static readonly NavbarButtonInfo shipDealer = new NavbarButtonInfo("IDS_HOTSPOT_SHIPDEALER_ROOM", "ShipDealer");

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
        public int ActiveHotspotIndex = 0;

        TestServerList serverList = new TestServerList();
        public TestServerList ServerList() => serverList;


        public void ConnectSelection()
        {
        }

        public void StartNetworking()
        {
        }

        public void StopNetworking()
        {
        }
        
        public NavbarButtonInfo[] GetNavbarButtons()
        {
            var l = new List<NavbarButtonInfo>();
            l.Add(cityscape);
            if(HasBar) l.Add(bar);
            if (HasTrader) l.Add(trader);
            if(HasEquip) l.Add(equip);
            if(HasShipDealer) l.Add(shipDealer);
            return l.ToArray();
        }

        public NavbarButtonInfo[] GetActionButtons()
        {
            var l = new List<NavbarButtonInfo>();
            if (HasLaunchAction) l.Add(new NavbarButtonInfo("Launch", "IDS_HOTSPOT_LAUNCH"));
            if (HasRepairAction) l.Add(new NavbarButtonInfo("Repair", "IDS_NN_REPAIR_YOUR_SHIP"));
            if (HasMissionVendor) l.Add(new NavbarButtonInfo("MissionVendor", "IDS_HOTSPOT_MISSIONVENDOR"));
            if(HasNewsAction) l.Add(new NavbarButtonInfo("NewsVendor", "IDS_HOTSPOT_NEWSVENDOR"));
            if(HasCommodityTraderAction) l.Add(new NavbarButtonInfo("CommodityTrader", "IDS_HOTSPOT_COMMODITYTRADER"));
            if(HasEquipmentDealerAction) l.Add(new NavbarButtonInfo("EquipmentDealer", "IDS_HOTSPOT_EQUIPMENTDEALER"));
            return l.ToArray();
        }

        public Maneuver[] ManeuverData;
        public Maneuver[] GetManeuvers() => ManeuverData;
        public string GetActiveManeuver() => "FreeFlight";

        public LuaCompatibleDictionary<string, bool> GetManeuversEnabled()
        {
            var dict = new LuaCompatibleDictionary<string, bool>();
            dict.Set("FreeFlight", true);
            dict.Set("Goto", true);
            dict.Set("Dock", true);
            dict.Set("Formation", false);
            return dict;
        }

        public int ThrustPercent() => 100;
        public int Speed() => 80;
        public void HotspotPressed(string hotspot)
        {
            
        }

        public string ActiveNavbarButton()
        {
            var btns = GetNavbarButtons();
            if (ActiveHotspotIndex >= btns.Length) return btns[0].IDS;
            return btns[ActiveHotspotIndex].IDS;
        }

        public bool IsMultiplayer() => false;

        public void NewGame() { }

        public void Exit() { }
        

    }
}