using System;
using LibreLancer.Platforms;

namespace LibreLancer;

public class GameConfiguration
{
    public bool IsSDL { get; private set; }
    private Action onTick;
    private int maxIterations;

    private GameConfiguration()
    {
    }

    public static GameConfiguration SDL() => new GameConfiguration() { IsSDL = true };
    public static GameConfiguration HeadlessTest() => new GameConfiguration() { IsSDL = false };

    public GameConfiguration WithTick(Action tick)
    {
        if (IsSDL) throw new InvalidOperationException("Only valid for HeadlessTest");
        onTick = tick;
        return this;
    }

    public GameConfiguration WithMaxIterations(int maxIterations)
    {
        if (IsSDL) throw new InvalidOperationException("Only valid for HeadlessTest");
        this.maxIterations = maxIterations;
        return this;
    }

    internal IGame GetGame(int width, int height, bool fullscreen, bool allowScreensaver)
    {
        if (IsSDL)
            return new SDL2Game(width, height, fullscreen, allowScreensaver);
        else
            return new NullGame() { OnTick = onTick, MaxIterations = maxIterations};
    }
}
