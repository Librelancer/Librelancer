// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System.Collections.Generic;
using LibreLancer.Data.Schema.NewCharDB;

namespace LibreLancer.Net.Protocol
{
    public class NewCharacterDBPacket : IPacket
    {
        public List<NewCharFaction> Factions;
        public List<NewCharPackage> Packages;
        public List<NewCharPilot> Pilots;
        public static NewCharacterDBPacket Read(PacketReader message)
        {
            var facCount = (int)message.GetVariableUInt32();
            var factions = new List<NewCharFaction>(facCount);
            for (int i = 0; i < facCount; i++)
            {
                factions.Add(new NewCharFaction() {
                    Nickname = message.GetString(),
                    RepGroup = message.GetString(),
                    Base = message.GetString(),
                    Package = message.GetString(),
                    Pilot = message.GetString()
                });
            }
            var pkgCount = (int)message.GetVariableUInt32();
            var packages = new List<NewCharPackage>(pkgCount);
            for (int i = 0; i < pkgCount; i++)
            {
                packages.Add(new NewCharPackage() {
                    Nickname = message.GetString(),
                    StridName = message.GetInt(),
                    StridDesc = message.GetInt(),
                    Ship = message.GetString(),
                    Loadout = message.GetString(),
                    Money = message.GetVariableInt64()
                });
            }
            var pilotCount = (int)message.GetVariableUInt32();
            var pilots = new List<NewCharPilot>(pilotCount);
            for (int i = 0; i < pilotCount; i++)
            {
                pilots.Add(new NewCharPilot() {
                    Nickname = message.GetString(),
                    Body = message.GetString(),
                    Comm = message.GetString(),
                    Voice = message.GetString(),
                    BodyAnim = message.GetString(),
                    CommAnim = new string[] { message.GetString(), message.GetString() }
                });
            }
            return new NewCharacterDBPacket() {
                Factions = factions,
                Packages = packages,
                Pilots = pilots
            };
        }

        public void WriteContents(PacketWriter outPacket)
        {
            outPacket.PutVariableUInt32((uint)Factions.Count);
            foreach(var f in Factions) {
                outPacket.Put(f.Nickname);
                outPacket.Put(f.RepGroup);
                outPacket.Put(f.Base);
                outPacket.Put(f.Package);
                outPacket.Put(f.Pilot);
            }
            outPacket.PutVariableUInt32((uint)Packages.Count);
            foreach(var p in Packages) {
                outPacket.Put(p.Nickname);
                outPacket.Put(p.StridName);
                outPacket.Put(p.StridDesc);
                outPacket.Put(p.Ship);
                outPacket.Put(p.Loadout);
                outPacket.PutVariableInt64(p.Money);
            }
            outPacket.PutVariableUInt32((uint)Pilots.Count);
            foreach(var p in Pilots) {
                outPacket.Put(p.Nickname);
                outPacket.Put(p.Body);
                outPacket.Put(p.Comm);
                outPacket.Put(p.Voice);
                outPacket.Put(p.BodyAnim);
                outPacket.Put(p.CommAnim[0]);
                outPacket.Put(p.CommAnim[1]);
            }
        }
    }
    public class OpenCharacterListPacket : IPacket
    {
        public CharacterSelectInfo Info;
        public static OpenCharacterListPacket Read(PacketReader message)
        {
            var oc = new OpenCharacterListPacket();
            oc.Info = new CharacterSelectInfo();
            oc.Info.ServerName = message.GetString();
            oc.Info.ServerDescription = message.GetString();
            oc.Info.ServerNews = message.GetString();
            var charCount = (int)message.GetVariableUInt32();
            oc.Info.Characters = new List<SelectableCharacter>(charCount);
            for(int i = 0; i < charCount; i++)
            {
                var c = new SelectableCharacter();
                c.Name = message.GetString();
                c.Rank = (int)message.GetVariableUInt32();
                c.Funds = message.GetVariableInt64();
                c.Ship = message.GetString();
                c.Location = message.GetString();
                oc.Info.Characters.Add(c);
            }
            return oc;
        }
        public void WriteContents(PacketWriter outPacket)
        {
            outPacket.Put(Info.ServerName);
            outPacket.Put(Info.ServerDescription);
            outPacket.Put(Info.ServerNews);
            outPacket.PutVariableUInt32((uint)Info.Characters.Count);
            foreach(var c in Info.Characters)
            {
                outPacket.Put(c.Name);
                outPacket.PutVariableUInt32((uint)c.Rank);
                outPacket.PutVariableInt64(c.Funds);
                outPacket.Put(c.Ship);
                outPacket.Put(c.Location);
            }
        }
    }
    public class AddCharacterPacket : IPacket
    {
        public SelectableCharacter Character;
        public static AddCharacterPacket Read(PacketReader message)
        {
            var ac = new AddCharacterPacket();
            ac.Character = new SelectableCharacter();
            ac.Character.Name = message.GetString();
            ac.Character.Rank = (int)message.GetVariableUInt32();
            ac.Character.Funds = message.GetVariableInt64();
            ac.Character.Ship = message.GetString();
            ac.Character.Location = message.GetString();
            return ac;
        }
        public void WriteContents(PacketWriter outPacket)
        {
            outPacket.Put(Character.Name);
            outPacket.PutVariableUInt32((uint)Character.Rank);
            outPacket.PutVariableInt64(Character.Funds);
            outPacket.Put(Character.Ship);
            outPacket.Put(Character.Location);
        }
    }
}
