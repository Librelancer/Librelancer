using System.Collections.Generic;
using System.Linq;
using LibreLancer.Data.GameData;
using LibreLancer.Data.GameData.World;
using LibreLancer.Fx;
using LibreLancer.Resources;
using LibreLancer.Thn;
using LibreLancer.Utf.Dfm;

namespace LibreLancer.Resources;

public static class ResourceLoadExtensions
{
    public static ParticleEffect? GetEffect(this ResolvedFx fx, ResourceManager resMan)
    {
        if (string.IsNullOrWhiteSpace(fx.AlePath))
        {
            return null;
        }

        foreach (var f in fx.LibraryFiles)
        {
            resMan.LoadResourceFile(f);
        }

        var lib = resMan.GetParticleLibrary(fx.AlePath);
        return lib?.FindEffect(fx.VisFxCrc);
    }


    public static ModelResource? LoadFile(this ResolvedModel mdl, ResourceManager res, MeshLoadMode loadMode = MeshLoadMode.GPU)
    {
        if (mdl.ModelFile == null)
        {
            return null;
        }

        if (mdl.LibraryFiles == null)
        {
            return res.GetDrawable(mdl.ModelFile, loadMode);
        }

        foreach (var f in mdl.LibraryFiles)
            res.LoadResourceFile(f, loadMode);

        return res.GetDrawable(mdl.ModelFile, loadMode);
    }

    public static ThnScript LoadScript(this ResolvedThn thn) =>
        new (thn.VFS.ReadAllBytes(thn.DataPath!), thn.ReadCallback, thn.DataPath!);

    public static DfmFile? LoadModel(this Bodypart bodypart, ResourceManager resources) => (DfmFile?)resources.GetDrawable(bodypart.Path!)?.Drawable;

    public static void Load(this ResolvedTexturePanels panels, ResourceManager res)
    {
        foreach (var f in panels.LibraryFiles!)
        {
            res.LoadResourceFile(f);
        }
    }

    public static IEnumerable<ThnScript> OpenScene(this BaseRoom room) => room.SceneScripts.Select(p => p.Thn.LoadScript());
    public static ThnScript? OpenSet(this BaseRoom room) => room.SetScript?.LoadScript();
    public static ThnScript? OpenGoodscart(this BaseRoom room) => room.GoodscartScript?.LoadScript();
}
