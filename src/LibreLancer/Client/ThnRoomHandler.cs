using System.Numerics;
using LibreLancer.Data.GameData;
using LibreLancer.Data.GameData.World;
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

    public static ThnSceneObject AddNpc(Cutscene scene,
        ResourceManager resources,
        AnmFile charAnimations,
        string name,
        string spot,
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
            Object = obj
        };
        scene.AddObject(thnObj);
        if (fidgetScript != null)
        {
            scene.FidgetScript(fidgetScript.LoadScript(), name);
        }
        return thnObj;
    }

}
