using ImGuiNET;

namespace LibreLancer.ImUI;

public abstract class PopupWindow
{
    public abstract string Title { get; set; }
    public virtual ImGuiWindowFlags WindowFlags => 0;
    
    public virtual bool NoClose => false;
    public abstract void Draw();

    public virtual void OnClosed() { }
}