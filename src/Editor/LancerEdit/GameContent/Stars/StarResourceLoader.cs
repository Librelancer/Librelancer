using LibreLancer.Data;
using LibreLancer.Resources;
using LibreLancer;

namespace LancerEdit.GameContent.Stars;

public static class StarResourceLoader
{
    public static void EnsureLoaded(GameDataContext context)
    {
        if (context.GameData.Items.Ini.Stars == null)
            return;

        foreach (var section in context.GameData.Items.Ini.Stars.TextureFiles)
        {
            foreach (var file in section.Files)
                context.Resources.LoadResourceFile(context.GameData.Items.DataPath(file));

            if (context.Resources is not GameResourceManager resourceManager)
                continue;

            foreach (var shape in section.Shapes)
            {
                if (string.IsNullOrWhiteSpace(shape) ||
                    resourceManager.TryGetShape(shape, out _))
                {
                    continue;
                }

                resourceManager.AddShape(shape,
                    new TextureShape(shape, shape, new RectangleF(0, 0, 1, 1)));
            }
        }
    }
}
