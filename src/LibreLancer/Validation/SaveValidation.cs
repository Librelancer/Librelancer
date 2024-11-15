using System.Collections.Generic;
using System.Linq;
using LibreLancer.Data.Save;
using LibreLancer.GameData;

namespace LibreLancer.Validation;

public static class SaveValidation
{
    public static List<string> ValidateSavePlayer(GameDataManager gameDataManager, SavePlayer savePlayer)
    {
        var result = new List<string>();

        if (savePlayer.Rank <= 0)
        {
            result.Add($"Player rank is a non-positive value of {savePlayer.Rank}");
        }

        if (savePlayer.NumKills <= 0)
        {
            result.Add($"Player number of kills is a non-positive value of {savePlayer.NumKills}");
        }

        if (savePlayer.NumMissionSuccesses <= 0)
        {
            result.Add(
                $"Player number of mission successes is a non-positive value of {savePlayer.NumMissionSuccesses}");
        }

        if (savePlayer.NumMissionFailures <= 0)
        {
            result.Add($"Player number of mission failures is a non-positive value of {savePlayer.NumMissionFailures}");
        }

        result.AddRange(savePlayer.House.Select(rep => ValidateReputation(gameDataManager, rep))
            .Where(repResult => repResult != null));

        //TODO: Validate Voice.
        gameDataManager.Ini.Voices.Voices.TryGetValue(savePlayer.Voice, out var voice);
        if (voice is null)
        {
            result.Add($"Voice is not a valid voice of {savePlayer.Voice}");
        }

        if (gameDataManager.Bodyparts.Get(savePlayer.Head) is null)
        {
            result.Add($"Head {savePlayer.Head} is not a valid body part.");
        }

        if (gameDataManager.Bodyparts.Get(savePlayer.Body) is null)
        {
            result.Add($"Body {savePlayer.Body} is not a valid body part.");
        }

        if (gameDataManager.Bodyparts.Get(savePlayer.LeftHand) is null)
        {
            result.Add($"LeftHand {savePlayer.LeftHand} is not a valid body part.");
        }

        if (gameDataManager.Bodyparts.Get(savePlayer.RightHand) is null)
        {
            result.Add($"RightHand {savePlayer.RightHand} is not a valid body part.");
        }

        if (gameDataManager.Bodyparts.Get(savePlayer.ComHead) is null)
        {
            result.Add($"ComHead {savePlayer.ComHead} is not a valid body part.");
        }

        if (gameDataManager.Bodyparts.Get(savePlayer.ComBody) is null)
        {
            result.Add($"ComBody {savePlayer.ComBody} is not a valid body part.");
        }

        if (gameDataManager.Bodyparts.Get(savePlayer.ComLeftHand) is null)
        {
            result.Add($"ComLeftHand {savePlayer.ComLeftHand} is not a valid body part.");
        }

        if (gameDataManager.Bodyparts.Get(savePlayer.ComRightHand) is null)
        {
            result.Add($"ComRightHand {savePlayer.ComRightHand} is not a valid body part.");
        }

        if (gameDataManager.Systems.Get(FLHash.CreateID(savePlayer.System)) is null)
        {
            result.Add($"System {savePlayer.System} does not exist");
        }

        if (gameDataManager.Bases.Get(FLHash.CreateID(savePlayer.LastBase)) is null)
        {
            result.Add($"Base {savePlayer.LastBase} specified in last_base does not exist");
        }

        //Assuming that if Pos and Rot are not present in the save file they default to <0,0,0> when loaded
        if (gameDataManager.Bases.Get(FLHash.CreateID(savePlayer.Base)) is null &&
            savePlayer.Position.Length() is not 0 && savePlayer.Rotate.Length() is not 0)
        {
            result.Add($"Base {savePlayer.Base} does not exist.");
        }


        result.AddRange(savePlayer.Cargo.Select(cargo => ValidateCargo(gameDataManager, cargo))
            .Where(cargoResult => cargoResult != null).ToList());

        if (gameDataManager.Ships.Get(savePlayer.ShipArchetype) is null)
        {
            result.Add($"Ship archetype {savePlayer.ShipArchetype} does not exist");
        }

        var ship = gameDataManager.Ships.Get(savePlayer.ShipArchetype);
        result.AddRange(savePlayer.Equip.Select(equip => ValidateEquipment(gameDataManager, equip, ship))
            .Where(equipResult => equipResult != null));

        result.AddRange(savePlayer.Visit.Select(visit => ValidateVisitEntry(gameDataManager, visit))
            .Where(visitRes => visitRes is not null));


        return result;
    }

    public static string ValidateCargo(GameDataManager gameDataManager, PlayerCargo cargo)
    {
        var checkOne = gameDataManager.Equipment.Get(cargo.Item);
        var checkTwo = gameDataManager.Goods.Get(cargo.Item);

        if (checkOne is null && checkTwo is null)
        {
            return $"{cargo.GetHashCode()} is not a valid cargo Id.";
        }

        if (cargo.Count <= 0)
        {
            return $"Cargo count has a non-positive value of {cargo.Count}";
        }

        if (cargo.PercentageHealth is <= 0 or > 1)
        {
            return $"Percent health has an invalid value of {cargo.PercentageHealth}";
        }

        return null;
    }


    public static string ValidateReputation(GameDataManager gameDataManager, SaveRep rep)
    {
        if (gameDataManager.Factions.Get(FLHash.CreateID(rep.Group)) is null)
        {
            return $"Faction {rep.Group} defined in reputations does not exist.";
        }

        //comparing to -1.01 instead of -1.0 to account for float error
        if (rep.Reputation < -1.01 || rep.Reputation > 1.01)
        {
            return $"Faction reputation value for {rep.Group} is outside the range of [-1, 1] {rep.Reputation}.";
        }

        return null;
    }

    public static string ValidateEquipment(GameDataManager gameDataManager, PlayerEquipment equipment, Ship ship)
    {
        var equip = gameDataManager.Equipment.Get(equipment.Item);

        if (equip is null)
        {
            return $"Equipment {equipment.Item} does not exist";
        }

        if (ship is null)
        {
            return $"Ship does not exist and thus equipment hard points for {equipment.Item} cannot be validated.";
        }

        if (ship.PossibleHardpoints.TryGetValue(equipment.Hardpoint, out var hpTypes) is false)
        {
            return $"Equipment {equipment.Item} is equipped to non-existent hard point {equipment.Hardpoint}.";
        }

        if (hpTypes.Any(hp => hp == equip.HpType) is false)
        {
            return
                $"Equipment {equipment.Item} is mounted on unsupported hard point of ${equipment.Hardpoint} with an HpType of {equip.HpType}.";
        }

        return null;
    }

    public static string ValidateVisitEntry(GameDataManager gameDataManager, VisitEntry visitEntry)
    {
        bool found = false;

        foreach (var sys in gameDataManager.Systems)
        {
            if (sys.Objects.Any(x => FLHash.CreateID(x.Nickname) == visitEntry.Obj))
            {
                found = true;
                break;
            }
        }

        if (!found)
        {
            return $"System Object with id {visitEntry.Obj} does not exist";
        }


        return null;
    }
}
