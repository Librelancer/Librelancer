// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using LibreLancer.Client;
using LibreLancer.Data;
using LibreLancer.Data.Save;
using LibreLancer.Missions;

namespace LibreLancer.Server
{
    public static class SaveWriter
    {


        public static SaveGame CreateSave(
            NetCharacter ch,
            string description,
            int ids,
            DateTime? timeStamp,
            GameDataManager gameData,
            IEnumerable<MissionRtc> rtcs,
            IEnumerable<AmbientInfo> ambients,
            StoryProgress story)
        {
            var sg = new SaveGame();
            sg.Player = new SavePlayer();
            sg.Player.TimeStamp = timeStamp;
            if (description != null)
                sg.Player.Description = description;
            else
                sg.Player.DescripStrid = ids;
            sg.Player.Name = ch.Name;
            sg.Player.Base = ch.Base;
            sg.Player.System = ch.System;
            sg.Player.Position = ch.Position;
            sg.Player.Money = ch.Credits;
            sg.Player.Rank = (int)ch.Rank;
            if (ch.Ship != null)
                sg.Player.ShipArchetype = new HashValue(ch.Ship.Nickname);
            foreach (var item in ch.Items) {
                if (!string.IsNullOrEmpty(item.Hardpoint))
                {
                    sg.Player.Equip.Add(new PlayerEquipment()
                    {
                        Item = new HashValue(item.Equipment.CRC),
                        Hardpoint = item.Hardpoint.Equals("internal", StringComparison.OrdinalIgnoreCase)
                            ? ""
                            : item.Hardpoint
                    });
                }
                else
                {
                    sg.Player.Cargo.Add(new PlayerCargo() {
                        Item = new HashValue(item.Equipment.CRC),
                        Count = item.Count
                    });
                }
            }
            foreach (var rep in ch.Reputation.Reputations) {
                sg.Player.House.Add(new SaveRep() { Group = rep.Key.Nickname, Reputation = rep.Value });
            }
            sg.Player.Interface = 3; //Unknown, matching vanilla

            if (story != null)
            {
                sg.StoryInfo = new StoryInfo();
                sg.StoryInfo.Mission = story.CurrentMission?.Nickname ?? "No_Mission";
                sg.StoryInfo.DeltaWorth = story.NextLevelWorth;
                sg.StoryInfo.MissionNum = story.MissionNum;
            }
            if (rtcs != null ||
                ambients != null)
            {
                sg.MissionState = new MissionState();
                if(rtcs != null) sg.MissionState.Rtcs.AddRange(rtcs);
                if (ambients != null)
                {
                    sg.MissionState.Ambients.AddRange(
                        ambients.Select(x =>
                        {
                            var roomHash = FLHash.CreateLocationID(x.Base, x.Room);
                            return new MissionAmbient(x.Script, new HashValue(roomHash), x.Base);
                        }));
                }
            }
            // Vanilla requires faction relationships to be in the save file
            if (gameData != null)
            {
                foreach (var fc in gameData.Factions)
                {
                    if (fc.Reputations.Count == 0)
                        continue;
                    var g = new SaveGroup { Nickname = fc.Nickname };
                    foreach (var f2 in fc.Reputations)
                        g.Rep.Add(new SaveRep() { Group = f2.Key.Nickname, Reputation = f2.Value });
                    sg.Groups.Add(g);
                }
            }

            return sg;
        }
    }
}
