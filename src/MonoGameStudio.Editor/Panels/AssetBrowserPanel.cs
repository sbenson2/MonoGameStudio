using ImGuiNET;
using MonoGameStudio.Editor.Layout;

namespace MonoGameStudio.Editor.Panels;

public class AssetBrowserPanel
{
    public void Draw()
    {
        if (ImGui.Begin(LayoutDefinitions.AssetBrowser))
        {
            ImGui.TextDisabled("Asset Browser");
        }
        ImGui.End();
    }
}
