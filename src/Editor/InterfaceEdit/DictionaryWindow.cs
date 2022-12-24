using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using ImGuiNET;

namespace InterfaceEdit;

public class DictionaryWindow
{
    public string Title { get; private set; }
    public IDictionary<string,string> Variables { get; private set; }

    public bool IsOpen;
    private string newName;

    public DictionaryWindow(string title, Dictionary<string, string> variables)
    {
        Title = title;
        Variables = variables;
    }

    public unsafe void Draw()
    {
        ImGui.SetNextWindowSize(new Vector2(450,200), ImGuiCond.FirstUseEver);
        if (IsOpen && ImGui.Begin(Title, ref IsOpen))
        {
            if (ImGui.Button("+"))
            {
                newName = "new" + (Variables.Count + 1);
                ImGui.OpenPopup("New##" + Title);
            }

            var allKeys = Variables.ToArray();
            if (ImGui.BeginTable("Items", 3))
            {
                ImGui.TableSetupColumn("Key", ImGuiTableColumnFlags.NoReorder | ImGuiTableColumnFlags.WidthStretch);
                ImGui.TableSetupColumn("Value", ImGuiTableColumnFlags.NoReorder | ImGuiTableColumnFlags.WidthStretch);
                ImGui.TableSetupColumn("", ImGuiTableColumnFlags.NoReorder | ImGuiTableColumnFlags.NoResize | ImGuiTableColumnFlags.WidthFixed, 18);
                ImGui.TableHeadersRow();

                List<string> deleteKeys = new List<string>();
                foreach (var kv in allKeys)
                {
                    ImGui.TableNextRow();
                    ImGui.TableSetColumnIndex(0);
                    ImGui.Text(kv.Key);
                    ImGui.TableSetColumnIndex(1);
                    string v = kv.Value;
                    bool set = false;
                    ImGui.InputText("##" + kv.Key, ref v, 2000, ImGuiInputTextFlags.CallbackEdit, data =>
                    {
                        set = true;
                        return 0;
                    });
                    if (set) 
                        Variables[kv.Key] = v;
                    ImGui.TableSetColumnIndex(2);
                    if(ImGui.Button("x##" + kv.Key))
                        deleteKeys.Add(kv.Key);
                }
                foreach (var k in deleteKeys)
                    Variables.Remove(k);

                ImGui.EndTable();
            }
            if (ImGui.BeginPopupModal("New##" + Title))
            {
                ImGui.InputText("Name", ref newName, 2000);
                if (ImGui.Button("Ok"))
                {
                    Variables.Add(newName, "");
                    ImGui.CloseCurrentPopup();
                }
                ImGui.SameLine();
                if (ImGui.Button("Cancel"))
                {
                    ImGui.CloseCurrentPopup();
                }
                ImGui.EndPopup();
            }
            ImGui.End();
        }
    }
}