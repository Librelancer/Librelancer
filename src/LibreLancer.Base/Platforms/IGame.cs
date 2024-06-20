using System;
using LibreLancer.Graphics;

namespace LibreLancer.Platforms;

interface IGame : IUIThread
{
    //Events
    void WaitForEvent(int timeout);
    void InterruptWait();
    bool EventsThisFrame { get; }
    //Window
    float DpiScale { get; }
    int Width { get; }
    int Height { get; }
    void SetWindowIcon(int width, int height, ReadOnlySpan<Bgra8> data);
    bool Focused { get; }
    string Title { get; set; }
    Point MinimumWindowSize { get; set; }
    void BringToFront();
    void SetVSync(bool vsync);
    //Loop
    void Run(Game loop);
    void Exit();
    void Crashed();

    bool IsUiThread();
    //Timing
    double TotalTime { get; }
    double TimerTick { get; }
    double RenderFrequency { get; }
    double FrameTime { get; }
    //Hardware
    bool RelativeMouseMode { get; set; }
    string Renderer { get; }
    RenderContext RenderContext { get; }
    Mouse Mouse { get; }
    Keyboard Keyboard { get; }
    void EnableTextInput();
    void DisableTextInput();
    void ToggleFullScreen();
    ScreenshotSaveHandler OnScreenshotSave { get; set; }
    void Screenshot(string filename);
    //Clipboard
    ClipboardContents ClipboardStatus();
    string GetClipboardText();
    void SetClipboardText(string text);
    byte[] GetClipboardArray();
    void SetClipboardArray(byte[] array);
    //Cursor
    CursorKind CursorKind { get; set; }
}
