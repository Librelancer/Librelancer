// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

namespace LibreLancer;

public delegate void MouseWheelEventHandler(float amountx, float amounty);
public delegate void MouseEventHandler(MouseEventArgs e);
public class Mouse
{
    public int X { get; internal set; }
    public int Y { get; internal set; }
    public MouseButtons Buttons { get; internal set; }

    public event MouseEventHandler? MouseMove;
    public event MouseEventHandler? MouseDown;
    public event MouseEventHandler? MouseUp;

    public event MouseWheelEventHandler? MouseWheel;

    public event MouseEventHandler? MouseDoubleClick;

    public float Wheel = 0;

    internal Mouse ()
    {
    }

    public bool IsButtonDown(MouseButtons b)
    {
        return (Buttons & b) == b;
    }
    internal void OnMouseMove(int x, int y)
    {
        MouseMove?.Invoke(new MouseEventArgs (x, y, Buttons));
    }

    internal void OnMouseWheel(float amountx, float amounty)
    {
        Wheel += amounty;
        MouseWheel?.Invoke(amountx, amounty);
    }
    internal void OnMouseDown(MouseButtons b)
    {
        MouseDown?.Invoke(new MouseEventArgs (X, Y, b));
    }

    internal void OnMouseDoubleClick(MouseButtons b)
    {
        MouseDoubleClick?.Invoke(new MouseEventArgs(X, Y, b));
    }

    internal void OnMouseUp (MouseButtons b)
    {
        MouseUp?.Invoke(new MouseEventArgs (X, Y, b));
    }


}
