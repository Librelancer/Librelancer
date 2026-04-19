using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using LibreLancer.Data.GameData;
using LibreLancer.Data.GameData.World;
using LibreLancer.Data.Schema.GCS;
using LibreLancer.Render;
using LibreLancer.Resources;
using LibreLancer.Thn;
using LibreLancer.Utf.Anm;
using LibreLancer.World;
using LibreLancer.World.Components;

namespace LibreLancer.Client;

public static class ThnRoomHandler
{
    public static ThnScriptContext CreateContext(Base currentBase, BaseRoom currentRoom)
    {
        var ctx = new ThnScriptContext(currentRoom.OpenSet());
        if (currentBase.TerrainTiny != null)
        {
            ctx.Substitutions.Add("$terrain_tiny", currentBase.TerrainTiny);
        }

        if (currentBase.TerrainSml != null)
        {
            ctx.Substitutions.Add("$terrain_sml", currentBase.TerrainSml);
        }

        if (currentBase.TerrainMdm != null)
        {
            ctx.Substitutions.Add("$terrain_mdm", currentBase.TerrainMdm);
        }

        if (currentBase.TerrainLrg != null)
        {
            ctx.Substitutions.Add("$terrain_lrg", currentBase.TerrainLrg);
        }

        if (currentBase.TerrainDyna1 != null)
        {
            ctx.Substitutions.Add("$terrain_dyna_01", currentBase.TerrainDyna1);
        }

        if (currentBase.TerrainDyna2 != null)
        {
            ctx.Substitutions.Add("$terrain_dyna_02", currentBase.TerrainDyna2);
        }
        return ctx;
    }

    private static Regex markerRegex = new(@"^Z([sg])\/(\w+)\/(\w+)\/(\d\d)\/(\w+)\/?(\w+)?$");


    public static RoomNpcSpot[] GetSpots(BaseRoom currentRoom)
    {
        var script = currentRoom.SetScript.LoadScript();
        //HashSet<int> indices = new();
        List<RoomNpcSpot> allSpots = new();
        foreach (var e in script.Entities.Values.Where(e => e.Type == EntityTypes.Marker))
        {
            var match = markerRegex.Match(e.Name);
            if (!match.Success)
            {
                continue;
            }

            // Exclude:
            // Prop (doesn't have 7th group)
            // Non-NPC markers
            // Groups that are not Group A (not used)
            if(match.Groups.Count != 7 ||
               match.Groups[2].Value != "NPC" || match.Groups[5].Value != "A")
            {
                continue;
            }

            bool isDynamic = match.Groups[1].Value == "g";
            // One each of 01/02/03 for dynamic
            /*if (isDynamic)
            {
                if (!int.TryParse(match.Groups[4].Value, out var index))
                    continue;
                if (!indices.Add(index))
                    continue;
            }*/

            if (!Enum.TryParse<Posture>(match.Groups[6].ValueSpan, out var posture))
                continue;
            allSpots.Add(new(e.Name, isDynamic, posture));
        }

        return allSpots.ToArray();
    }

    public static ThnSceneObject AddNpc(Cutscene scene,
        ResourceManager resources,
        AnmFile charAnimations,
        string name,
        string spot,
        string? voice,
        Bodypart? head,
        Bodypart? body,
        Bodypart? rightHand,
        Bodypart? leftHand,
        Accessory? accessory,
        ResolvedThn? fidgetScript)
    {
        var skel = new DfmSkeletonManager(
            body?.LoadModel(resources)!, head?.LoadModel(resources),
            leftHand?.LoadModel(resources), rightHand?.LoadModel(resources));
        var obj = new GameObject
        {
            Nickname = name
        };
        var accessoryModel = accessory?.ModelFile?.LoadFile(resources)?.Drawable as IRigidModelFile;
        obj.RenderComponent = new CharacterRenderer(skel)
        {
            Accessory = accessory,
            AccessoryModel = accessoryModel?.CreateRigidModel(true, resources)
        };
        var animation = new AnimationComponent(obj, charAnimations);
        obj.AnimationComponent = animation;
        obj.AddComponent(animation);
        var spotObj = scene!.GetObject(spot)!;
        obj.SetLocalTransform(new Transform3D(spotObj.Translate with { Y = 0 }, spotObj.Rotate));
        var thnObj = new ThnSceneObject
        {
            Name = name,
            Translate = spotObj.Translate with { Y = 0 },
            Rotate = spotObj.Rotate,
            Object = obj,
            Voice = voice
        };
        scene.AddObject(thnObj);
        if (fidgetScript != null)
        {
            scene.FidgetScript(fidgetScript.LoadScript(), name);
        }
        return thnObj;
    }

}

public record struct RoomNpcSpot(string Nickname, bool Dynamic, Posture Posture);
