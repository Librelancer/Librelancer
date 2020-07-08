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
                    Money = message.GetLong()
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

        public void WriteContents(NetDataWriter message)
        {
            message.PutVariableUInt32((uint)Factions.Count);
            foreach(var f in Factions) {
                message.Put(f.Nickname);
                message.Put(f.RepGroup);
                message.Put(f.Base);
                message.Put(f.Package);
                message.Put(f.Pilot);
            }
            message.PutVariableUInt32((uint)Packages.Count);
            foreach(var p in Packages) {
                message.Put(p.Nickname);
                message.Put(p.StridName);
                message.Put(p.StridDesc);
                message.Put(p.Ship);
                message.Put(p.Loadout);
                message.Put(p.Money);
            }
            message.PutVariableUInt32((uint)Pilots.Count);
            foreach(var p in Pilots) {
                message.Put(p.Nickname);
                message.Put(p.Body);
                message.Put(p.Comm);
                message.Put(p.Voice);
                message.Put(p.BodyAnim);
                message.Put(p.CommAnim[0]);
                message.Put(p.CommAnim[1]);
            }
        }
    }
    public enum CharacterListAction : byte
    {
        RequestCharacterDB,
        SelectCharacter,
        CreateNewCharacter,
        DeleteCharacter
    }
    public class CharacterListActionPacket : IPacket
    {
        public CharacterListAction Action;
        public string StringArg;
        public int IntArg;

        public static CharacterListActionPacket Read(NetPacketReader message)
        {
            var cla = new CharacterListActionPacket();
            cla.Action = (CharacterListAction)message.GetByte();
            switch(cla.Action)
            {
                case CharacterListAction.SelectCharacter:
                    cla.IntArg = (int)message.GetVariableUInt32();
                    break;
                case CharacterListAction.CreateNewCharacter:
                    cla.IntArg = (int)message.GetVariableUInt32();
                    cla.StringArg = message.GetString();
                    break;
            }
            return cla;
        }
        public void WriteContents(NetDataWriter message)
        {
            message.Put((byte)Action);
            switch(Action)
            {
                case CharacterListAction.SelectCharacter:
                    message.PutVariableUInt32((uint)IntArg);
                    break;
                case CharacterListAction.CreateNewCharacter:
                    message.PutVariableUInt32((uint)IntArg);
                    message.Put(StringArg);
                    break;
            }
        }
    }
    public class CharacterListActionResponsePacket : IPacket
    {
        public bool Success;
        public string FailReason;
        public static CharacterListActionResponsePacket Read(NetPacketReader message)
        {
            var p = new CharacterListActionResponsePacket();
            p.Success = message.GetByte() != 0;
            if (!p.Success) p.FailReason = message.GetString();
            return p;
        }
        public void WriteContents(NetDataWriter message)
        {
            message.Put(Success ? (byte)1 : (byte)0);
            if (!Success) message.Put(FailReason);
        }
    }
    public class OpenCharacterListPacket : IPacket
    {
        public CharacterSelectInfo Info;
        public static OpenCharacterListPacket Read(NetPacketReader message)
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
                c.Funds = message.GetLong();
                c.Ship = message.GetString();
                c.Location = message.GetString();
                oc.Info.Characters.Add(c);
            }
            return oc;
        }
        public void WriteContents(NetDataWriter message)
        {
            message.Put(Info.ServerName);
            message.Put(Info.ServerDescription);
            message.Put(Info.ServerNews);
            message.PutVariableUInt32((uint)Info.Characters.Count);
            foreach(var c in Info.Characters)
            {
                message.Put(c.Name);
                message.PutVariableUInt32((uint)c.Rank);
                message.Put(c.Funds);
                message.Put(c.Ship);
                message.Put(c.Location);
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
            ac.Character.Name = message.GetString();
            ac.Character.Rank = (int)message.GetVariableUInt32();
            ac.Character.Funds = message.GetLong();
            ac.Character.Ship = message.GetString();
            ac.Character.Location = message.GetString();
            return ac;
        }
        public void WriteContents(NetDataWriter message)
        {
            message.Put(Character.Name);
            message.PutVariableUInt32((uint)Character.Rank);
            message.Put(Character.Funds);
            message.Put(Character.Ship);
            message.Put(Character.Location);
        }
    }
}
