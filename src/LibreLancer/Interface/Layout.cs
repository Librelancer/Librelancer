using System.Numerics;

namespace LibreLancer.Interface;

public class Layout
{
    public RectangleF Parent;

    public Layout(RectangleF parent)
    {
        Parent = parent;
    }

    public virtual RectangleF Place(RectangleF child, AnchorKind anchor)
    {
        var pos = AnchorPosition(Parent, anchor, child.X, child.Y, child.Width, child.Height);
        return new(pos.X, pos.Y, child.Width, child.Height);
    }

    public virtual RectangleF Fill() => Parent;

    protected static Vector2 AnchorPosition(RectangleF parent, AnchorKind anchor, float x, float y, float width,
        float height)
    {
        float resolveX = 0;
        float resolveY = 0;

        switch (anchor)
        {
            case AnchorKind.TopLeft:
                resolveX = parent.X + x;
                resolveY = parent.Y + y;
                break;
            case AnchorKind.TopCenter:
                resolveX = parent.X + (parent.Width / 2) - (width / 2) + x;
                resolveY = parent.Y + y;
                break;
            case AnchorKind.TopRight:
                resolveX = parent.X + parent.Width - width - x;
                resolveY = parent.Y + y;
                break;
            case AnchorKind.CenterLeft:
                resolveX = parent.X + x;
                resolveY = parent.Y + (parent.Height / 2) - (height / 2) + y;
                break;
            case AnchorKind.Center:
                resolveX = parent.X + (parent.Width / 2) - (width / 2) + x;
                resolveY = parent.Y + (parent.Height / 2) - (height / 2) + y;
                break;
            case AnchorKind.CenterRight:
                resolveX = parent.X + parent.Width - width - x;
                resolveY = parent.Y + (parent.Height / 2) - (height / 2) + y;
                break;
            case AnchorKind.BottomLeft:
                resolveX = parent.X + x;
                resolveY = parent.Y + parent.Height - height - y;
                break;
            case AnchorKind.BottomCenter:
                resolveX = parent.X + (parent.Width / 2) - (width / 2) + x;
                resolveY = parent.Y + parent.Height - height - y;
                break;
            case AnchorKind.BottomRight:
                resolveX = parent.X + parent.Width - width - x;
                resolveY = parent.Y + parent.Height - height - y;
                break;
        }

        return new Vector2(resolveX, resolveY);
    }
}
