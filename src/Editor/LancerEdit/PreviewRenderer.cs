using System;
using System.Collections.Generic;
using System.Numerics;
using LibreLancer;
using LibreLancer.Data.GameData;
using LibreLancer.Graphics;
using LibreLancer.Render;
using LibreLancer.Render.Cameras;
using LibreLancer.Resources;
using LibreLancer.Utf.Mat;
using LibreLancer.World;

namespace LancerEdit;

public class PreviewRenderer : IDisposable
{
    static SystemLighting lighting;
    static PreviewRenderer()
    {
        lighting = new SystemLighting();
        lighting.Lights.Add(new DynamicLight()
        {
            Light = new RenderLight()
            {
                Kind = LightKind.Directional,
                Direction = new Vector3(0, -1, 0),
                Color = Color3f.White
            }
        });
        lighting.Lights.Add(new DynamicLight()
        {
            Light = new RenderLight()
            {
                Kind = LightKind.Directional,
                Direction = new Vector3(0, 0, 1),
                Color = Color3f.White
            }
        });
    }

    private MainWindow win;
    private GameResourceManager resources;
    private LookAtCamera camera;
    private SystemRenderer renderer;

    public GameResourceManager Resources => resources;
    public PreviewRenderer(MainWindow win, GameResourceManager resources)
    {
        this.win = win;
        this.resources = resources;
        camera = new LookAtCamera();
        renderer = new SystemRenderer(camera, resources, win);
        renderer.NullColor = new Color4(56, 57, 58, 255);
        renderer.SystemLighting = lighting;
    }

    public Texture2D Render(Asteroid asteroid, int width, int height)
    {
        var mdl = asteroid.ModelFile.LoadFile(resources, MeshLoadMode.GPU | MeshLoadMode.NoCollision);
        var radius = 10f;
        if (mdl.Drawable is IRigidModelFile rmf)
        {
            radius = rmf.CreateRigidModel(true, resources).GetRadius();
        }
        if (mdl.Drawable is SphFile)
        {
            radius *= 1.17f; //render planets a little smaller, looks better
        }
        var mat = Matrix4x4.CreateFromYawPitchRoll(2.62f, -0.24f, 0);
        var res = Vector3.Transform(new Vector3(0, 0, radius* 2.35f), mat);
        camera.Update(width, height, res, Vector3.Zero);
        var world = new GameWorld(renderer, resources, null, false);
        var obj = new GameObject(mdl, resources, true, false);
        obj.SetLocalTransform(Transform3D.Identity);
        obj.World = world;
        world.AddObject(obj);
        return RenderWorldAndDispose(obj, world, width, height);
    }

    Texture2D RenderWorldAndDispose(GameObject obj, GameWorld world, int width, int height)
    {
        var restoreTarget = win.RenderContext.RenderTarget;
        var renderTarget = new RenderTarget2D(win.RenderContext, width, height);
        win.RenderContext.RenderTarget = renderTarget;
        win.RenderContext.PushViewport(0, 0, width, height);
        win.RenderContext.Cull = true;
        //HACK: Fix up renderer objects to not need this step
        renderer.objects = new List<ObjectRenderer>();
        obj.PrepareRender(camera, null, renderer);
        //Update preview world
        world.Update(1 / 60.0f);
        for(int i = 0; i < 120; i++)
            world.RenderUpdate(1 / 30.0f);
        renderer.Draw(width, height);
        //Clean state
        win.RenderContext.PopViewport();
        win.RenderContext.DepthEnabled = false;
        win.RenderContext.BlendMode = BlendMode.Normal;
        win.RenderContext.Cull = false;
        win.RenderContext.RenderTarget = restoreTarget;
        renderTarget.Dispose(true);
        world.Dispose();
        return renderTarget.Texture;
    }

    public Texture2D Render(Archetype archetype, int width, int height)
    {
        var mdl = archetype.ModelFile?.LoadFile(resources, MeshLoadMode.GPU | MeshLoadMode.NoCollision).Drawable;
        var radius = 10f;
        if (mdl is IRigidModelFile rmf)
        {
            radius = rmf.CreateRigidModel(true, resources).GetRadius();
        }
        if (mdl is SphFile)
        {
            radius *= 1.17f; //render planets a little smaller, looks better
        }

        var mat = Matrix4x4.CreateFromYawPitchRoll(2.62f, -0.24f, 0);
        var res = Vector3.Transform(new Vector3(0, 0, radius* 2.35f), mat);
        camera.Update(width, height, res, Vector3.Zero);
        var world = new GameWorld(renderer, resources, null, false);
        var obj = new GameObject(archetype, null, resources, true, false);
        if(archetype.Loadout != null)
            obj.SetLoadout(archetype.Loadout, null);
        obj.SetLocalTransform(Transform3D.Identity);
        obj.World = world;
        world.AddObject(obj);
        obj.Register(world.Physics); //no physics but register method called
        return RenderWorldAndDispose(obj, world, width, height);
    }

    public void Dispose()
    {
        renderer.Dispose();
    }
}
