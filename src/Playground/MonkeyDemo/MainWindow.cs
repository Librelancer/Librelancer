// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System.Numerics;
using ImGuiNET;
using LibreLancer;
using LibreLancer.Data.IO;
using LibreLancer.Data.Schema;
using LibreLancer.ImUI;
using LibreLancer.Render;
using LibreLancer.Render.Cameras;
using LibreLancer.Resources;
using LibreLancer.Shaders;
using LibreLancer.Thn;
using LibreLancer.World;

namespace MonkeyDemo;

public class MainWindow : Game
{
    private ImGuiHelper imGui = null!;
    private GameResourceManager resources = null!;
    private SystemRenderer renderer = null!;
    private GameWorld world = null!;
    private FileSystem vfs;

    private Billboards billboards = null!;
    private CommandBuffer commands = null!;

    private GameObject monkey = null!;
    private MonkeyComponent control = null!;

    private Cutscene cutscene;


    public MainWindow() : base(800, 600, true)
    {
        vfs = FileSystem.FromPath(Path.Combine(Platform.GetBasePath(), "Assets"));
    }

    protected override void Load()
    {
        Title = "Monkey Demo";
        imGui = new ImGuiHelper(this, 1);
        RenderContext.PushViewport(0, 0, Width, Height);
        AllShaders.Compile(RenderContext);
        new MaterialMap(); // bad

        commands = new(RenderContext);
        billboards = new(RenderContext);
        resources = new(this, vfs);

        Services.Add(commands);
        Services.Add(billboards);
        Services.Add(resources);
        Services.Add(new GameSettings()); // Default settings

        renderer = new(new LookAtCamera(), resources, this);
        renderer.SystemLighting.Ambient = new Color4(0.6f, 0.6f, 0.6f, 1f);
        renderer.BackgroundOverride = new Color4(0.2f, 0.2f, 0.2f, 1f);
        world = new(renderer, resources, null);

        var model = resources.GetDrawable("monkey.3db")!;
        monkey = new GameObject(model, resources);
        monkey.PhysicsComponent!.Mass = 3;
        control = new MonkeyComponent(monkey);
        monkey.AddComponent(control);
        world.AddObject(monkey);
        monkey.Register(world);

        cutscene = new(new ThnScriptContext(null) { MainObject = monkey },
            this, null!, world, resources);
        cutscene.BeginScene(new ThnScript(File.ReadAllBytes("Assets/camera_follow.lua"),
            null!, "thorn"));
    }

    private double accum = 0;
    void UpdateWorld(double delta)
    {
        Console.WriteLine("WORLD UPDATE");
        accum += delta;
        double updateInterval = 1 / 60.0;
        while (accum >= updateInterval)
        {
            accum -= updateInterval;
            double fixedDelta = 1 / 60.0;
            world.Update(fixedDelta);
        }
        var fraction = accum / updateInterval;
        world.UpdateInterpolation((float) fraction);

        Console.WriteLine("CUTSCENE UPDATE");
        cutscene.UpdateViewport(new(0, 0, Width, Height), (float)Width / Height);
        cutscene.Update(delta);
        renderer.Camera = cutscene.CameraHandle;
    }


    protected override void Update(double elapsed)
    {
        Console.WriteLine("UPDATE FRAME");

        control.Move = Keyboard.IsKeyDown(Keys.Space);
        control.Rotate = Keyboard.IsKeyDown(Keys.Enter);
        UpdateWorld(elapsed);
    }

    private bool vsync = true;

    private float mZ = 0;
    protected override void Draw(double elapsed)
    {
        Console.WriteLine("DRAW FRAME");
        imGui.NewFrame(elapsed);

        RenderContext.ReplaceViewport(0, 0, Width, Height);


        if (monkey.WorldTransform.Position.Z < mZ)
        {
            throw new Exception();
        }

        mZ = monkey.WorldTransform.Position.Z;
        world.RenderUpdate(elapsed);
        renderer.Draw(Width, Height);

        var camera = (ThnCamera)cutscene.CameraHandle;

        Console.WriteLine($"r: {camera.Position}, {monkey.WorldTransform.Position}, " +
            $"{monkey.WorldTransform.Position + new Vector3(-10, 7.5f, 30)}");
        ImGui.PushFont(ImGuiHelper.Roboto, 0);

        var o = vsync;
        ImGui.Checkbox("VSync", ref vsync);
        if(o != vsync)
            SetVSync(vsync);
        ImGui.Text("Test scene for following .thn MainObject with the camera. Enter = rotate, Space = velocity.");
        ImGui.Text($"FPS: {RenderFrequency:0.00}");
        ImGui.Text($"Monkey Pos: {monkey.LocalTransform.Position}");
        ImGui.Text($"Monkey Velocity: {monkey.PhysicsComponent.Body.LinearVelocity}");
        ImGui.Text($"{monkey.PhysicsComponent.Body.AngularVelocity}");
        ImGui.PopFont();
        imGui.Render(RenderContext);
    }

}
