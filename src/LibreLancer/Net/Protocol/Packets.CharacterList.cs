// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Collections.Generic;
using Lidgren.Network;
using LibreLancer.Data.NewCharDB;

namespace LibreLancer
{
    public class NewCharacterDBPacket : IPacket
    {
        public List<NewCharFaction> Factions;
        public List<NewCharPackage> Packages;
        public List<NewCharPilot> Pilots;
        public static NewCharacterDBPacket Read(NetIncomingMessage message)
        {
            var facCount = message.ReadVariableInt32();
            var factions = new List<NewCharFaction>(facCount);
            for (int i = 0; i < facCount; i++)
            {
                factions.Add(new NewCharFaction() {
                    Nickname = message.ReadString(),
                    RepGroup = message.ReadString(),
                    Base = message.ReadString(),
                    Package = message.ReadString(),
                    Pilot = message.ReadString()
                });
            }
            var pkgCount = message.ReadVariableInt32();
            var packages = new List<NewCharPackage>(pkgCount);
            for (int i = 0; i < pkgCount; i++)
            {
                packages.Add(new NewCharPackage() {
                    Nickname = message.ReadString(),
                    StridName = message.ReadInt32(),
                    StridDesc = message.ReadInt32(),
                    Ship = message.ReadString(),
                    Loadout = message.ReadString(),
                    Money = message.ReadInt64()
                });
            }
            var pilotCount = message.ReadVariableInt32();
            var pilots = new List<NewCharPilot>(pilotCount);
            for (int i = 0; i < pilotCount; i++)
            {
                pilots.Add(new NewCharPilot() {
                    Nickname = message.ReadString(),
                    Body = message.ReadString(),
                    Comm = message.ReadString(),
                    Voice = message.ReadString(),
                    BodyAnim = message.ReadString(),
                    CommAnim = new string[] { message.ReadString(), message.ReadString() }
                });
            }
            return new NewCharacterDBPacket() {
                Factions = factions,
                Packages = packages,
                Pilots = pilots
            };
        }

        public void WriteContents(NetOutgoingMessage message)
        {
            message.WriteVariableInt32(Factions.Count);
            foreach(var f in Factions) {
                message.Write(f.Nickname);
                message.Write(f.RepGroup);
                message.Write(f.Base);
                message.Write(f.Package);
                message.Write(f.Pilot);
            }
            message.WriteVariableInt32(Packages.Count);
            foreach(var p in Packages) {
                message.Write(p.Nickname);
                message.Write(p.StridName);
                message.Write(p.StridDesc);
                message.Write(p.Ship);
                message.Write(p.Loadout);
                message.Write(p.Money);
            }
            message.WriteVariableInt32(Pilots.Count);
            foreach(var p in Pilots) {
                message.Write(p.Nickname);
                message.Write(p.Body);
                message.Write(p.Comm);
                message.Write(p.Voice);
                message.Write(p.BodyAnim);
                message.Write(p.CommAnim[0]);
                message.Write(p.CommAnim[1]);
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

        public static CharacterListActionPacket Read(NetIncomingMessage message)
        {
            var cla = new CharacterListActionPacket();
            cla.Action = (CharacterListAction)message.ReadByte();
            switch(cla.Action)
            {
                case CharacterListAction.SelectCharacter:
                    cla.IntArg = message.ReadVariableInt32();
                    break;
                case CharacterListAction.CreateNewCharacter:
                    cla.IntArg = message.ReadVariableInt32();
                    cla.StringArg = message.ReadString();
                    break;
            }
            return cla;
        }
        public void WriteContents(NetOutgoingMessage message)
        {
            message.Write((byte)Action);
            switch(Action)
            {
                case CharacterListAction.SelectCharacter:
                    message.WriteVariableInt32(IntArg);
                    break;
                case CharacterListAction.CreateNewCharacter:
                    message.WriteVariableInt32(IntArg);
                    message.Write(StringArg);
                    break;
            }
        }
    }
    public class CharacterListActionResponsePacket : IPacket
    {
        public bool Success;
        public string FailReason;
        public static CharacterListActionResponsePacket Read(NetIncomingMessage message)
        {
            var p = new CharacterListActionResponsePacket();
            p.Success = message.ReadByte() != 0;
            if (!p.Success) p.FailReason = message.ReadString();
            return p;
        }
        public void WriteContents(NetOutgoingMessage message)
        {
            message.Write(Success ? (byte)1 : (byte)0);
            if (!Success) message.Write(FailReason);
        }
    }
    public class OpenCharacterListPacket : IPacket
    {
        public CharacterSelectInfo Info;
        public static OpenCharacterListPacket Read(NetIncomingMessage message)
        {
            var oc = new OpenCharacterListPacket();
            oc.Info = new CharacterSelectInfo();
            oc.Info.ServerName = message.ReadString();
            oc.Info.ServerDescription = message.ReadString();
            oc.Info.ServerNews = message.ReadString();
            var charCount = message.ReadVariableInt32();
            oc.Info.Characters = new List<SelectableCharacter>(charCount);
            for(int i = 0; i < charCount; i++)
            {
                var c = new SelectableCharacter();
                c.Name = message.ReadString();
                c.Rank = message.ReadVariableInt32();
                c.Funds = message.ReadInt64();
                c.Ship = message.ReadString();
                c.Location = message.ReadString();
                oc.Info.Characters.Add(c);
            }
            return oc;
        }
        public void WriteContents(NetOutgoingMessage message)
        {
            message.Write(Info.ServerName);
            message.Write(Info.ServerDescription);
            message.Write(Info.ServerNews);
            message.WriteVariableInt32(Info.Characters.Count);
            foreach(var c in Info.Characters)
            {
                message.Write(c.Name);
                message.WriteVariableInt32(c.Rank);
                message.Write(c.Funds);
                message.Write(c.Ship);
                message.Write(c.Location);
            }
        }
    }
    public class AddCharacterPacket : IPacket
    {
        public SelectableCharacter Character;
        public static AddCharacterPacket Read(NetIncomingMessage message)
        {
            var ac = new AddCharacterPacket();
            ac.Character = new SelectableCharacter();
            ac.Character.Name = message.ReadString();
            ac.Character.Rank = message.ReadVariableInt32();
            ac.Character.Funds = message.ReadInt64();
            ac.Character.Ship = message.ReadString();
            ac.Character.Location = message.ReadString();
            return ac;
        }
        public void WriteContents(NetOutgoingMessage message)
        {
            message.Write(Character.Name);
            message.WriteVariableInt32(Character.Rank);
            message.Write(Character.Funds);
            message.Write(Character.Ship);
            message.Write(Character.Location);
        }
    }
}
