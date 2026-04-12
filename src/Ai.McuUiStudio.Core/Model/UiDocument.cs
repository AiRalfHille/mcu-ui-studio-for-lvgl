namespace Ai.McuUiStudio.Core.Model;

public sealed class UiDocument
{
    public UiDocument(UiNode root)
    {
        Root = root;
    }

    public UiNode Root { get; set; }
}
