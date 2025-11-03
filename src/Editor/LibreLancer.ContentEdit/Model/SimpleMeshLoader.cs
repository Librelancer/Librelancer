using System;
using System.Numerics;
using System.Threading;
using System.Threading.Tasks;

namespace LibreLancer.ContentEdit.Model;

public static class SimpleMeshLoader
{
    public static async Task<EditResult<SimpleMesh.Model>> ModelFromFile(
        string filename,
        string blenderPath = null,
        CancellationToken cancellationToken = default,
        Action<string> log = null)
    {
        var model =
            Blender.FileIsBlender(filename)
                ? await Blender.LoadBlenderFile(filename, cancellationToken, log, blenderPath)
                : await EditResult<SimpleMesh.Model>.RunBackground(
                    () => { return EditResult<SimpleMesh.Model>.TryCatch(() => SimpleMesh.Model.FromFile(filename)); },
                    cancellationToken);
        return model.Then(x =>
        {
            var mdl = x.Data.AutoselectRoot(out _)
                .ApplyScale();
            var rootPos = Vector3.Transform(Vector3.Zero, mdl.Roots[0].Transform);
            var modelWarning = rootPos.Length() > 0.0001;
            mdl = mdl.ApplyRootTransforms(false)
                .CalculateBounds()
                .MergeTriangleGroups();
            if (modelWarning)
                return new EditResult<SimpleMesh.Model>(mdl,
                    [EditMessage.Warning("Model root is off-center, consider re-exporting.")]);
            return mdl.AsResult();
        });
    }
}
