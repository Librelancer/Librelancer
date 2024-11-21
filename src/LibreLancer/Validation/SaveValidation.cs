using System;
using System.Collections.Generic;
using System.Linq;
using LibreLancer.Data;
using LibreLancer.Data.Save;
using LibreLancer.GameData;
using LibreLancer.GameData.World;

namespace LibreLancer.Validation;

public static class SaveValidation
{
    public static List<ValidationError> ValidateSavePlayer(GameDataManager gameDataManager, SavePlayer savePlayer)
    {
        var result = new List<ValidationError>();

        if (savePlayer.Rank <= 0)
        {
            result.Add(new ValidationError
            {
                severity = ValidationSeverity.Warning,
                message = $"Player rank is a non-positive value of {savePlayer.Rank}"
            });
        }

        if (savePlayer.NumKills <= 0)
        {
            result.Add(new ValidationError
            {
                severity = ValidationSeverity.Warning,
                message = $"Player number of kills is a non-positive value of {savePlayer.NumKills}"
            });
        }

        if (savePlayer.NumMissionSuccesses <= 0)
        {
            result.Add(new ValidationError
            {
                severity = ValidationSeverity.Warning,
                message =
                    $"Player number of mission successes is a non-positive value of {savePlayer.NumMissionSuccesses}"
            });
        }

        if (savePlayer.NumMissionFailures <= 0)
        {
            result.Add(new ValidationError
            {
                severity = ValidationSeverity.Warning,
                message =
                    $"Player number of mission failures is a non-positive value of {savePlayer.NumMissionFailures}"
            });
        }


        foreach (var rep in savePlayer.House)
        {
            result.AddRange(ValidateReputation(gameDataManager, rep));
        }


        gameDataManager.Ini.Voices.Voices.TryGetValue(savePlayer.Voice, out var voice);
        if (voice is null)
        {
            result.Add(new ValidationError
            {
                severity = ValidationSeverity.Warning,
                message =
                    $"Voice is not a valid voice of {savePlayer.Voice}"
            });
        }

        if (gameDataManager.Bodyparts.Get(savePlayer.Head) is null)
        {
            result.Add(new ValidationError
            {
                severity = ValidationSeverity.Warning,
                message =
                    $"Head {savePlayer.Head} is not a valid body part."
            });
        }

        if (gameDataManager.Bodyparts.Get(savePlayer.Body) is null)
        {
            result.Add(new ValidationError
            {
                severity = ValidationSeverity.Warning,
                message =
                    $"Body {savePlayer.Body} is not a valid body part."
            });
        }

        if (gameDataManager.Bodyparts.Get(savePlayer.LeftHand) is null)
        {
            result.Add(new ValidationError
            {
                severity = ValidationSeverity.Warning,
                message =
                    $"LeftHand {savePlayer.LeftHand} is not a valid body part."
            });
        }

        if (gameDataManager.Bodyparts.Get(savePlayer.RightHand) is null)
        {
            result.Add(new ValidationError
            {
                severity = ValidationSeverity.Warning,
                message =
                    $"RightHand {savePlayer.RightHand} is not a valid body part."
            });
        }

        if (gameDataManager.Bodyparts.Get(savePlayer.ComHead) is null)
        {
            result.Add(new ValidationError
            {
                severity = ValidationSeverity.Warning,
                message =
                    $"ComHead {savePlayer.ComHead} is not a valid body part."
            });
        }

        if (gameDataManager.Bodyparts.Get(savePlayer.ComBody) is null)
        {
            result.Add(new ValidationError
            {
                severity = ValidationSeverity.Warning,
                message =
                    $"ComBody {savePlayer.ComBody} is not a valid body part."
            });
        }

        if (gameDataManager.Bodyparts.Get(savePlayer.ComLeftHand) is null)
        {
            result.Add(new ValidationError
            {
                severity = ValidationSeverity.Warning,
                message =
                    $"ComLeftHand {savePlayer.ComLeftHand} is not a valid body part."
            });
        }

        if (gameDataManager.Bodyparts.Get(savePlayer.ComRightHand) is null)
        {
            result.Add(new ValidationError
            {
                severity = ValidationSeverity.Warning,
                message =
                    $"ComRightHand {savePlayer.ComRightHand} is not a valid body part."
            });
        }

        if (gameDataManager.Systems.Get(FLHash.CreateID(savePlayer.System)) is null)
        {
            result.Add(new ValidationError
            {
                severity = ValidationSeverity.Error,
                message =
                    $"System {savePlayer.System} does not exist"
            });
        }

        if (gameDataManager.Bases.Get(FLHash.CreateID(savePlayer.LastBase)) is null)
        {
            result.Add(new ValidationError
            {
                severity = ValidationSeverity.Error,
                message =
                    $"Base {savePlayer.LastBase} specified in last_base does not exist"
            });
        }

        //Assuming that if Pos and Rot are not present in the save file they default to <0,0,0> when loaded
        if (gameDataManager.Bases.Get(FLHash.CreateID(savePlayer.Base)) is null &&
            Math.Abs(savePlayer.Position.Length()) < .05 && savePlayer.Rotate.Length() is not 0)
        {
            result.Add(new ValidationError
            {
                severity = ValidationSeverity.Error,
                message =
                    $"Base {savePlayer.Base} does not exist."
            });
        }


        foreach (var cargo in savePlayer.Cargo)
        {
            result.AddRange(ValidateCargo(gameDataManager, cargo));
        }

        if (gameDataManager.Ships.Get(savePlayer.ShipArchetype) is null)
        {
            result.Add(new ValidationError
            {
                severity = ValidationSeverity.Error,
                message =
                    $"Ship archetype {savePlayer.ShipArchetype} does not exist"
            });
        }

        var ship = gameDataManager.Ships.Get(savePlayer.ShipArchetype);

        foreach (var equip in savePlayer.Equip)
        {
            var res = ValidateEquipment(gameDataManager, equip, ship);
            if (res != null)
            {
                result.Add((ValidationError) res);
            }
        }

        var solars = gameDataManager.Systems.SelectMany(system => system.Objects).ToList()
            .GroupBy(obj => FLHash.CreateID(obj.Nickname))
            .ToDictionary(group => group.Key, group => group.First());

        foreach (var visit in savePlayer.Visit)
        {
            var res = ValidateVisitEntry(solars, visit);
            if (res is not null)
            {
                result.Add((ValidationError) res);
            }
        }

        return result;
    }

