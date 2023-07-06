using System.Numerics;
using LibreLancer;
using LibreLancer.Render;
using LibreLancer.Render.Cameras;

namespace LancerEdit;

public class ModelPreviews
{
    static Lighting lighting;
    static ModelPreviews()
    {
        lighting = Lighting.Create();
        lighting.Enabled = true;
        lighting.Ambient = Color3f.Black;
        var src = new SystemLighting();
        src.Lights.Add(new DynamicLight()
        {
            Light = new RenderLight()
            {
                Kind = LightKind.Directional,
                Direction = new Vector3(0, -1, 0),
                Color = Color3f.White
            }
        });
        src.Lights.Add(new DynamicLight()
        {
            Light = new RenderLight()
            {
                Kind = LightKind.Directional,
                Direction = new Vector3(0, 0, 1),
                Color = Color3f.White
            }
        });
        lighting.Lights.SourceLighting = src;
        lighting.Lights.SourceEnabled[0] = true;
        lighting.Lights.SourceEnabled[1] = true;
        lighting.NumberOfTilesX = -1;
    }
    
    public static Texture2D RenderPreview(MainWindow win, RigidModel model, ResourceManager resources, int width, int height)
    {
        var restoreTarget = win.RenderContext.RenderTarget;
        var renderTarget = new RenderTarget2D(width, height);
        win.RenderContext.Cull = true;
        win.RenderContext.DepthEnabled = true;
        win.RenderContext.RenderTarget = renderTarget;
        win.RenderContext.PushViewport(0,0,width,height);
        win.RenderContext.ClearColor = Color4.Transparent;
        win.RenderContext.ClearAll();
        //Set camera
        var mat = Matrix4x4.CreateFromYawPitchRoll(2.62f, -0.24f, 0);
        var res = Vector3.Transform(new Vector3(0, 0, model.GetRadius() * 2.1f), mat);
        var camera = new LookAtCamera();
        camera.Update(width, height, res, Vector3.Zero);
        win.RenderContext.SetCamera(camera);
        win.Commands.Camera = camera;
        //Set model
        model.Update(0);
        model.UpdateTransform();
        //Draw
        win.Commands.StartFrame(win.RenderContext);
        model.DrawBuffer(0, win.Commands, resources, Matrix4x4.Identity, ref lighting);
        win.Commands.DrawOpaque(win.RenderContext);
        win.RenderContext.DepthWrite = false;
        win.Commands.DrawTransparent(win.RenderContext);
        win.RenderContext.DepthWrite = true;
        //Clean state
        win.RenderContext.PopViewport();
        win.RenderContext.DepthEnabled = false;
        win.RenderContext.BlendMode = BlendMode.Normal;
        win.RenderContext.Cull = false;
        win.RenderContext.RenderTarget = restoreTarget;
        renderTarget.Dispose(true);
        return renderTarget.Texture;
    }
}