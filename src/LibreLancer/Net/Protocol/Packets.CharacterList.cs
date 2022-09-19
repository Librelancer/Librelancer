// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Collections.Generic;
using LiteNetLib;
using LiteNetLib.Utils;
using LibreLancer.Data.NewCharDB;

namespace LibreLancer
{
    public class NewCharacterDBPacket : IPacket
    {
        public List<NewCharFaction> Factions;
        public List<NewCharPackage> Packages;
        public List<NewCharPilot> Pilots;
        public static NewCharacterDBPacket Read(NetPacketReader message)
        {
            var facCount = (int)message.GetVariableUInt32();
            var factions = new List<NewCharFaction>(facCount);
            for (int i = 0; i < facCount; i++)
            {
                factions.Add(new NewCharFaction() {
                    Nickname = message.GetStringPacked(),
                    RepGroup = message.GetStringPacked(),
                    Base = message.GetStringPacked(),
                    Package = message.GetStringPacked(),
                    Pilot = message.GetStringPacked()
                });
            }
            var pkgCount = (int)message.GetVariableUInt32();
            var packages = new List<NewCharPackage>(pkgCount);
            for (int i = 0; i < pkgCount; i++)
            {
                packages.Add(new NewCharPackage() {
                    Nickname = message.GetStringPacked(),
                    StridName = message.GetInt(),
                    StridDesc = message.GetInt(),
                    Ship = message.GetStringPacked(),
                    Loadout = message.GetStringPacked(),
                    Money = message.GetVariableInt64()
                });
            }
            var pilotCount = (int)message.GetVariableUInt32();
            var pilots = new List<NewCharPilot>(pilotCount);
            for (int i = 0; i < pilotCount; i++)
            {
                pilots.Add(new NewCharPilot() {
                    Nickname = message.GetStringPacked(),
                    Body = message.GetStringPacked(),
                    Comm = message.GetStringPacked(),
                    Voice = message.GetStringPacked(),
                    BodyAnim = message.GetStringPacked(),
                    CommAnim = new string[] { message.GetStringPacked(), message.GetStringPacked() }
                });
            }
            return new NewCharacterDBPacket() {
                Factions = factions,
                Packages = packages,
                Pilots = pilots
            };
        }

        public void WriteContents(NetDataWriter outPacket)
        {
            outPacket.PutVariableUInt32((uint)Factions.Count);
            foreach(var f in Factions) {
                outPacket.PutStringPacked(f.Nickname);
                outPacket.PutStringPacked(f.RepGroup);
                outPacket.PutStringPacked(f.Base);
                outPacket.PutStringPacked(f.Package);
                outPacket.PutStringPacked(f.Pilot);
            }
            outPacket.PutVariableUInt32((uint)Packages.Count);
            foreach(var p in Packages) {
                outPacket.PutStringPacked(p.Nickname);
                outPacket.Put(p.StridName);
                outPacket.Put(p.StridDesc);
                outPacket.PutStringPacked(p.Ship);
                outPacket.PutStringPacked(p.Loadout);
                outPacket.PutVariableInt64(p.Money);
            }
            outPacket.PutVariableUInt32((uint)Pilots.Count);
            foreach(var p in Pilots) {
                outPacket.PutStringPacked(p.Nickname);
                outPacket.PutStringPacked(p.Body);
                outPacket.PutStringPacked(p.Comm);
                outPacket.PutStringPacked(p.Voice);
                outPacket.PutStringPacked(p.BodyAnim);
                outPacket.PutStringPacked(p.CommAnim[0]);
                outPacket.PutStringPacked(p.CommAnim[1]);
            }
        }
    }
    public class OpenCharacterListPacket : IPacket
    {
        public CharacterSelectInfo Info;
        public static OpenCharacterListPacket Read(NetPacketReader message)
        {
            var oc = new OpenCharacterListPacket();
            oc.Info = new CharacterSelectInfo();
            oc.Info.ServerName = message.GetStringPacked();
            oc.Info.ServerDescription = message.GetStringPacked();
            oc.Info.ServerNews = message.GetStringPacked();
            var charCount = (int)message.GetVariableUInt32();
            oc.Info.Characters = new List<SelectableCharacter>(charCount);
            for(int i = 0; i < charCount; i++)
            {
                var c = new SelectableCharacter();
                c.Name = message.GetStringPacked();
                c.Rank = (int)message.GetVariableUInt32();
                c.Funds = message.GetVariableInt64();
                c.Ship = message.GetStringPacked();
                c.Location = message.GetStringPacked();
                oc.Info.Characters.Add(c);
            }
            return oc;
        }
        public void WriteContents(NetDataWriter outPacket)
        {
            outPacket.PutStringPacked(Info.ServerName);
            outPacket.PutStringPacked(Info.ServerDescription);
            outPacket.PutStringPacked(Info.ServerNews);
            outPacket.PutVariableUInt32((uint)Info.Characters.Count);
            foreach(var c in Info.Characters)
            {
                outPacket.PutStringPacked(c.Name);
                outPacket.PutVariableUInt32((uint)c.Rank);
                outPacket.PutVariableInt64(c.Funds);
                outPacket.PutStringPacked(c.Ship);
                outPacket.PutStringPacked(c.Location);
            }
        }
    }
    public class AddCharacterPacket : IPacket
    {
        public SelectableCharacter Character;
        public static AddCharacterPacket Read(NetPacketReader message)
        {
            var ac = new AddCharacterPacket();
            ac.Character = new SelectableCharacter();
            ac.Character.Name = message.GetStringPacked();
            ac.Character.Rank = (int)message.GetVariableUInt32();
            ac.Character.Funds = message.GetVariableInt64();
            ac.Character.Ship = message.GetStringPacked();
            ac.Character.Location = message.GetStringPacked();
            return ac;
        }
        public void WriteContents(NetDataWriter outPacket)
        {
            outPacket.PutStringPacked(Character.Name);
            outPacket.PutVariableUInt32((uint)Character.Rank);
            outPacket.PutVariableInt64(Character.Funds);
            outPacket.PutStringPacked(Character.Ship);
            outPacket.PutStringPacked(Character.Location);
        }
    }
}