    private static List<ValidationError> ValidateCargo(GameDataManager gameDataManager, PlayerCargo cargo)
    {
        var result = new List<ValidationError>();

        var checkOne = gameDataManager.Equipment.Get(cargo.Item);
        var checkTwo = gameDataManager.Goods.Get(cargo.Item);

        if (checkOne is null && checkTwo is null)
        {
            result.Add(new ValidationError
            {
                severity = ValidationSeverity.Error,
                message = $"{cargo.GetHashCode()} is not a valid cargo Id."
            });
        }

        if (cargo.Count <= 0)
        {
            result.Add(new ValidationError
            {
                severity = ValidationSeverity.Warning,
                message = $"Cargo count has a non-positive value of {cargo.Count}"
            });
        }

        if (cargo.PercentageHealth is <= 0 or > 1)
        {
            result.Add(new ValidationError
            {
                severity = ValidationSeverity.Warning,
                message = $"Percent health has an invalid value of {cargo.PercentageHealth}"
            });
        }

        return result;
    }


    private static List<ValidationError> ValidateReputation(GameDataManager gameDataManager, SaveRep rep)
    {
        var result = new List<ValidationError>();

        if (gameDataManager.Factions.Get(FLHash.CreateID(rep.Group)) is null)
        {
            result.Add(new ValidationError
            {
                severity = ValidationSeverity.Error,
                message = $"Faction {rep.Group} defined in reputations does not exist."
            });
        }

        //comparing to -1.01 instead of -1.0 to account for float error
        if (rep.Reputation < -1.01 || rep.Reputation > 1.01)
        {
            result.Add(new ValidationError
            {
                severity = ValidationSeverity.Warning,
                message = $"Faction reputation value for {rep.Group} is outside the range of [-1, 1] {rep.Reputation}."
            });
        }

        return result;
    }

    private static ValidationError? ValidateEquipment(GameDataManager gameDataManager, PlayerEquipment equipment,
        Ship ship)
    {
        if (ship is null)
        {
            return null;
        }

        var equip = gameDataManager.Equipment.Get(equipment.Item);
        if (equip is null)
        {
            return new ValidationError
            {
                severity = ValidationSeverity.Error,
                message = $"Equipment {equipment.Item} does not exist"
            };
        }

        if (ship.PossibleHardpoints.TryGetValue(equipment.Hardpoint, out var hpTypes) is false)
        {
            return new ValidationError
            {
                severity = ValidationSeverity.Warning,
                message = $"Equipment {equipment.Item} is equipped to non-existent hard point {equipment.Hardpoint}."
            };
        }

        if (hpTypes.Any(hp => hp.Equals(equip.HpType, StringComparison.OrdinalIgnoreCase)) is false)
        {
            return new ValidationError
            {
                severity = ValidationSeverity.Warning,
                message =
                    $"Equipment {equipment.Item} is mounted on unsupported hard point of ${equipment.Hardpoint} with an HpType of {equip.HpType}."
            };
        }

        return null;
    }

    private static ValidationError? ValidateVisitEntry(Dictionary<uint, SystemObject> solars, VisitEntry visitEntry)
    {
        return !solars.TryGetValue(visitEntry.Obj.Hash, out var _)
            ? new ValidationError
            {
                severity = ValidationSeverity.Warning,
                message =
                    $"System Object with id {visitEntry.Obj} does not exist"
            }
            : null;
    }
}
