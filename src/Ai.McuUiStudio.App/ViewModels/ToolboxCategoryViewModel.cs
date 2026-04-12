using System.Collections.ObjectModel;

namespace Ai.McuUiStudio.App.ViewModels;

public sealed class ToolboxCategoryViewModel
{
    public ToolboxCategoryViewModel(string name, IEnumerable<string> tools)
    {
        Name = name;
        Tools = new ObservableCollection<string>(tools);
    }

    public string Name { get; }

    public ObservableCollection<string> Tools { get; }
}
