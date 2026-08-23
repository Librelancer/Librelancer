using LibreLancer.Graphics;
using WattleScript.Interpreter;

namespace LibreLancer.Interface;

[UiLoadable]
[WattleScriptUserData]
public class Checkbox : Button
{
    private UiRenderable? check;

    public Checkbox()
    {
        OnClick(_ => Selected = !Selected);
    }

    public override void Render(UiContext context, double delta, DrawList2D drawList)
    {
        if (!Visible)
            return;
        base.Render(context, delta, drawList);
        if (Selected && check != null)
        {
            check.Draw(context, drawList, ClientRectangle);
        }
    }

    protected override ElementStyle OnRestyle(UiContext context)
    {
        var style = new StyleResolver()
            .Add(context.Data.Stylesheet?.Styles.DefaultStyle<CheckboxStyle>())
            .Add(Style)
            .Add(WidthProperty)
            .Add(HeightProperty)
            .Add(mouseEnterSound)
            .Add(mouseDownSound)
            .Create<CheckboxStyle>();
        ButtonStyle = style;
        check = style.Check;
        var stateApp = State switch
        {
            ButtonState.Selected => ButtonStyle.Selected,
            ButtonState.Hover => ButtonStyle.Hover,
            ButtonState.Pressed => ButtonStyle.Pressed,
            ButtonState.Disabled => ButtonStyle.Disabled,
            _ => null
        };

        var res = new StyleResolver()
            .Add(ButtonStyle)
            .Add(ButtonStyle.Normal)
            .Add(stateApp)
            .Add(marginLeft)
            .Add(marginRight)
            .Add(textSize)
            .Add(fontFamily)
            .Add(horizontalAlignment)
            .Add(verticalAlignment)
            .Add(textColor)
            .Add(textShadow)
            .Add(BackgroundProperty)
            .Add(BorderProperty);
        Appearance = res.Create<StyledButton>();
        return Appearance;
    }
}
